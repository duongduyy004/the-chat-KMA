using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace KMA.EditorTools {
[InitializeOnLoad] public static class FootballWorkflow {
 const string RequestPath="Builds/FootballWorkflow/request.json";
 static TestRunnerApi runner;
 [Serializable] class Request {public string id;public string operation;public string filter;public string output;}
 static FootballWorkflow(){EditorApplication.update+=Poll; runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Callbacks());}
 static void Poll(){
  if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(RequestPath))return;
  try {var r=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));if(r==null||string.IsNullOrEmpty(r.id)||SessionState.GetString("FootballWorkflow.Last","")==r.id)return;
   SessionState.SetString("FootballWorkflow.Last",r.id);
   if(r.operation=="close"){EditorSceneManager.SaveOpenScenes();EditorApplication.Exit(0);return;}
   if(r.operation=="build"){FootballSceneConfigurator.BuildScene();File.WriteAllText(r.output,"built");return;}
   SessionState.SetString("FootballWorkflow.Output",r.output);
   var f=new Filter{testMode=r.operation=="play"?TestMode.PlayMode:TestMode.EditMode};
   if(!string.IsNullOrEmpty(r.filter))f.groupNames=new[]{r.filter};
   runner.Execute(new ExecutionSettings(f));
  }catch(Exception e){Debug.LogException(e);}
 }
 class Callbacks:ICallbacks {
  public void RunStarted(ITestAdaptor t){} public void TestStarted(ITestAdaptor t){} public void TestFinished(ITestResultAdaptor r){}
  public void RunFinished(ITestResultAdaptor r){var path=SessionState.GetString("FootballWorkflow.Output","");if(!string.IsNullOrEmpty(path)){Directory.CreateDirectory(Path.GetDirectoryName(path));TestRunnerApi.SaveResultToFile(r,path);}}
 }
}}
