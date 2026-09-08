---
name: testing-unity-ui-with-screenshots
description: Use when verifying how a Unity scene, menu, HUD, or minigame actually looks in this project - confirming a UI change rendered correctly, checking layout/text/colors visually - without building an APK or manually clicking Play in the Editor.
---

# Testing Unity UI with Screenshots

## Overview

A single background Unity Editor instance stays open and services screenshot
requests dropped as a JSON file. No APK build, no manual Play Mode clicks, no
relaunching the Editor between checks — each request takes seconds once the
Editor is warm.

## When to use

- "Does this menu/HUD/scene look right?" for this project
- Verifying a UI or scene-config change visually after editing it
- Spot-checking any of the `Assets/_Project/Scenes/*.unity` scenes in Play Mode

Not for interaction testing — this captures one static frame after a wait;
no keyboard/touch input is simulated. Not for Android-specific rendering
concerns (safe areas, GPU driver quirks) — build an APK for those instead.

## Setup (once per session)

1. Check nothing already has the project open (only one Editor may hold the
   project lock):
   ```
   tasklist //FI "IMAGENAME eq Unity.exe"
   ```
2. Find the Editor version this project needs: `ProjectSettings/ProjectVersion.txt`.
3. Launch a background Editor — GUI, **not** `-batchmode` (Play Mode needs a
   real render surface to capture from):
   ```bash
   "/c/Program Files/Unity/Hub/Editor/<version>/Editor/Unity.exe" \
     -projectPath "<absolute-repo-path>" -logFile Builds/Screenshots/editor.log &
   ```
   Run this with a backgroundable shell call — it's a long-lived GUI process.
4. Wait for first compile to finish (cold start is ~30-90s): poll the log for
   `Loaded All Assemblies`:
   ```bash
   for i in $(seq 1 40); do grep -q "Loaded All Assemblies" Builds/Screenshots/editor.log 2>/dev/null && break; sleep 3; done
   ```

## Capture a screenshot

```bash
tools/qa-screenshot.sh <output.png> [scene.unity] [waitSeconds]
```

Example:
```bash
tools/qa-screenshot.sh Builds/Screenshots/map.png Assets/_Project/Scenes/Map.unity 4
```

This writes a request; the already-running Editor picks it up, opens the
scene, enters Play Mode, waits `waitSeconds` (default 3 — raise it for scenes
with a splash/loading delay), captures, exits Play Mode, and prints
`done.json`'s contents. Then read the resulting PNG with the Read tool to
actually look at it — the command succeeding only means the file was written.

Omit `scene` to capture whatever is already open/playing (useful for a
second screenshot of the same session without a scene reload).

## How it works

`Assets/Editor/PlayModeScreenshot.cs` is an `[InitializeOnLoad]` static class
— its polling loop starts the moment the Editor opens and survives every
script-recompile domain reload. It watches `Builds/Screenshots/request.json`;
when a new `id` shows up, it opens the requested scene, flips
`EditorApplication.isPlaying`, waits, calls `ScreenCapture.CaptureScreenshot`,
exits Play Mode, and writes `Builds/Screenshots/done.json` with that same id.

## Common mistakes

- **Launching a fresh Unity.exe per screenshot.** Slow (60-90s) and defeats
  the point — reuse the one running instance for the rest of the session.
- **Using `-batchmode`.** Play Mode won't have a render surface; the capture
  comes back blank or black.
- **Trusting the PNG before checking `done.json`'s id matches your request
  and `status` is `"ok"`.** `tools/qa-screenshot.sh` already waits for this;
  don't `Read` the output file before the script returns.
- **Forgetting the Editor holds the project lock.** Close it before running
  `tools/build-apk.sh` or any other Unity batch command against this project.

## Quick reference

| Task | Command |
|---|---|
| Start editor | `Unity.exe -projectPath . -logFile Builds/Screenshots/editor.log &` |
| Screenshot | `tools/qa-screenshot.sh <out.png> [scene] [waitSeconds]` |
| Check editor alive | `tasklist //FI "IMAGENAME eq Unity.exe"` |
| Stop editor | `taskkill //PID <pid> //F` when done testing |
