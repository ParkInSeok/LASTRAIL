using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LastRail.Editor.Signing
{
    public static class AndroidBuildPolicy
    {
        public const int MaximumCode = 2100000000;

        public static void RequireServer(string server)
        {
            if (server != "DEV" && server != "PRD") throw new ArgumentException("server must be DEV or PRD.");
        }

        public static string SelectDefines(string existing, string server)
        {
            RequireServer(server);
            var symbols = (existing ?? "").Split(';').Select(s => s.Trim()).Where(s => s.Length != 0).Distinct().ToList();
            if (symbols.Contains("LASTRAIL_ENV_DEV") && symbols.Contains("LASTRAIL_ENV_PRD"))
                throw new InvalidOperationException("Conflicting environment symbols must be resolved explicitly.");
            if (symbols.Any(s => s.StartsWith("LASTRAIL_ENV_", StringComparison.Ordinal)
                && s != "LASTRAIL_ENV_DEV" && s != "LASTRAIL_ENV_PRD"))
                throw new InvalidOperationException("Unsupported environment symbol.");
            symbols.RemoveAll(s => s == "LASTRAIL_ENV_DEV" || s == "LASTRAIL_ENV_PRD");
            symbols.Add("LASTRAIL_ENV_" + server);
            return string.Join(";", symbols);
        }

        public static string RestoreDefines(string current, string original, string selectedServer)
        {
            if (current == SelectDefines(original, selectedServer)) return original;
            var features = (current ?? "").Split(';').Where(s => s.Length > 0 && !s.StartsWith("LASTRAIL_ENV_", StringComparison.Ordinal));
            var originalEnvironment = (original ?? "").Split(';').Where(s => s.StartsWith("LASTRAIL_ENV_", StringComparison.Ordinal));
            return string.Join(";", features.Concat(originalEnvironment).Distinct());
        }

        public static void ValidateContentRoot(string server, string remote)
        {
            RequireServer(server);
            // Mirrors the approved DEV authority in DevAuthorizedContentClient.
            // No PRD authority has been approved in the source policies yet.
            if (server != "DEV") throw new InvalidOperationException("PRD content authority is not configured in the approved allowlist.");
            const string root = "https://dev-content.lastrail.app/android/releases/";
            if (remote == null || !remote.StartsWith(root, StringComparison.Ordinal)
                || !Regex.IsMatch(remote.Substring(root.Length), @"\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}/\z"))
                throw new InvalidOperationException("Content profile must match the approved environment authority and immutable release namespace.");
        }
    }

    public sealed class BuildRequest
    {
        public string Server { get; private set; }
        public string Version { get; private set; }
        public string Package => Server == "DEV" ? "app.lastrail.game.dev" : "app.lastrail.game";
        public string Format { get; private set; }
        public int? VersionCode { get; private set; }

        public static BuildRequest Parse(string[] arguments)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var argument in arguments)
            {
                var pair = argument.Split(new[] { '=' }, 2);
                if (pair.Length != 2 || !new[] { "server", "version", "versionCode", "format" }.Contains(pair[0])
                    || values.ContainsKey(pair[0])) throw new ArgumentException("Unknown, duplicate or malformed build argument.");
                values.Add(pair[0], pair[1]);
            }
            if (!values.TryGetValue("server", out var server) || !values.TryGetValue("version", out var version))
                throw new ArgumentException("Explicit server and version are required.");
            AndroidBuildPolicy.RequireServer(server);
            if (!Regex.IsMatch(version, @"\A[0-9A-Za-z][0-9A-Za-z._+-]{0,63}\z"))
                throw new ArgumentException("Version must be a safe explicit display version, at most 64 characters.");
            var format = values.TryGetValue("format", out var inputFormat) ? inputFormat : server == "DEV" ? "apk" : "aab";
            if (format != "apk" && format != "aab") throw new ArgumentException("format must be apk or aab.");
            int? code = null;
            if (values.TryGetValue("versionCode", out var inputCode))
            {
                if (!Regex.IsMatch(inputCode, @"\A[0-9]+\z")
                    || !int.TryParse(inputCode, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    || parsed < 1 || parsed > AndroidBuildPolicy.MaximumCode)
                    throw new ArgumentException("versionCode must be an integer between 1 and 2100000000.");
                code = parsed;
            }
            return new BuildRequest { Server = server, Version = version, Format = format, VersionCode = code };
        }

        public string OutputDirectory(string project, int code) => Path.Combine(project, "Builds", "Android", Server, Version + "_" + code);
        public string ArtifactName(int code) => "LASTRAIL_" + Server + "_" + Version + "_" + code + "." + Format;
    }

    public sealed class BuildRegistry
    {
        private readonly string root;
        public BuildRegistry(string root) { this.root = Path.GetFullPath(root); }

        private FileStream Lock(string server)
        {
            AndroidBuildPolicy.RequireServer(server);
            Directory.CreateDirectory(root);
            return new FileStream(Path.Combine(root, server + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public void Reconcile(string server, int successful, int published)
        {
            if (successful < 0 || published < 0 || successful > AndroidBuildPolicy.MaximumCode || published > AndroidBuildPolicy.MaximumCode)
                throw new ArgumentException("Confirmed baseline codes must be in range.");
            using (Lock(server))
            {
                if (File.Exists(ReservationPath(server)))
                    throw new InvalidOperationException("Resolve the interrupted reservation before reconciling history.");
                if (File.Exists(StatePath(server)))
                {
                    var previous = Read(server);
                    if (successful < previous.Item1 || published < previous.Item2)
                        throw new InvalidOperationException("Confirmed baselines must never regress.");
                }
                Write(server, successful, published);
            }
        }

        public BuildLease Reserve(BuildRequest request)
        {
            var held = Lock(request.Server);
            try
            {
                var reservation = ReservationPath(request.Server);
                if (File.Exists(reservation)) throw new InvalidOperationException("An interrupted reservation needs explicit reconciliation; automatic reuse is forbidden.");
                var state = Read(request.Server);
                var floor = Math.Max(state.Item1, state.Item2);
                var code = request.VersionCode ?? checked(floor + 1);
                if (code <= floor || code > AndroidBuildPolicy.MaximumCode)
                    throw new InvalidOperationException("Requested versionCode conflicts with confirmed history or exceeds the platform maximum.");
                using (var stream = new FileStream(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new XElement("reservation", new XAttribute("code", code)).Save(stream);
                    stream.Flush(true);
                }
                return new BuildLease(held, code, () => Write(request.Server, code, state.Item2), () => File.Delete(reservation));
            }
            catch { held.Dispose(); throw; }
        }

        public void ResolveInterrupted(string server, int code, bool confirmedNotPublished)
        {
            if (!confirmedNotPublished) throw new InvalidOperationException("Confirm the interrupted artifact was not published before releasing its reservation.");
            using (Lock(server))
            {
                var path = ReservationPath(server);
                if (!File.Exists(path) || (int)XElement.Load(path).Attribute("code") != code)
                    throw new InvalidOperationException("The exact interrupted code must match.");
                File.Delete(path);
            }
        }

        private string ReservationPath(string server) => Path.Combine(root, server + ".pending.xml");

        private string StatePath(string server) => Path.Combine(root, server + ".xml");

        private Tuple<int, int> Read(string server)
        {
            var path = StatePath(server);
            if (!File.Exists(path)) throw new InvalidOperationException("Confirm local successful and externally published baselines before building.");
            try
            {
                var xml = XElement.Load(path);
                var successful = (int)xml.Attribute("successful");
                var published = (int)xml.Attribute("published");
                if (xml.Name != "build-state" || (string)xml.Attribute("server") != server
                    || (string)xml.Attribute("schema") != "1" || successful < 0 || published < 0
                    || successful > AndroidBuildPolicy.MaximumCode || published > AndroidBuildPolicy.MaximumCode)
                    throw new InvalidDataException("Invalid build registry.");
                return Tuple.Create(successful, published);
            }
            catch (Exception error) when (!(error is IOException))
            { throw new InvalidDataException("Build registry is unreadable; reconcile from verified evidence.", error); }
        }

        private void Write(string server, int successful, int published)
        {
            var path = StatePath(server);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new XElement("build-state", new XAttribute("schema", "1"), new XAttribute("server", server),
                        new XAttribute("successful", successful), new XAttribute("published", published)).Save(stream);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    public sealed class BuildLease : IDisposable
    {
        private FileStream held;
        private Action commit;
        private Action release;
        public int Code { get; }
        internal BuildLease(FileStream held, int code, Action commit, Action release)
        { this.held = held; Code = code; this.commit = commit; this.release = release; }
        public void CommitVerifiedSuccess()
        {
            if (held == null || commit == null) throw new InvalidOperationException("Lease is closed or already committed.");
            commit();
            commit = null;
        }
        public void Dispose()
        {
            try { if (held != null) release?.Invoke(); }
            finally { held?.Dispose(); held = null; commit = null; release = null; }
        }
    }
}
