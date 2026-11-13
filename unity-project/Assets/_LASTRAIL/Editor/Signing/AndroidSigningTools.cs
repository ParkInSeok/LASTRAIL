#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using UnityEditor;

namespace LastRail.Editor.Signing
{
    public static class AndroidSigningTools
    {
        public const string SigningRoot = "D:/Project/LASTRAIL-Signing";
        private static string AndroidRoot => Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
        private static string Java(string name) => Path.Combine(AndroidRoot, "OpenJDK", "bin", name + ".exe");
        private static string BuildTool(string name) => Path.Combine(AndroidRoot, "SDK", "build-tools", "36.0.0", name);
        public static string KeyPath(string server) => Path.Combine(SigningRoot, server == "DEV" ? "lastrail-dev.keystore" : "lastrail-prd-upload.keystore");
        public static string Alias(string server) => server == "DEV" ? "lastrail-dev" : "lastrail-prd-upload";

        public static string VerifyKey(string server, string password)
        {
            AndroidBuildPolicy.RequireServer(server);
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("A transient signing password is required.");
            var path = Path.GetFullPath(KeyPath(server));
            if (!File.Exists(path)) throw new FileNotFoundException("Provision the approved outside-repository signing key first.");
            for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)); directory != null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Signing paths must not be reparse points.");
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Signing key must not be a link.");
            var pem = Run(Java("keytool"), new[] { "-exportcert", "-rfc", "-keystore", path, "-alias", Alias(server),
                "-storepass:env", "LASTRAIL_KEYTOOL_PASSWORD" }, password);
            var encoded = Regex.Replace(pem, "-----BEGIN CERTIFICATE-----|-----END CERTIFICATE-----|\\s", "");
            using (var certificate = new X509Certificate2(Convert.FromBase64String(encoded)))
            using (var sha = SHA256.Create())
            {
                if (certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow) throw new InvalidOperationException("Signing certificate has expired.");
                return BitConverter.ToString(sha.ComputeHash(certificate.RawData)).Replace("-", "");
            }
        }

        public static void VerifyArtifact(string path, BuildRequest request, int code, string expectedSha256)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0) throw new InvalidDataException("Build artifact is missing or empty.");
            if (request.Format == "apk")
            {
                var identity = Run(BuildTool("aapt.exe"), new[] { "dump", "badging", path });
                var package = Regex.Match(identity, @"(?m)^package: name='([^']+)' versionCode='([0-9]+)' versionName='([^']*)'");
                if (!package.Success || package.Groups[1].Value != request.Package || package.Groups[2].Value != code.ToString(CultureInfo.InvariantCulture)
                    || package.Groups[3].Value != request.Version) throw new InvalidDataException("APK identity does not match the requested build.");
                var signature = Run(Java("java"), new[] { "-jar", BuildTool("lib/apksigner.jar"), "verify", "--verbose", "--print-certs", path });
                var certificates = Regex.Matches(signature, @"(?m)^Signer #[0-9]+ certificate SHA-256 digest: ([0-9a-fA-F]+)\s*$");
                if (certificates.Count != 1 || !string.Equals(certificates[0].Groups[1].Value, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("APK signature does not match the selected key.");
            }
            else
            {
                var bundletool = Path.Combine(AndroidRoot, "Tools", "bundletool-all-1.17.2.jar");
                foreach (var field in new Dictionary<string, string> { { "package", request.Package }, { "android:versionCode", code.ToString(CultureInfo.InvariantCulture) }, { "android:versionName", request.Version } })
                {
                    var value = Run(Java("java"), new[] { "-jar", bundletool, "dump", "manifest", "--bundle=" + path, "--xpath=/manifest/@" + field.Key }).Trim();
                    if (value != field.Value) throw new InvalidDataException("AAB identity does not match the requested build.");
                }
                var source = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), "Tools", "VerifySignedBundle.java");
                Run(Java("java"), new[] { source, path, expectedSha256 });
            }
        }

        private static string Quote(string value)
        {
            if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) throw new ArgumentException("Invalid process argument.");
            return "\"" + Regex.Replace(Regex.Replace(value, "(\\\\*)\"", "$1$1\\\""), "(\\\\+)$", "$1$1") + "\"";
        }

        private static string Run(string executable, IEnumerable<string> arguments, string password = null)
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("Required Android SDK verification tool is missing.");
            var info = new ProcessStartInfo(executable, string.Join(" ", arguments.Select(Quote)))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            if (password != null) info.EnvironmentVariables["LASTRAIL_KEYTOOL_PASSWORD"] = password;
            try
            {
                using (var process = Process.Start(info))
                {
                    var output = process.StandardOutput.ReadToEndAsync();
                    var errors = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(120000))
                    {
                        process.Kill();
                        process.WaitForExit();
                        throw new InvalidOperationException("Android verification tool timed out.");
                    }
                    if (process.ExitCode != 0) throw new InvalidOperationException("Android verification tool failed; raw output is not logged.");
                    errors.GetAwaiter().GetResult();
                    return output.GetAwaiter().GetResult();
                }
            }
            finally { info.EnvironmentVariables.Remove("LASTRAIL_KEYTOOL_PASSWORD"); }
        }
    }
}
#endif
