#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Client;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEngine;

namespace LastRail.Editor.Signing
{
    public static class AndroidSigningVerification
    {
        private static string Root => Path.Combine(Path.GetDirectoryName(Application.dataPath), "QA", "Reports", "AndroidSigningBuild-20260908");
        private static readonly NamedBuildTarget Target = NamedBuildTarget.Android;
        private static readonly Type Builder = typeof(AndroidSignedBuild);

        private static object Call(string name, params object[] values)
        {
            return Builder.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, values);
        }

        public static void VerifyRestoration()
        {
            var journal = (string)Builder.GetProperty("Journal", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (File.Exists(journal)) throw new InvalidOperationException("Do not overwrite a real pending build with verification.");
            var request = BuildRequest.Parse(new[] { "server=DEV", "version=0.1.1" });
            var baseline = Call("Capture", request);
            var expected = JsonUtility.ToJson(baseline);
            foreach (var scenario in new[] { "cancel", "interrupted", "exception" })
            {
                var pending = Call("Capture", request);
                if (scenario == "interrupted")
                {
                    pending.GetType().GetField("phase").SetValue(pending, "Building");
                    pending.GetType().GetField("settingsApplied").SetValue(pending, true);
                }
                Call("Persist", pending, false);
                try
                {
                    if (scenario == "interrupted")
                    {
                    PlayerSettings.SetApplicationIdentifier(Target, "app.lastrail.verification");
                    PlayerSettings.bundleVersion = "0.0.0-verification";
                    PlayerSettings.Android.bundleVersionCode = 123;
                    PlayerSettings.Android.keystoreName = "verification-not-a-key";
                    PlayerSettings.Android.keyaliasName = "verification-not-an-alias";
                    PlayerSettings.Android.useCustomKeystore = !PlayerSettings.Android.useCustomKeystore;
                    EditorUserBuildSettings.buildAppBundle = !EditorUserBuildSettings.buildAppBundle;
                    EditorUserBuildSettings.development = !EditorUserBuildSettings.development;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = !EditorUserBuildSettings.exportAsGoogleAndroidProject;
                    }
                    if (scenario == "cancel") AndroidSignedBuild.Cancel();
                    else if (scenario == "interrupted") Call("RecoverInterrupted");
                    else
                    {
                        var rejected = false;
                        try { AndroidSignedBuild.Build(request, null); }
                        catch (Exception) { rejected = true; }
                        if (!rejected) throw new Exception("Missing credentials must reject the fixture without building.");
                    }
                    if (File.Exists(journal) || JsonUtility.ToJson(Call("Capture", request)) != expected)
                        throw new Exception("Settings were not restored: " + scenario);
                    if (PlayerSettings.Android.keystorePass != "" || PlayerSettings.Android.keyaliasPass != "")
                        throw new Exception("Signing password fields must be cleared.");
                    Debug.Log("SIGNING_VERIFICATION_PASS " + scenario);
                }
                finally
                {
                    if (File.Exists(journal)) Call("Restore", pending);
                }
            }
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "restoration.txt"), "PASS cancel, interrupted recovery, preflight exception, password clearing; no BuildPlayer call.\n");
        }

        [Serializable]
        private sealed class ReloadFixture
        {
            public string original, selected, expectedEnvironment;
        }

        public static void BeginReload()
        {
            var path = Path.Combine(Root, "reload-fixture.json");
            if (File.Exists(path)) throw new InvalidOperationException("A reload fixture already exists; recover it first.");
            var original = PlayerSettings.GetScriptingDefineSymbols(Target);
            var next = ClientEnvironmentBootstrap.CurrentEnvironment == ClientEnvironment.Dev ? "PRD" : "DEV";
            var fixture = new ReloadFixture { original = original, selected = AndroidBuildPolicy.SelectDefines(original, next), expectedEnvironment = next };
            Directory.CreateDirectory(Root);
            File.WriteAllText(path, JsonUtility.ToJson(fixture));
            var pending = Call("Capture", BuildRequest.Parse(new[] { "server=" + next, "version=0.1.1" }));
            Call("Persist", pending, false);
            PlayerSettings.SetScriptingDefineSymbols(Target, fixture.selected);
            Debug.Log("SIGNING_RELOAD_BEGIN " + next);
        }

        public static void FinishReload()
        {
            var fixture = JsonUtility.FromJson<ReloadFixture>(File.ReadAllText(Path.Combine(Root, "reload-fixture.json")));
            try
            {
                var pending = Call("Read");
                if ((string)pending.GetType().GetField("phase").GetValue(pending) != "Prepared")
                    throw new Exception("Batch prepare must persist without automatic build or cancellation.");
                var expected = fixture.expectedEnvironment == "DEV" ? ClientEnvironment.Dev : ClientEnvironment.Prd;
                if (ClientEnvironmentBootstrap.CurrentEnvironment != expected || PlayerSettings.GetScriptingDefineSymbols(Target) != fixture.selected)
                    throw new Exception("Actual compiled environment does not match selected symbols.");
                File.WriteAllText(Path.Combine(Root, "reload-selected.txt"), "PASS actual compiled environment " + fixture.expectedEnvironment + "\n");
            }
            finally { AndroidSignedBuild.Cancel(); }
        }

        public static void VerifyReloadRestored()
        {
            var path = Path.Combine(Root, "reload-fixture.json");
            var fixture = JsonUtility.FromJson<ReloadFixture>(File.ReadAllText(path));
            var symbols = fixture.original.Split(';');
            var expected = symbols.Contains("LASTRAIL_ENV_DEV") ? ClientEnvironment.Dev : symbols.Contains("LASTRAIL_ENV_PRD") ? ClientEnvironment.Prd : ClientEnvironment.Unconfigured;
            if (PlayerSettings.GetScriptingDefineSymbols(Target) != fixture.original || ClientEnvironmentBootstrap.CurrentEnvironment != expected)
                throw new Exception("Restored environment did not compile correctly.");
            File.WriteAllText(Path.Combine(Root, "reload-restored.txt"), "PASS restored symbols and actual compiled environment; no automatic build resume.\n");
            File.WriteAllText(Path.Combine(Root, "reload-fixture.completed.json"), JsonUtility.ToJson(fixture));
            File.Delete(path);
        }
    }
}
#endif
