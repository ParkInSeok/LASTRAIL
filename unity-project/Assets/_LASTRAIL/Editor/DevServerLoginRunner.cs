using System;
using System.IO;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastRail.Editor
{
    [InitializeOnLoad]
    public static class DevServerLoginRunner
    {
        const string Pending="LastRail.DevServerLogin.Pending";
        const string RestoreBoot="LastRail.DevServerLogin.RestoreBoot";
        const string Output="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/formation-plan-v02/dev/real-login-03";
        static bool reloadLocked;
        static DevServerLoginRunner(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Start()
        {
            if(Environment.GetEnvironmentVariable("LASTRAIL_DEV_LOGIN_QA")!="1"||EditorApplication.isPlayingOrWillChangePlaymode||Directory.Exists(Output))throw new InvalidOperationException("Fresh dedicated DEV verification Editor required.");
            EditorSceneManager.OpenScene("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity");
            var boot=UnityEngine.Object.FindFirstObjectByType<ResourceBootstrapController>();
            if(boot==null)throw new InvalidOperationException("Bootstrap scene missing.");
            SessionState.SetBool(RestoreBoot,boot.enabled);boot.enabled=false;SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Pending,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                // Preserve one-use DEV hooks and running probes until this QA session leaves Play mode.
                EditorApplication.LockReloadAssemblies();reloadLocked=true;
                try{new GameObject("Actual DEV login verification").AddComponent<DevServerLoginProbe>().Begin(UnityEngine.Object.FindFirstObjectByType<ResourceBootstrapController>(),Output);}
                catch{EditorApplication.isPlaying=false;throw;}
            }
            if(state==PlayModeStateChange.ExitingPlayMode&&reloadLocked){reloadLocked=false;EditorApplication.UnlockReloadAssemblies();}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Pending,false);var boot=UnityEngine.Object.FindFirstObjectByType<ResourceBootstrapController>();if(boot!=null)boot.enabled=SessionState.GetBool(RestoreBoot,true);ResourceBootstrapController.EditorVerificationBackend=null;ResourceBootstrapController.EditorCaptureAuthentication=null;if(Environment.GetEnvironmentVariable("LASTRAIL_DEV_LOGIN_KEEP_EDITOR")!="1")EditorApplication.Exit(0);}
        }
    }
}
