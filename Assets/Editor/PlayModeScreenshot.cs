using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.EditorTools
{
    /// <summary>
    /// QA helper: keeps a single Unity Editor instance running and services screenshot
    /// requests dropped as a JSON file, so external tooling never has to relaunch the Editor.
    ///
    /// One-time launch (GUI, not -batchmode - Play Mode needs a real render surface):
    ///   Unity.exe -projectPath . -logFile Builds/Screenshots/editor.log
    /// [InitializeOnLoad] means the poll loop starts as soon as the Editor is open, and keeps
    /// running across script-recompile domain reloads.
    ///
    /// Each iteration, write Builds/Screenshots/request.json (atomically - write to a .tmp file
    /// then rename, so the Editor never reads a half-written file):
    ///   {"id":"<unique>","scene":"Assets/_Project/Scenes/Map.unity","output":"Builds/Screenshots/map.png","waitSeconds":3}
    /// "scene" may be empty/omitted to reuse whatever scene is already open.
    /// Poll for Builds/Screenshots/done.json; it is rewritten once per request with
    ///   {"id":"<same id>","status":"ok"|"error","message":"..."}
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeScreenshot
    {
        const string RequestPath = "Builds/Screenshots/request.json";
        const string DonePath = "Builds/Screenshots/done.json";

        const string KeyLastRequestId = "KMA_PMS_LastRequestId";
        const string KeyActive = "KMA_PMS_Active";
        const string KeyId = "KMA_PMS_Id";
        const string KeyOutput = "KMA_PMS_Output";
        const string KeyCaptureAt = "KMA_PMS_CaptureAt";
        const string KeyPhase = "KMA_PMS_Phase";
        const string KeyFrames = "KMA_PMS_Frames";

        [Serializable]
        class Request
        {
            public string id;
            public string scene;
            public string output;
            public float waitSeconds = 3f;
        }

        static PlayModeScreenshot()
        {
            EditorApplication.update += OnUpdate;
        }

        // Optional: -executeMethod entry point for a one-shot legacy invocation.
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var req = new Request
            {
                id = Guid.NewGuid().ToString(),
                scene = GetArg(args, "-kmaScene"),
                output = GetArg(args, "-kmaOutput") ?? "Builds/Screenshots/screenshot.png",
            };
            var waitArg = GetArg(args, "-kmaWaitSeconds");
            if (waitArg != null) float.TryParse(waitArg, out req.waitSeconds);
            StartCapture(req);
        }

        static void OnUpdate()
        {
            if (!SessionState.GetBool(KeyActive, false))
            {
                PollForNewRequest();
                return;
            }
            RunActiveCapture();
        }

        static void PollForNewRequest()
        {
            string fullPath = Path.GetFullPath(RequestPath);
            if (!File.Exists(fullPath)) return;

            Request req;
            try
            {
                req = JsonUtility.FromJson<Request>(File.ReadAllText(fullPath));
            }
            catch
            {
                return; // likely mid-write; try again next tick
            }

            if (req == null || string.IsNullOrEmpty(req.id) || req.id == SessionState.GetString(KeyLastRequestId, ""))
                return;

            SessionState.SetString(KeyLastRequestId, req.id);
            StartCapture(req);
        }

        static void StartCapture(Request req)
        {
            try
            {
                if (!string.IsNullOrEmpty(req.scene))
                    EditorSceneManager.OpenScene(req.scene);
            }
            catch (Exception e)
            {
                WriteDone(req.id, "error", "OpenScene failed: " + e.Message);
                return;
            }

            string output = Path.GetFullPath(string.IsNullOrEmpty(req.output) ? "Builds/Screenshots/screenshot.png" : req.output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            SessionState.SetString(KeyId, req.id);
            SessionState.SetString(KeyOutput, output);
            SessionState.SetFloat(KeyCaptureAt, (float)(EditorApplication.timeSinceStartup + req.waitSeconds));
            SessionState.SetInt(KeyPhase, 0);
            SessionState.SetInt(KeyFrames, 0);
            SessionState.SetBool(KeyActive, true);

            EditorApplication.isPlaying = true;
        }

        static void RunActiveCapture()
        {
            if (!EditorApplication.isPlaying) return;

            int phase = SessionState.GetInt(KeyPhase, 0);
            if (phase == 0)
            {
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat(KeyCaptureAt, 0)) return;
                string output = SessionState.GetString(KeyOutput, null);
                ScreenCapture.CaptureScreenshot(output);
                Debug.Log("[KMA] PlayModeScreenshot captured: " + output);
                SessionState.SetInt(KeyPhase, 1);
                return;
            }

            // Give CaptureScreenshot a few frames to flush the file before tearing down.
            int frames = SessionState.GetInt(KeyFrames, 0) + 1;
            SessionState.SetInt(KeyFrames, frames);
            if (frames < 5) return;

            SessionState.SetBool(KeyActive, false);
            EditorApplication.isPlaying = false;
            WriteDone(SessionState.GetString(KeyId, ""), "ok", null);
        }

        static void WriteDone(string id, string status, string message)
        {
            string fullPath = Path.GetFullPath(DonePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string json = JsonUtility.ToJson(new DoneResult { id = id, status = status, message = message });
            File.WriteAllText(fullPath + ".tmp", json);
            File.Copy(fullPath + ".tmp", fullPath, true);
            File.Delete(fullPath + ".tmp");
        }

        [Serializable]
        class DoneResult
        {
            public string id;
            public string status;
            public string message;
        }

        static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
