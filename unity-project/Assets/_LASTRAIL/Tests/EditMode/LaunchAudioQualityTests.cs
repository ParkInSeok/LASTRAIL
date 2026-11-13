#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Presentation.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAudioQualityTests
    {
        [Test]
        public void LegacyPcm_ClipsRetainHeadroomLowDcAndOriginalDurations()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/_LASTRAIL/UI/Style/lastrail-audio-resources.json"));
            Assert.That(manifest.clips.Length, Is.EqualTo(22));
            var legacy = manifest.clips.Where(s => s.sourceType == "generated_local_launch_audio")
                .Concat(new[] { new Spec { id = "historical_bgm", path = "Assets/_LASTRAIL/Audio/Bgm/bgm_act1_rail_loop.wav", durationSeconds = 12, loop = true } }).ToArray();
            Assert.That(legacy.Length, Is.EqualTo(22), "21 unchanged SFX and preserved historical BGM");
            foreach (var spec in legacy)
            {
                using (var reader = new BinaryReader(File.OpenRead(spec.Path)))
                {
                    reader.BaseStream.Position = 40;
                    var count = reader.ReadInt32() / 2;
                    double sum = 0, squares = 0, peak = 0;
                    for (var i = 0; i < count; i++)
                    {
                        var sample = reader.ReadInt16() / 32768d;
                        sum += sample; squares += sample * sample; peak = Math.Max(peak, Math.Abs(sample));
                        if (!spec.Loop && (i == 0 || i == count - 1)) Assert.That(sample, Is.Zero, spec.Id);
                    }
                    Assert.That(count, Is.EqualTo(Mathf.RoundToInt(spec.DurationSeconds * 44100)), spec.Id);
                    Assert.That(peak, Is.LessThanOrEqualTo(0.5012), spec.Id + " -6dBFS sample peak");
                    Assert.That(Math.Abs(sum / count), Is.LessThan(0.001), spec.Id + " DC");
                    Assert.That(Math.Sqrt(squares / count), Is.GreaterThan(0.01), spec.Id + " non-silent");
                }
                var imported = AssetDatabase.LoadAssetAtPath<AudioClip>(spec.Path);
                Assert.That(imported, Is.Not.Null, spec.Id);
                Assert.That(imported.samples, Is.EqualTo(Mathf.RoundToInt(spec.DurationSeconds * 44100)), spec.Id);
                var importer = (AudioImporter)AssetImporter.GetAtPath(spec.Path);
                Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM), spec.Id);
            }
        }

        [Test]
        public void ApprovedBattleOriginal_MatchesApprovedBytesAndImportsWithoutPlaceholderConstraints()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/_LASTRAIL/UI/Style/lastrail-audio-resources.json"));
            var spec = manifest.clips.Single(s => s.Id == "bgm_act1_rail_loop");
            Assert.That(spec.sourceType, Is.EqualTo("approved_runway_original"));
            Assert.That(spec.Path, Is.EqualTo("Assets/_LASTRAIL/Audio/Approved20261110/BGM/battle-music.approved.v03.20261020.mp3"));
            using (var hash = SHA256.Create())
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(spec.Path))).Replace("-", "").ToLowerInvariant(),
                    Is.EqualTo("0d2f3aaa22670abdf1adea57c2d550aaf479624f3cfe3fff7beb719650d1d451"));
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(spec.Path);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.samples, Is.GreaterThan(0));
            Assert.That(clip.length, Is.GreaterThan(1));
            Assert.That(clip.channels, Is.EqualTo(2));
            Assert.That(clip.frequency, Is.GreaterThanOrEqualTo(22050));
            var importer = AssetImporter.GetAtPath(spec.Path) as AudioImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.False);
        }

        [Test]
        public void LegacyPcmBuilder_DoesNotTargetApprovedImports()
        {
            var builder = Type.GetType("LastRail.Editor.LaunchAudioResourceBuilder, LastRail.Editor", true);
            var specs = (IEnumerable)builder.GetField("Clips").GetValue(null);
            foreach (var spec in specs)
            {
                var path = (string)spec.GetType().GetProperty("Path").GetValue(spec);
                Assert.That(Path.GetExtension(path), Is.EqualTo(".wav"));
                Assert.That(path, Does.Not.Contain("Approved20261110"));
            }
        }
        [Serializable] private sealed class Manifest { public Spec[] clips; }
        [Serializable] private sealed class Spec
        {
            public string id, path, sourceType;
            public float durationSeconds;
            public bool loop;
            public string Id => id;
            public string Path => path;
            public float DurationSeconds => durationSeconds;
            public bool Loop => loop;
        }

        [Test]
        public void CuePlayer_PreservesSourceGainAndMuteAndCoalescesSameFrameAliases()
        {
            var root = new GameObject("AudioQualityTest");
            var clip = AudioClip.Create("test", 4410, 1, 44100, false);
            try
            {
                var sfx = root.AddComponent<AudioSource>();
                var bgm = root.AddComponent<AudioSource>();
                sfx.volume = 0.2f; sfx.mute = true;
                bgm.volume = 0.1f; bgm.mute = true;
                var player = root.AddComponent<LastRailAudioCuePlayer>();
                player.ConfigureForTests(sfx, bgm, new[] {
                    new LastRailAudioClipBinding("combat_hit", clip, false, 0.8f),
                    new LastRailAudioClipBinding("combat_skill", clip, false, 0.8f),
                    new LastRailAudioClipBinding("bgm_act1_rail_loop", clip, true, 0.35f) });
                Assert.That(player.TryPlayCue("combat_hit").Played, Is.True);
                Assert.That(sfx.volume, Is.EqualTo(0.2f));
                Assert.That(sfx.mute, Is.True);
                Assert.That(player.TryPlayCue("sfx_hit").Reason, Is.EqualTo("same_frame_duplicate"));
                Assert.That(player.TryPlayCue("combat_skill").Played, Is.True);
                player.TryPrepareBgmLoop("bgm_act1_loop");
                Assert.That(bgm.volume, Is.EqualTo(0.1f));
                Assert.That(bgm.mute, Is.True);
                Assert.That(bgm.playOnAwake, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(clip); }
        }

        [UnityTest]
        public IEnumerator HistoricalProceduralLoop_IsContinuousAndRepeatedCuesDoNotLatchAcrossFrames()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("AudioQualityPlayback");
            try
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_LASTRAIL/Audio/Bgm/bgm_act1_rail_loop.wav");
                clip.LoadAudioData();
                var deadline = Time.realtimeSinceStartup + 10;
                while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
                var samples = new float[clip.samples];
                Assert.That(clip.GetData(samples, 0), Is.True, "Read actual Unity-imported PCM");
                double sine = 0, cosine = 0;
                for (var i = 0; i < samples.Length; i++)
                {
                    var phase = Math.PI * i / clip.frequency;
                    sine += samples[i] * Math.Sin(phase); cosine += samples[i] * Math.Cos(phase);
                }
                Assert.That(2 * Math.Sqrt(sine*sine + cosine*cosine) / samples.Length, Is.LessThan(0.0001), "No 0.5Hz headroom waste");
                var seam = samples[0] - samples[samples.Length - 1];
                Assert.That(Math.Abs(seam), Is.LessThan(0.004), "Boundary is a normal waveform step, not a zero-padded gap");
                Assert.That(Math.Abs(seam - (samples[1] - samples[0])), Is.LessThan(0.001), "Loop slope continuity");
                var sfx = root.AddComponent<AudioSource>();
                var bgm = root.AddComponent<AudioSource>();
                sfx.mute = bgm.mute = true;
                // Full-suite PlayMode transitions can leave a newly added empty source marked playing.
                // Establish the stopped fixture before testing whether configuration starts playback.
                sfx.Stop(); bgm.Stop();
                Assert.That(bgm.isPlaying, Is.False, "Stopped fixture baseline");
                sfx.volume = 0.2f; bgm.volume = 0.1f;
                var player = root.AddComponent<LastRailAudioCuePlayer>();
                var hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_LASTRAIL/Audio/Sfx/Combat/combat_hit.wav");
                player.ConfigureForTests(sfx, bgm, new[] {
                    new LastRailAudioClipBinding("combat_hit", hit, false, 0.8f),
                    new LastRailAudioClipBinding("bgm_act1_rail_loop", clip, true, 0.35f) });
                Assert.That(bgm.isPlaying, Is.False, "No forced autoplay");
                Assert.That(player.TryPlayCue("combat_hit").Played, Is.True);
                Assert.That(player.TryPlayCue("sfx_hit").Played, Is.False);
                yield return null;
                Assert.That(player.TryPlayCue("sfx_hit").Played, Is.True, "No cross-frame cooldown");
                player.TryPrepareBgmLoop("bgm_act1_loop");
                yield return null;
                player.TryPrepareBgmLoop("bgm_act1_loop");
                Assert.That(bgm.clip, Is.SameAs(clip));
                Assert.That(bgm.loop, Is.True);
                Assert.That(bgm.volume, Is.EqualTo(0.1f));
                Assert.That(bgm.mute, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
#endif
