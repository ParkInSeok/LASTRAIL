using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LastRail.Diagnostics
{
    public enum SafeClientErrorKind { UnityError, UnityException, UnityAssert }

    public sealed class SafeClientErrorEvent
    {
        public SafeClientErrorKind Kind { get; }
        public AppBuildSnapshot Build { get; }
        public long OccurredAtUnixMs { get; }
        public int Occurrences { get; }
        public SafeClientErrorEvent(SafeClientErrorKind kind, AppBuildSnapshot build, long occurredAtUnixMs, int occurrences = 1)
        {
            if (!Enum.IsDefined(typeof(SafeClientErrorKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (occurrences < 1 || occurrences > 60) throw new ArgumentOutOfRangeException(nameof(occurrences));
            Kind = kind; Build = build ?? throw new ArgumentNullException(nameof(build));
            OccurredAtUnixMs = occurredAtUnixMs; Occurrences = occurrences;
        }
    }

    public sealed class SafeClientErrorQueue
    {
        public const int MaxEntries = 100;
        public const int MaxFileBytes = 65536;
        public const long RetentionMs = 7L * 24 * 60 * 60 * 1000;
        private readonly object sync = new object();
        private readonly string path;
        private readonly List<SafeClientErrorEvent> events = new List<SafeClientErrorEvent>();
        private long windowStart = long.MinValue;
        private int inWindow;
        private bool dirty;
        public bool TransportAvailable => false;
        public SafeClientErrorQueue(string path) { this.path = Path.GetFullPath(path); }
        public IReadOnlyList<SafeClientErrorEvent> Snapshot() { lock (sync) return events.ToArray(); }

        public bool Record(SafeClientErrorKind kind, AppBuildSnapshot build, long now)
        {
            if (!Enum.IsDefined(typeof(SafeClientErrorKind), kind) || build == null || now < 0) return false;
            lock (sync)
            {
                Prune(now);
                if (windowStart == long.MinValue || now - windowStart >= 60000) { windowStart = now; inWindow = 0; }
                if (++inWindow > 60) { inWindow = 61; return false; }
                var previous = events.FindLastIndex(e => e.Kind == kind && e.Build.AppVersion == build.AppVersion
                    && e.Build.AppVersionCode == build.AppVersionCode && now >= e.OccurredAtUnixMs && now - e.OccurredAtUnixMs < 60000 && e.Occurrences < 60);
                if (previous >= 0)
                {
                    var item = events[previous];
                    events[previous] = new SafeClientErrorEvent(kind, item.Build, item.OccurredAtUnixMs, item.Occurrences + 1);
                }
                else events.Add(new SafeClientErrorEvent(kind, build, now));
                while (events.Count > MaxEntries) events.RemoveAt(0);
                dirty = true; return true;
            }
        }

        private void Prune(long now)
        {
            if (events.RemoveAll(e => e.OccurredAtUnixMs > now || now - e.OccurredAtUnixMs > RetentionMs) > 0) dirty = true;
        }

        // Disk I/O is invoked by the main-thread driver, never the threaded callback.
        public bool Save(long now)
        {
            lock (sync)
            {
                Prune(now);
                if (!dirty) return true;
                var temporary = path + ".tmp";
                try
                {
                    var bytes = Encode();
                    while (bytes.Length > MaxFileBytes && events.Count > 0) { events.RemoveAt(0); bytes = Encode(); }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                    dirty = false; return true;
                }
                catch (Exception) { return false; }
                finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception) { } }
            }
        }

        public bool Load(long now)
        {
            lock (sync)
            {
                events.Clear();
                try
                {
                    if (!File.Exists(path)) return true;
                    if (new FileInfo(path).Length > MaxFileBytes) throw new InvalidDataException();
                    var data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
                    if (data == null || data.schema != 1 || data.events == null || data.events.Length > MaxEntries) throw new InvalidDataException();
                    foreach (var row in data.events)
                    {
                        if (row == null || row.occurredAtUnixMs < 0 || !Enum.TryParse(row.errorKind, out SafeClientErrorKind kind)
                            || !Enum.IsDefined(typeof(SafeClientErrorKind), kind)) throw new InvalidDataException();
                        events.Add(new SafeClientErrorEvent(kind, new AppBuildSnapshot(row.appVersion, row.hasVersionCode ? (long?)row.appVersionCode : null), row.occurredAtUnixMs, row.occurrences));
                    }
                    Prune(now); return true;
                }
                catch (Exception)
                {
                    events.Clear(); dirty = true;
                    return false;
                }
            }
        }

        private byte[] Encode() => Encoding.UTF8.GetBytes(JsonUtility.ToJson(new FileData
        {
            schema = 1,
            events = events.Select(e => new Row { errorKind = e.Kind.ToString(), appVersion = e.Build.AppVersion,
                hasVersionCode = e.Build.AppVersionCode.HasValue, appVersionCode = e.Build.AppVersionCode.GetValueOrDefault(),
                occurredAtUnixMs = e.OccurredAtUnixMs, occurrences = e.Occurrences }).ToArray()
        }));

        [Serializable] private sealed class FileData { public int schema; public Row[] events; }
        [Serializable] private sealed class Row
        {
            public string errorKind, appVersion;
            public bool hasVersionCode;
            public long appVersionCode, occurredAtUnixMs;
            public int occurrences;
        }
    }
}
