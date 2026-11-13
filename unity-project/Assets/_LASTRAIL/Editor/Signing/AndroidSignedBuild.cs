#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Client;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastRail.Editor.Signing
{
    [InitializeOnLoad]
    public static class AndroidSignedBuild
    {
        private static string Project => Path.GetDirectoryName(Application.dataPath);
        private static string Journal => Path.Combine(Project, "Library", "LastRailSigningBuild", "pending.json");
        private static BuildRegistry Registry => new BuildRegistry(Path.Combine(AndroidSigningTools.SigningRoot, "build-state"));
        private static readonly NamedBuildTarget Target = NamedBuildTarget.Android;

        [Serializable]
        private sealed class Pending
        {
            public string phase, server, version, format, code, stage, output;
            public string defines, package, originalVersion, keyPath, alias, profile;
            public int originalCode;
            public bool customKey, development, bundle, export;
            public bool settingsApplied;
        }

        static AndroidSignedBuild()
        {
            // Reload never starts a build. A crashed build can only recover settings.
            EditorApplication.delayCall += RecoverInterrupted;
            EditorApplication.quitting += () => { if (!Application.isBatchMode) Cancel(); };
        }

        public static void Reconcile(string server, int successful, int published)
        {
            if (File.Exists(Journal)) throw new InvalidOperationException("Finish or cancel the prepared build before baseline reconciliation.");
            Registry.Reconcile(server, successful, published);
        }

        public static void PrepareFromCommandLine() => Prepare(CommandRequest());
        public static void BuildFromCommandLine()
        {
            try { Build(CommandRequest(), Environment.GetEnvironmentVariable("LASTRAIL_SIGNING_PASSWORD")); }
            finally { Environment.SetEnvironmentVariable("LASTRAIL_SIGNING_PASSWORD", null); }
        }

        private static BuildRequest CommandRequest()
        {
            var arguments = Environment.GetCommandLineArgs();
            var separator = Array.IndexOf(arguments, "--");
            if (separator < 0) throw new ArgumentException("Place explicit build parameters after --.");
            return BuildRequest.Parse(arguments.Skip(separator + 1).ToArray());
        }

        public static void Prepare(BuildRequest request)
        {
            RequireIdle();
            if (File.Exists(Journal)) throw new InvalidOperationException("A build is already prepared. Resume or cancel it first.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch to Android and finish compilation before preparing the build.");
            var selectedDefines = AndroidBuildPolicy.SelectDefines(PlayerSettings.GetScriptingDefineSymbols(Target), request.Server);
            Preflight(request);
            using (var check = Registry.Reserve(request))
                RequireFreshOutput(request.OutputDirectory(Project, check.Code));
            var pending = Capture(request);
            Persist(pending, false);
            try { PlayerSettings.SetScriptingDefineSymbols(Target, selectedDefines); }
            catch { Restore(pending); throw; }
        }

        private static Pending Capture(BuildRequest request)
        {
            return new Pending {
                phase = "Prepared", server = request.Server, version = request.Version, format = request.Format,
                code = request.VersionCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                defines = PlayerSettings.GetScriptingDefineSymbols(Target), package = PlayerSettings.GetApplicationIdentifier(Target),
                originalVersion = PlayerSettings.bundleVersion, originalCode = PlayerSettings.Android.bundleVersionCode,
                keyPath = PlayerSettings.Android.keystoreName, alias = PlayerSettings.Android.keyaliasName,
                customKey = PlayerSettings.Android.useCustomKeystore, development = EditorUserBuildSettings.development,
                bundle = EditorUserBuildSettings.buildAppBundle, export = EditorUserBuildSettings.exportAsGoogleAndroidProject,
                profile = AddressableAssetSettingsDefaultObject.Settings.activeProfileId
            };
        }

        public static string Build(BuildRequest request, string password, string contentCandidatePath = null)
        {
            RequireIdle();
            var pending = Read();
            string completedOutput = null;
            if (pending.phase != "Prepared" || pending.server != request.Server || pending.version != request.Version
                || pending.format != request.Format || pending.code != (request.VersionCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""))
                throw new InvalidOperationException("Build arguments must exactly match the prepared request.");
            try
            {
                Preflight(request);
                var expectedEnvironment = request.Server == "DEV" ? ClientEnvironment.Dev : ClientEnvironment.Prd;
                if (ClientEnvironmentBootstrap.CurrentEnvironment != expectedEnvironment
                    || PlayerSettings.GetScriptingDefineSymbols(Target) != AndroidBuildPolicy.SelectDefines(pending.defines, request.Server))
                    throw new InvalidOperationException("Selected environment must finish recompiling before build. Prepare again after cancellation.");
                var fingerprint = AndroidSigningTools.VerifyKey(request.Server, password);
                var originalDefines = pending.defines;
                pending = Capture(request);
                pending.defines = originalDefines;
                using (var lease = Registry.Reserve(request))
                {
                    if (contentCandidatePath != null) DevPlayPublisher.ValidateCandidate(request, lease.Code, contentCandidatePath);
                    pending.output = request.OutputDirectory(Project, lease.Code);
                    RequireFreshOutput(pending.output);
                    pending.stage = Path.Combine(Project, "Builds", "Android", ".staging", Guid.NewGuid().ToString("N"));
                    pending.phase = "Building";
                    pending.settingsApplied = true;
                    Persist(pending, true);
                    Directory.CreateDirectory(pending.stage);
                    PlayerSettings.SetApplicationIdentifier(Target, request.Package);
                    PlayerSettings.bundleVersion = request.Version;
                    PlayerSettings.Android.bundleVersionCode = lease.Code;
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = AndroidSigningTools.KeyPath(request.Server);
                    PlayerSettings.Android.keyaliasName = AndroidSigningTools.Alias(request.Server);
                    PlayerSettings.Android.keystorePass = password;
                    PlayerSettings.Android.keyaliasPass = password;
                    EditorUserBuildSettings.buildAppBundle = request.Format == "aab";
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                    EditorUserBuildSettings.development = request.Server == "DEV";
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    settings.activeProfileId = settings.profileSettings.GetProfileId(request.Server);
                    var artifact = Path.Combine(pending.stage, request.ArtifactName(lease.Code));
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                        scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                        locationPathName = artifact, target = BuildTarget.Android,
                        options = BuildOptions.StrictMode | (request.Server == "DEV" ? BuildOptions.Development : BuildOptions.None)
                    });
                    if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build did not succeed: " + report.summary.result);
                    AndroidSigningTools.VerifyArtifact(artifact, request, lease.Code, fingerprint);
                    File.WriteAllText(Path.Combine(pending.stage, "build-report.json"), JsonUtility.ToJson(new PublicReport {
                        environment = request.Server, version = request.Version, versionCode = lease.Code,
                        package = request.Package, certificateSha256 = fingerprint, format = request.Format
                    }, true));
                    // Commit after verified artifact, before promotion; a failed promotion never permits code reuse.
                    lease.CommitVerifiedSuccess();
                    Directory.CreateDirectory(Path.GetDirectoryName(pending.output));
                    Directory.Move(pending.stage, pending.output);
                    pending.stage = null;
                    completedOutput = pending.output;
                    Debug.Log("LASTRAIL verified signed artifact: " + Path.Combine(pending.output, request.ArtifactName(lease.Code)));
                }
            }
            finally
            {
                password = null;
                Restore(pending);
            }
            return completedOutput;
        }

        [Serializable]
        private sealed class PublicReport
        {
            public string environment, version, package, certificateSha256, format;
            public int versionCode;
        }

        private static void RequireIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Wait for an idle Android editor before continuing.");
        }

        private static void Preflight(BuildRequest request)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) throw new InvalidOperationException("Android must be the active target.");
            if (!File.Exists(AndroidSigningTools.KeyPath(request.Server))) throw new FileNotFoundException("The selected signing key has not been provisioned.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var id = settings?.profileSettings.GetProfileId(request.Server);
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Matching Addressables profile is missing.");
            var remote = settings.profileSettings.GetValueByName(id, AddressableAssetSettings.kRemoteLoadPath);
            AndroidBuildPolicy.ValidateContentRoot(request.Server, remote);
            if (string.IsNullOrWhiteSpace(remote) || remote.Contains("UNCONFIGURED")
                || !Uri.TryCreate(remote, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                throw new InvalidOperationException("The selected content profile requires an approved HTTPS remote root. No automatic endpoint rewrite is permitted.");
            var otherId = settings.profileSettings.GetProfileId(request.Server == "DEV" ? "PRD" : "DEV");
            if (!string.IsNullOrEmpty(otherId) && remote == settings.profileSettings.GetValueByName(otherId, AddressableAssetSettings.kRemoteLoadPath))
                throw new InvalidOperationException("DEV and PRD content roots must not be identical.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            if (scenes.Length == 0 || scenes.Any(s => !File.Exists(s.path) || s.path.Contains("Smoke")))
                throw new InvalidOperationException("Explicit enabled shipping scenes are required; smoke scenes are excluded.");
        }

        private static void RequireFreshOutput(string path)
        {
            if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Output already exists; no overwrite is allowed.");
            for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)); directory != null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Output ancestors must not be reparse points.");
        }

        public static void Cancel()
        {
            if (File.Exists(Journal) && !BuildPipeline.isBuildingPlayer) Restore(Read());
        }

        private static void RecoverInterrupted()
        {
            if (!File.Exists(Journal)) return;
            var pending = Read();
            if (pending.phase != "Prepared") Restore(pending);
        }

        private static Pending Read()
        {
            if (!File.Exists(Journal)) throw new InvalidOperationException("Prepare the build first.");
            var pending = JsonUtility.FromJson<Pending>(File.ReadAllText(Journal));
            if (pending == null || (pending.phase != "Prepared" && pending.phase != "Building" && pending.phase != "Restoring"))
                throw new InvalidDataException("Build recovery journal is invalid. Manual reconciliation is required.");
            AndroidBuildPolicy.RequireServer(pending.server);
            return pending;
        }

        private static void Restore(Pending pending)
        {
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            pending.phase = "Restoring";
            Persist(pending, true);
            if (pending.settingsApplied)
            {
                PlayerSettings.SetApplicationIdentifier(Target, pending.package);
                PlayerSettings.bundleVersion = pending.originalVersion;
                PlayerSettings.Android.bundleVersionCode = pending.originalCode;
                PlayerSettings.Android.useCustomKeystore = pending.customKey;
                PlayerSettings.Android.keystoreName = pending.keyPath;
                PlayerSettings.Android.keyaliasName = pending.alias;
                EditorUserBuildSettings.development = pending.development;
                EditorUserBuildSettings.buildAppBundle = pending.bundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = pending.export;
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null) settings.activeProfileId = pending.profile;
            }
            PlayerSettings.SetScriptingDefineSymbols(Target, AndroidBuildPolicy.RestoreDefines(
                PlayerSettings.GetScriptingDefineSymbols(Target), pending.defines, pending.server));
            if (!string.IsNullOrEmpty(pending.stage) && Directory.Exists(pending.stage))
            {
                var stagingRoot = Path.GetFullPath(Path.Combine(Project, "Builds", "Android", ".staging")) + Path.DirectorySeparatorChar;
                var source = Path.GetFullPath(pending.stage);
                if (!source.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid recovery staging path.");
                var quarantine = Path.Combine(Project, "Builds", "Android", ".failed", Path.GetFileName(source));
                Directory.CreateDirectory(Path.GetDirectoryName(quarantine));
                Directory.Move(source, quarantine);
            }
            File.Delete(Journal);
        }

        private static void Persist(Pending pending, bool replace)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Journal));
            var temporary = Journal + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(pending, true));
                    writer.Flush();
                    stream.Flush(true);
                }
                if (replace) File.Replace(temporary, Journal, null);
                else File.Move(temporary, Journal);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
#endif
