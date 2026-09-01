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
    }
}
#endif
