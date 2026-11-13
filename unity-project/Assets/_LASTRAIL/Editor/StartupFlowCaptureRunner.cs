using System;
using System.IO;
using LastRail.Presentation.Startup;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastRail.Editor
{
    // CLI-only QA entry. Does not save scenes or touch user-owned Editor processes.
    [InitializeOnLoad]
    public static class StartupFlowCaptureRunner
    {
        const string Pending = "LastRail.FlowCapture.Pending";
        const string Output = "LastRail.FlowCapture.Output";
        const string Scenario = "LastRail.FlowCapture.Scenario";
        const string Scene = "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity";
        static StartupFlowCaptureRunner() { EditorApplication.playModeStateChanged += StateChanged; }

        public static void Run()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A dedicated idle batch Editor is required.");
            var args = Environment.GetCommandLineArgs();
            var output = Argument(args,"-flowOutput");
            var scenario = Argument(args,"-flowScenario") ?? "fast";
            if (scenario!="fast" && scenario!="slow" && scenario!="failed" && scenario!="update" && scenario!="reentry")
                throw new InvalidOperationException("Unknown local QA scenario.");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Act1Launch/Frames")) + Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(output) || !Path.GetFullPath(output).StartsWith(root,StringComparison.OrdinalIgnoreCase) || Directory.Exists(output))
                throw new InvalidOperationException("Fresh QA/Act1Launch/Frames output required.");
            EditorSceneManager.OpenScene(Scene);
            var boot = UnityEngine.Object.FindFirstObjectByType<ResourceBootstrapController>();
            if (boot==null) throw new InvalidOperationException("Bootstrap missing.");
            // Prevent real network/auth startup before installing explicit local inputs.
            boot.enabled=false;
            SessionState.SetString(Output,Path.GetFullPath(output)); SessionState.SetString(Scenario,scenario);
            SessionState.SetBool(Pending,true);
            EditorApplication.EnterPlaymode();
        }
        static string Argument(string[] args,string name)
        {
            for(int i=0;i<args.Length-1;i++) if(args[i]==name) return args[i+1];
            return null;
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Pending,false)) return;
            string output=SessionState.GetString(Output,"");
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    var scenario=SessionState.GetString(Scenario,"fast");
                    new GameObject("FlowCaptureFixture").AddComponent<StartupPlayModeCapture>().Begin(output,scenario,scenario=="fast"||output.Contains("native-popup"));
                }
                catch(Exception error)
                {
                    Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output,"failed.txt"),error.ToString());
                    EditorApplication.ExitPlaymode();
                }
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending,false);
                Time.captureFramerate=0;
                ResourceBootstrapController.EditorCaptureBackend=null;
                ResourceBootstrapController.EditorCaptureAuthentication=null;
                EditorSceneManager.OpenScene(Scene); // Restore original unsaved scene state.
                bool success=File.Exists(Path.Combine(output,"complete.json")) && !File.Exists(Path.Combine(output,"failed.txt"));
                File.WriteAllText(Path.Combine(output,"editor-restored.json"),"{\"playMode\":false,\"captureFramerate\":0,\"sceneDirty\":"+(EditorSceneManager.GetActiveScene().isDirty?"true":"false")+"}");
                EditorApplication.Exit(success?0:1);
            }
        }
    }
}
