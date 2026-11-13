#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Editor
{
    // Internal build artifacts only. Hashes detect corruption, not the provenance of imported files.
    public static class LocalContentBaselineArchive
    {
        public static string Archive(string root, string id, string statePath, string runtimeDirectory,
            string remoteDirectory, string identity, string baselineId)
        {
            Id(id); Identity(identity);
            root = LocalPath(root);
            statePath = LocalPath(statePath);
            runtimeDirectory = LocalPath(runtimeDirectory);
            remoteDirectory = LocalPath(remoteDirectory);
            if (Within(root, runtimeDirectory) || Within(runtimeDirectory, root)
                || Within(root, remoteDirectory) || Within(remoteDirectory, root))
                throw new ArgumentException("Archive and source directories must not overlap.");
            var destination = Child(root, id);
            if (Exists(destination)) throw new IOException("Archive ID already exists.");
            if (!File.Exists(statePath) || !Directory.Exists(runtimeDirectory) || !Directory.Exists(remoteDirectory))
                throw new FileNotFoundException("Build outputs are missing.");
            string baselineHash = null;
            if (baselineId != null)
            {
                if (baselineId == id) throw new ArgumentException("Update ID must differ from full baseline.");
                baselineHash = Digest(Validate(root, baselineId, identity));
                if (Digest(statePath) != baselineHash) throw new InvalidDataException("Update must retain original full state.");
            }
            // Enumerate before creating staging, rejecting junctions instead of following them.
            var runtimeFiles = Files(runtimeDirectory);
            var remoteFiles = Files(remoteDirectory);
            Directory.CreateDirectory(root); CheckAncestors(root);
            var staging = Child(root, ".staging-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                Directory.CreateDirectory(Child(staging, "runtime"));
                Directory.CreateDirectory(Child(staging, "remote"));
                var entries = new JArray();
                Copy(statePath, Child(staging, "state.bin"), "state.bin", entries);
                CopyTree(runtimeDirectory, runtimeFiles, staging, "runtime", entries);
                CopyTree(remoteDirectory, remoteFiles, staging, "remote", entries);
                var manifest = new JObject {
                    ["schemaVersion"] = 1, ["id"] = id, ["identity"] = identity,
                    ["baselineId"] = baselineId == null ? JValue.CreateNull() : new JValue(baselineId),
                    ["baselineStateSha256"] = baselineHash == null ? JValue.CreateNull() : new JValue(baselineHash),
                    ["files"] = entries
                };
                using (var stream = new FileStream(Child(staging, "manifest.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.Write(manifest.ToString(Formatting.Indented));
                Verify(staging, id, identity, false);
                if (baselineId != null && (Digest(Validate(root, baselineId, identity)) != baselineHash
                    || Digest(Child(staging, "state.bin")) != baselineHash))
                    throw new InvalidDataException("Full baseline changed during archive.");
                CheckAncestors(root); CheckAncestors(staging);
                // Same-parent rename publishes a complete archive; never replace an existing ID.
                Directory.Move(staging, destination);
                return destination;
            }
            finally
            {
                if (Directory.Exists(staging))
                {
                    try { CheckAncestors(staging); Files(staging); Directory.Delete(staging, true); }
                    catch { /* Leave unsafe or inaccessible staging untouched. */ }
                }
            }
        }

        public static string Validate(string root, string id, string identity)
        {
            Id(id); Identity(identity);
            return Verify(Child(LocalPath(root), id), id, identity, true);
        }

        private static string Verify(string folder, string id, string identity, bool requireFull)
        {
            CheckAncestors(folder);
            var manifestPath = Child(folder, "manifest.json");
            if (!File.Exists(manifestPath) || new FileInfo(manifestPath).Length > 16 * 1024 * 1024)
                throw new InvalidDataException("Missing or oversized archive manifest.");
            JObject manifest;
            using (var stream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                manifest = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new InvalidDataException("Trailing manifest data.");
            }
            var keys = new[] { "schemaVersion", "id", "identity", "baselineId", "baselineStateSha256", "files" };
            if (manifest.Count != keys.Length || manifest.Properties().Any(p => !keys.Contains(p.Name))
                || manifest["schemaVersion"]?.Type != JTokenType.Integer || (int)manifest["schemaVersion"] != 1
                || Text(manifest, "id") != id || Text(manifest, "identity") != identity)
                throw new InvalidDataException("Archive identity or schema mismatch.");
            var baseline = manifest["baselineId"];
            if (baseline.Type != JTokenType.Null)
            {
                Id(Text(manifest, "baselineId"));
                if (requireFull || !HashText(Text(manifest, "baselineStateSha256")))
                    throw new InvalidDataException("An update archive is not a full baseline.");
            }
            else if (manifest["baselineStateSha256"].Type != JTokenType.Null)
                throw new InvalidDataException("Unexpected full baseline reference.");
            if (!(manifest["files"] is JArray entries) || entries.Count == 0)
                throw new InvalidDataException("Missing archive files.");
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in entries)
            {
                if (!(token is JObject entry) || entry.Count != 3
                    || entry.Properties().Any(p => p.Name != "path" && p.Name != "length" && p.Name != "sha256"))
                    throw new InvalidDataException("Invalid archive file entry.");
                var relative = Text(entry, "path");
                if (relative != "state.bin" && !relative.StartsWith("runtime/", StringComparison.Ordinal)
                    && !relative.StartsWith("remote/", StringComparison.Ordinal)) throw new InvalidDataException("Unexpected archive path.");
                if (relative.Contains("\\") || !listed.Add(relative)) throw new InvalidDataException("Duplicate or noncanonical archive path.");
                var path = Child(folder, relative);
                var hash = Text(entry, "sha256");
                if (entry["length"]?.Type != JTokenType.Integer || (long)entry["length"] < 0 || !HashText(hash)
                    || !File.Exists(path) || new FileInfo(path).Length != (long)entry["length"] || Digest(path) != hash)
                    throw new InvalidDataException("Archive file hash or length mismatch.");
            }
            if (!listed.Contains("state.bin") || !Directory.Exists(Child(folder, "runtime")) || !Directory.Exists(Child(folder, "remote")))
                throw new InvalidDataException("Incomplete archive layout.");
            var actual = Files(folder).Select(p => Relative(folder, p)).Where(p => p != "manifest.json");
            if (!listed.SetEquals(actual)) throw new InvalidDataException("Unlisted archive files.");
            return Child(folder, "state.bin");
        }

        private static void CopyTree(string source, string[] files, string staging, string name, JArray entries)
        {
            foreach (var file in files)
            {
                var relative = name + "/" + Relative(source, file);
                Copy(file, Child(staging, relative), relative, entries);
            }
        }
        private static void Copy(string source, string destination, string relative, JArray entries)
        {
            CheckAncestors(source); CheckAncestors(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)) input.CopyTo(output);
            entries.Add(new JObject { ["path"] = relative, ["length"] = new FileInfo(destination).Length, ["sha256"] = Digest(destination) });
        }
        internal static string[] Files(string root)
        {
            CheckAncestors(root);
            var files = new List<string>(); var directories = new Stack<string>(); directories.Push(root);
            while (directories.Count > 0)
            {
                var directory = directories.Pop(); CheckAncestors(directory);
                foreach (var path in Directory.GetFileSystemEntries(directory))
                {
                    LocalPath(path);
                    if ((File.GetAttributes(path) & FileAttributes.Directory) != 0) directories.Push(path);
                    else files.Add(path);
                }
            }
            return files.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }
        internal static string Digest(string path)
        {
            CheckAncestors(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static string Text(JObject value, string key)
        {
            if (value[key]?.Type != JTokenType.String) throw new InvalidDataException("Expected manifest string.");
            return (string)value[key];
        }
        private static bool HashText(string text) => text != null && text.Length == 64 && text.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        private static void Identity(string identity)
        { if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("Explicit build identity is required."); }
        private static void Id(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 128 || id == "." || id == ".." || id.EndsWith(".", StringComparison.Ordinal)
                || id.Any(c => !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_' && c != '.'))
                throw new ArgumentException("Explicit safe archive ID is required.");
            Segment(id);
        }
        internal static string LocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
                throw new ArgumentException("Only local paths are allowed.");
            var normalized = path.Replace('\\', '/');
            var drive = normalized.Length >= 2 && normalized[1] == ':' ? normalized.Substring(0, 2) : "";
            foreach (var part in normalized.Substring(drive.Length).Split('/')) if (part.Length > 0) Segment(part);
            var full = Path.GetFullPath(path);
            if (full.StartsWith("\\\\", StringComparison.Ordinal)) throw new ArgumentException("UNC paths are not allowed.");
            if (string.Equals(full, Path.GetPathRoot(full), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Filesystem roots are not artifact paths.");
            CheckAncestors(full);
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        private static void Segment(string value)
        {
            if (value == "." || value == ".." || value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith(" ", StringComparison.Ordinal)
                || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains(":")) throw new ArgumentException("Unsafe path segment.");
            var stem = value.Split('.')[0].ToUpperInvariant();
            if (new[] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$" }.Contains(stem)
                || stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '0' && stem[3] <= '9')
                throw new ArgumentException("Device names are not local artifact paths.");
        }
        internal static string Child(string parent, string relative)
        {
            if (Path.IsPathRooted(relative)) throw new ArgumentException("Expected relative archive path.");
            var result = LocalPath(Path.Combine(parent, relative));
            if (!Within(result, parent) || string.Equals(result, parent, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Path escapes archive.");
            return result;
        }
        private static bool Within(string path, string directory) => string.Equals(path, directory, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        private static string Relative(string root, string file) => file.Substring(root.Length + 1).Replace('\\', '/');
        private static bool Exists(string path)
        {
            try { File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        private static void CheckAncestors(string path)
        {
            for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse paths are not permitted.");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
        }
    }
}
#endif
