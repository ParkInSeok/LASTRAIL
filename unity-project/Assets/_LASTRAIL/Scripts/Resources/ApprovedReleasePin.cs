using System;
using System.IO;
using LastRail.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Resources
{
    public sealed class ApprovedReleasePinException : InvalidOperationException
    {
        public ApprovedReleasePinException() : base("Pending content release requires explicit confirmation.") { }
    }

    // Stores no authority: every startup must validate a fresh server approval before comparing this record.
    public sealed class ApprovedReleasePin
    {
        private readonly string path;
        private JObject verified;
        private JObject completed;
        public ApprovedReleasePin(string path) { this.path = Path.GetFullPath(path); }
        public void VerifyOrPin(string source, string installedCode, string trustedRoot, ClientContentGate gate)
        {
            verified = null; completed = null;
            var expected = new JObject { ["schema"] = 1, ["source"] = source, ["installedCode"] = installedCode,
                ["trustedRoot"] = trustedRoot, ["release"] = Release(gate) };
            if (File.Exists(path))
            {
                if (!JToken.DeepEquals(Read(), expected)) throw new ApprovedReleasePinException();
                verified = expected;
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(expected.ToString(Formatting.None));
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                // Move cannot overwrite an existing pin created by a concurrent startup.
                File.Move(temporary, path);
                verified = expected;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public void Clear() { File.Delete(path); verified = null; completed = null; }
        public void Complete(ClientContentGate gate)
        {
            if (completed != null && !File.Exists(path) && JToken.DeepEquals(completed["release"], Release(gate))) return;
            if (verified == null || !File.Exists(path) || !JToken.DeepEquals(verified["release"], Release(gate))
                || !JToken.DeepEquals(Read(), verified)) throw new ApprovedReleasePinException();
            var identity = verified;
            Clear();
            completed = identity;
        }
        private JObject Read()
        {
            try
            {
                if (new FileInfo(path).Length > 8192) throw new ApprovedReleasePinException();
                return GameApiJson.ReadObject(File.ReadAllText(path));
            }
            catch (IOException) { throw; }
            catch (UnauthorizedAccessException) { throw; }
            catch { throw new ApprovedReleasePinException(); }
        }
        private static JObject Release(ClientContentGate gate)
        {
            if (gate == null || gate.Evaluate(null) != ContentStage.Initializing || string.IsNullOrEmpty(gate.ApprovedReleaseId))
                throw new InvalidOperationException("Fresh approved content is required.");
            return new JObject { ["id"] = gate.ApprovedReleaseId, ["version"] = gate.ApprovedContentVersion,
                ["catalog"] = gate.ApprovedCatalogUrl, ["hash"] = gate.ApprovedCatalogHash };
        }
    }
}
