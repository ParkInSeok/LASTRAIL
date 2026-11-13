#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace LastRail.Resources
{
    internal sealed class EditorLoopbackContentFixture
    {
        internal string ConfigUrl { get; }
        internal string TrustedCatalogRoot { get; }
        internal string RuntimeSettingsPath { get; }
        internal string StateDirectory { get; }
        internal string InstalledVersionCode { get; }
        internal const string DevProbeRoot = "https://dev-content.lastrail.app/windowsqa/releases/pc-resources-20261111-v02/";
        internal const string FormationProbeRoot = "https://dev-content.lastrail.app/windowsqa/releases/pc-formation-20261112-v02/";

        internal static EditorLoopbackContentFixture CreateDevProbe(string configUrl, string runtimeSettingsPath,
            string stateDirectory, string installedVersionCode)
        {
            if (configUrl != "http://127.0.0.1:18285/client-config.json")
                throw new InvalidOperationException("DEV probe requires its explicit TEST_ONLY loopback admission endpoint.");
            // Reuse all local-path and state isolation checks without widening the local fixture contract.
            var local = Create(configUrl, "http://127.0.0.1:18285/LOCAL_TEST/WindowsQA/",
                runtimeSettingsPath, stateDirectory, installedVersionCode);
            var project = Path.GetFullPath("D:/Project/LASTRAIL") + Path.DirectorySeparatorChar;
            if (!local.RuntimeSettingsPath.StartsWith(project, StringComparison.OrdinalIgnoreCase) ||
                !local.StateDirectory.StartsWith(project, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DEV probe files must remain in the active D project.");
            return new EditorLoopbackContentFixture(configUrl, DevProbeRoot, local.RuntimeSettingsPath,
                local.StateDirectory, installedVersionCode);
        }

        private EditorLoopbackContentFixture(string config, string root, string settings, string state, string code)
        {
            ConfigUrl = config; TrustedCatalogRoot = root; RuntimeSettingsPath = settings;
            StateDirectory = state; InstalledVersionCode = code;
        }

        internal static EditorLoopbackContentFixture CreateDevServerProbe(string settings, string state)
        {
            var validated = CreateDevProbe("http://127.0.0.1:18285/client-config.json", settings, state, "45");
            return new EditorLoopbackContentFixture(LastRail.Client.ClientEnvironmentBootstrap.DevClientConfigUrl,
                FormationProbeRoot, validated.RuntimeSettingsPath, validated.StateDirectory, "45");
        }

        internal static EditorLoopbackContentFixture Create(string configUrl, string trustedCatalogRoot,
            string runtimeSettingsPath, string stateDirectory, string installedVersionCode)
        {
            var source = LoopbackUri(configUrl);
            var root = LoopbackUri(trustedCatalogRoot);
            if (source.Port != root.Port || !Regex.IsMatch(root.AbsolutePath, @"\A/LOCAL_TEST/[A-Za-z0-9_-]+/\z") ||
                source.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
                throw new InvalidOperationException("Fixture requires one loopback authority and an explicit LOCAL_TEST target root.");
            ClientAdmissionContract.VersionCode(installedVersionCode);
            var settings = LocalPath(runtimeSettingsPath);
            var state = LocalPath(stateDirectory);
            if (Path.GetFileName(settings) != "settings.json" ||
                state == Path.GetPathRoot(state) || Overlaps(state, LocalPath(Application.persistentDataPath)) ||
                Overlaps(state, Path.GetDirectoryName(settings)))
                throw new InvalidOperationException("Fixture settings and state must be isolated from production storage and each other.");
            return new EditorLoopbackContentFixture(configUrl, trustedCatalogRoot, settings, state, installedVersionCode);
        }

        internal UnityWebRequest CreateAdmissionRequest() => ClientAdmissionContract.CreateFixtureRequest(this);
        internal ClientContentGate ParseAdmission(string json) => ClientAdmissionContract.ParseFixture(json, this);
        internal string VerifiedCatalogPath(string catalogUrl, string catalogHash, bool jsonCatalogEnabled) =>
            ApprovedCatalogSafety.FixtureCatalogPath(this, catalogUrl, catalogHash, jsonCatalogEnabled);
        internal IResourceLocation[] BuildRemoteBundlePlan(IResourceLocation[] roots, Func<IResourceLocation, string> transform) =>
            ApprovedCatalogSafety.BuildFixturePlan(this, roots, transform);

        internal void ValidateContentUrl(string value)
        {
            if (TrustedCatalogRoot == DevProbeRoot || TrustedCatalogRoot == FormationProbeRoot)
            {
                if (value == null || !value.StartsWith(TrustedCatalogRoot, StringComparison.Ordinal) ||
                    !Regex.IsMatch(value.Substring(TrustedCatalogRoot.Length), @"\A[A-Za-z0-9_-][A-Za-z0-9._&/-]*\z") ||
                    value.IndexOf("//", TrustedCatalogRoot.Length, StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("DEV content URL must stay inside the fixed Windows QA release.");
                foreach (var segment in value.Substring(TrustedCatalogRoot.Length).Split('/'))
                    if (segment == "." || segment == ".." || segment.Length == 0)
                        throw new InvalidOperationException("DEV content URL contains invalid path segments.");
                return;
            }
            var uri = LoopbackUri(value);
            if (uri.Port != new Uri(TrustedCatalogRoot).Port || !value.StartsWith(TrustedCatalogRoot, StringComparison.Ordinal) ||
                value.Length == TrustedCatalogRoot.Length)
                throw new InvalidOperationException("Fixture content URL is outside the LOCAL_TEST release root.");
        }

        internal void ValidateStateDirectory()
        {
            if (Overlaps(StateDirectory, LocalPath(Application.persistentDataPath)))
                throw new InvalidOperationException("Fixture state overlaps production storage.");
            RejectReparsePoints(StateDirectory);
            RejectReparsePoints(Path.Combine(StateDirectory, "pending-content-release.json"));
            RejectReparsePoints(Path.Combine(StateDirectory, "approved-catalogs"));
        }

        // The package has no public static path overload. Read its actual singleton and
        // invoke the installed package operation; never replace its state or providers.
        internal AsyncOperationHandle<IResourceLocator> InitializeRuntime(out object instance)
        {
            RejectReparsePoints(RuntimeSettingsPath);
            if (!File.Exists(RuntimeSettingsPath)) throw new InvalidOperationException("Built fixture settings are missing.");
            instance = CurrentInstance();
            var started = instance.GetType().GetField("hasStartedInitialization", BindingFlags.Instance | BindingFlags.NonPublic);
            var initialize = instance.GetType().GetMethod("InitializeAsync", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(string), typeof(string), typeof(bool) }, null);
            if (started == null || initialize == null)
                throw new InvalidOperationException("Installed Addressables package does not support the fixture path adapter.");
            if ((bool)started.GetValue(instance))
                throw new InvalidOperationException("Addressables was already initialized without this fixture's path ownership.");
            try
            {
                return (AsyncOperationHandle<IResourceLocator>)initialize.Invoke(instance,
                    new object[] { RuntimeSettingsPath, null, false });
            }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        internal void RequireSameRuntime(object instance)
        {
            if (instance == null || !ReferenceEquals(instance, CurrentInstance()))
                throw new InvalidOperationException("The fixture's Addressables session has changed.");
        }

        private static object CurrentInstance()
        {
            var property = typeof(Addressables).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (property == null) throw new InvalidOperationException("Installed Addressables singleton is unavailable.");
            return property.GetValue(null) ?? throw new InvalidOperationException("Addressables singleton is unavailable.");
        }

        private static Uri LoopbackUri(string value)
        {
            var match = Regex.Match(value ?? "", @"\Ahttp://127\.0\.0\.1:([1-9][0-9]{0,4})(/[A-Za-z0-9._/-]*)\z");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
                port > 65535 || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("Fixture URLs require canonical explicit-port IPv4 loopback HTTP.");
            var path = match.Groups[2].Value;
            foreach (var segment in path.Split('/'))
                if (segment == "." || segment == "..") throw new InvalidOperationException("Fixture URL contains dot segments.");
            if (path.Contains("//")) throw new InvalidOperationException("Fixture URL contains empty path segments.");
            return uri;
        }

        private static string LocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path != path.Trim() || !Path.IsPathRooted(path) ||
                path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal) ||
                path.Contains("://") || path.Contains("{") || path.Contains("}"))
                throw new InvalidOperationException("Fixture paths must be explicit absolute local paths.");
            var root = Path.GetPathRoot(path);
            if (root.Length < 3 || (!root.EndsWith("\\", StringComparison.Ordinal) && !root.EndsWith("/", StringComparison.Ordinal)))
                throw new InvalidOperationException("Fixture paths must include an absolute local volume root.");
            var full = Path.GetFullPath(path);
            if (full.IndexOf(':', 2) >= 0) throw new InvalidOperationException("Fixture path contains an alternate stream.");
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool Overlaps(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase) ||
            left.StartsWith(right.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            right.StartsWith(left.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        private static void RejectReparsePoints(string path)
        {
            for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Fixture paths cannot traverse reparse points.");
        }
    }
}
#endif
