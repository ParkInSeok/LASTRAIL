#if UNITY_EDITOR
using System;
using NUnit.Framework;
using LastRail.Presentation;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchEditorFlowVideoQaReportTests
    {
        [Test]
        public void CreatePngFrameSequenceReport_WritesStableEvidenceFieldsWithoutSensitivePayloads()
        {
            var report = LaunchEditorFlowVideoQaReport.ForPngFrameSequence(
                "C:/Temp/LastRailEditorVideoQa",
                new[]
                {
                    "mission_shell",
                    "gacha_screen",
                    "gacha_single_pull",
                    "gacha_ten_pull",
                    "gacha_duplicate_conversion",
                    "fully_animated_combat_hud",
                    "codex_archive",
                    "reward_review",
                    "offline_reward",
                    "story_progress_summary",
                    "system_feedback_showcase",
                    "operator_card_polish_showcase",
                    "audio_showcase"
                },
                frameCount: 13,
                warningCount: 0,
                failCount: 0,
                blockers: Array.Empty<string>());

            Assert.That(report.Status, Is.EqualTo("PASS"));
            Assert.That(report.VideoFormat, Is.EqualTo("png_frame_sequence"));
            Assert.That(report.Width, Is.EqualTo(1080));
            Assert.That(report.Height, Is.EqualTo(1920));
            Assert.That(report.FrameCount, Is.EqualTo(13));
            Assert.That(report.EstimatedDurationSeconds, Is.GreaterThanOrEqualTo(60));
            Assert.That(report.CheckedSurfaceCount, Is.EqualTo(13));
            Assert.That(report.HasMp4Artifact, Is.False);
            Assert.That(report.HasRecorderPackage, Is.False);
            Assert.That(report.UsesPortraitSafeArea, Is.True);
            Assert.That(report.HasServerTransport, Is.False);
            Assert.That(report.HasRewardCalculation, Is.False);
            Assert.That(report.HasClientSideRng, Is.False);

            var json = LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report);
            Assert.That(json, Does.Contain("\"videoFormat\":\"png_frame_sequence\""));
            Assert.That(json, Does.Contain("\"screenKey\":\"gacha_ten_pull\""));
            Assert.That(json, Does.Contain("\"warningCount\":0"));
            Assert.That(json, Does.Contain("\"failCount\":0"));

            foreach (var forbidden in new[]
            {
                "accessToken",
                "refreshToken",
                "idToken",
                "accountId",
                "Authorization",
                "rawRng",
                "probability",
                "fullWeights",
                "rewardAmount",
                "ledger",
                "serverSubmit"
            })
            {
                Assert.That(json, Does.Not.Contain(forbidden), forbidden);
            }
        }

        [Test]
        public void CreateMp4ArtifactReport_WritesProjectQaEvidenceFieldsWithoutSensitivePayloads()
        {
            var report = LaunchEditorFlowVideoQaReport.ForMp4Artifact(
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch",
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/editor-flow-video-qa.mp4",
                "imageio_ffmpeg",
                new[]
                {
                    "mission_shell",
                    "gacha_screen",
                    "gacha_single_pull",
                    "gacha_ten_pull",
                    "gacha_duplicate_conversion",
                    "fully_animated_combat_hud",
                    "codex_archive",
                    "reward_review",
                    "offline_reward",
                    "story_progress_summary",
                    "system_feedback_showcase",
                    "operator_card_polish_showcase",
                    "audio_showcase"
                },
                frameCount: 13,
                durationSeconds: 65,
                mp4SizeBytes: 123456,
                hasRecorderPackage: false,
                warningCount: 0,
                failCount: 0,
                blockers: Array.Empty<string>());

            Assert.That(report.Status, Is.EqualTo("PASS"));
            Assert.That(report.VideoFormat, Is.EqualTo("mp4"));
            Assert.That(report.HasMp4Artifact, Is.True);
            Assert.That(report.Mp4Path, Is.EqualTo("E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/editor-flow-video-qa.mp4"));
            Assert.That(report.Mp4Encoder, Is.EqualTo("imageio_ffmpeg"));
            Assert.That(report.Mp4SizeBytes, Is.EqualTo(123456));
            Assert.That(report.Width, Is.EqualTo(1080));
            Assert.That(report.Height, Is.EqualTo(1920));
            Assert.That(report.FrameCount, Is.EqualTo(13));
            Assert.That(report.EstimatedDurationSeconds, Is.EqualTo(65));
            Assert.That(report.OutputRoot, Does.Contain("unity-project/QA/Act1Launch"));
            Assert.That(report.CheckedSurfaceCount, Is.EqualTo(13));
            Assert.That(report.HasRecorderPackage, Is.False);

            var json = LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report);
            Assert.That(json, Does.Contain("\"videoFormat\":\"mp4\""));
            Assert.That(json, Does.Contain("\"hasMp4Artifact\":true"));
            Assert.That(json, Does.Contain("\"mp4Path\":\"E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/editor-flow-video-qa.mp4\""));
            Assert.That(json, Does.Contain("\"mp4Encoder\":\"imageio_ffmpeg\""));
            Assert.That(json, Does.Contain("\"mp4SizeBytes\":123456"));
            Assert.That(json, Does.Contain("\"estimatedDurationSeconds\":65"));

            foreach (var forbidden in new[]
            {
                "accessToken",
                "refreshToken",
                "idToken",
                "accountId",
                "Authorization",
                "rawRng",
                "probability",
                "fullWeights",
                "rewardAmount",
                "ledger",
                "serverSubmit"
            })
            {
                Assert.That(json, Does.Not.Contain(forbidden), forbidden);
            }
        }

        [Test]
        public void CreateMp4ArtifactReport_RecordsRealResourceCaptureModeAndRejectsHiddenFallbackEvidence()
        {
            var report = LaunchEditorFlowVideoQaReport.ForMp4Artifact(
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch",
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/launch-art-resource-walkthrough.mp4",
                "unity_rendered_frames_ffmpeg",
                new[]
                {
                    "background_key_visuals",
                    "operator_portrait_cards",
                    "combat_enemy_boss_vfx",
                    "gacha_recruitment_visuals",
                    "mission_story_thumbnails",
                    "reward_offline_visuals"
                },
                frameCount: 1800,
                durationSeconds: 60,
                mp4SizeBytes: 987654,
                hasRecorderPackage: true,
                warningCount: 0,
                failCount: 0,
                blockers: Array.Empty<string>(),
                captureMode: "resource_showcase_render",
                usesFallbackComposite: false,
                fallbackCompositeFrameCount: 0,
                encodedFps: 30,
                hasHighQualityArtResourceCoverage: true);

            Assert.That(report.CaptureMode, Is.EqualTo("resource_showcase_render"));
            Assert.That(report.UsesFallbackComposite, Is.False);
            Assert.That(report.FallbackCompositeFrameCount, Is.EqualTo(0));
            Assert.That(report.EncodedFps, Is.EqualTo(30));
            Assert.That(report.HasHighQualityArtResourceCoverage, Is.True);

            var json = LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report);
            Assert.That(json, Does.Contain("\"captureMode\":\"resource_showcase_render\""));
            Assert.That(json, Does.Contain("\"usesFallbackComposite\":false"));
            Assert.That(json, Does.Contain("\"fallbackCompositeFrameCount\":0"));
            Assert.That(json, Does.Contain("\"encodedFps\":30"));
            Assert.That(json, Does.Contain("\"hasHighQualityArtResourceCoverage\":true"));
        }

        [Test]
        public void CreateMp4ArtifactReport_FailsWhenCombatActualFlowIdsOrFrameHashesDoNotProveUniqueSquads()
        {
            var report = LaunchEditorFlowVideoQaReport.ForMp4Artifact(
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch",
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/act1-launch-latest-qa.mp4",
                "imageio_ffmpeg",
                new[] { "combat_ordinary_wave_demo", "combat_elite_squad_a_demo", "combat_elite_squad_b_demo" },
                frameCount: 3,
                durationSeconds: 65,
                mp4SizeBytes: 123456,
                hasRecorderPackage: false,
                warningCount: 0,
                failCount: 0,
                blockers: Array.Empty<string>(),
                actualFlowVisibleOperatorIdsBySurface: new[]
                {
                    LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                        "combat_ordinary_wave_demo",
                        new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                        new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                        "same_hash"),
                    LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                        "combat_elite_squad_a_demo",
                        new[] { "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris" },
                        new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                        "same_hash"),
                    LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                        "combat_elite_squad_b_demo",
                        new[] { "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden" },
                        new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                        "same_hash")
                },
                combatSegmentFrameHashesDiffer: false,
                retainedMp4Count: 1);

            Assert.That(report.Status, Is.EqualTo("FAIL"));
            Assert.That(report.ManifestOnlyAnimationCoverageAllowed, Is.False);
            Assert.That(report.OperatorAnimationResourceCoverageCount, Is.EqualTo(15));
            Assert.That(report.OperatorAnimationActualFlowCoverageCount, Is.EqualTo(5));
            Assert.That(report.InitialOperatorActualFlowCoverageCount, Is.EqualTo(5));
            Assert.That(report.EliteOperatorActualFlowCoverageCount, Is.EqualTo(0));
            Assert.That(report.CombatSegmentFrameHashesDiffer, Is.False);
            Assert.That(report.Blockers, Does.Contain("combat_actual_flow_operator_ids_mismatch"));
            Assert.That(report.Blockers, Does.Contain("combat_segment_frame_hashes_identical"));

            var json = LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report);
            Assert.That(json, Does.Contain("\"actualFlowVisibleOperatorIdsBySurface\""));
            Assert.That(json, Does.Contain("\"expectedOperatorIds\""));
            Assert.That(json, Does.Contain("\"actualVisibleOperatorIds\""));
            Assert.That(json, Does.Contain("\"operatorAnimationResourceCoverageCount\":15"));
            Assert.That(json, Does.Contain("\"operatorAnimationActualFlowCoverageCount\":5"));
            Assert.That(json, Does.Contain("\"eliteOperatorActualFlowCoverageCount\":0"));
            Assert.That(json, Does.Contain("\"manifestOnlyAnimationCoverageAllowed\":false"));
            Assert.That(json, Does.Contain("\"combatSegmentFrameHashesDiffer\":false"));
            Assert.That(json, Does.Contain("\"retainedMp4Count\":1"));
        }

        [Test]
        public void CreateMp4ArtifactReport_FailsWhenCombatInteriorBackgroundsAreMissingOrDuplicated()
        {
            var evidence = new[]
            {
                LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                    "combat_ordinary_wave_demo",
                    new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                    new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                    "hash_ordinary"),
                LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                    "combat_elite_squad_a_demo",
                    new[] { "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris" },
                    new[] { "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris" },
                    "hash_elite_a"),
                LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                    "combat_elite_squad_b_demo",
                    new[] { "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden" },
                    new[] { "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden" },
                    "hash_elite_b")
            };

            var report = LaunchEditorFlowVideoQaReport.ForMp4Artifact(
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch",
                "E:/Projects/LASTRAIL/unity-project/QA/Act1Launch/Video/act1-launch-latest-qa.mp4",
                "imageio_ffmpeg",
                new[] { "combat_ordinary_wave_demo", "combat_elite_squad_a_demo", "combat_elite_squad_b_demo", "combat_story_raid_boss_demo" },
                frameCount: 4,
                durationSeconds: 65,
                mp4SizeBytes: 123456,
                hasRecorderPackage: false,
                warningCount: 0,
                failCount: 0,
                blockers: Array.Empty<string>(),
                actualFlowVisibleOperatorIdsBySurface: evidence,
                combatSegmentFrameHashesDiffer: true,
                combatInteriorBackgroundBySurface: new[]
                {
                    LaunchEditorFlowCombatInteriorEvidence.ForSurface(
                        "combat_ordinary_wave_demo",
                        "combat_train_interior_initial_squad",
                        "Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_initial_squad.png"),
                    LaunchEditorFlowCombatInteriorEvidence.ForSurface(
                        "combat_elite_squad_a_demo",
                        "combat_train_interior_initial_squad",
                        "Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_initial_squad.png")
                },
                combatInteriorBackgroundsDiffer: false,
                retainedMp4Count: 1);

            Assert.That(report.Status, Is.EqualTo("FAIL"));
            Assert.That(report.CombatInteriorBackgroundsDiffer, Is.False);
            Assert.That(report.Blockers, Does.Contain("combat_interior_backgrounds_not_unique"));
            Assert.That(report.Blockers, Does.Contain("combat_interior_background_evidence_incomplete"));

            var json = LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report);
            Assert.That(json, Does.Contain("\"combatInteriorBackgroundBySurface\""));
            Assert.That(json, Does.Contain("\"backgroundId\":\"combat_train_interior_initial_squad\""));
            Assert.That(json, Does.Contain("\"combatInteriorBackgroundsDiffer\":false"));
        }
    }
}
#endif
