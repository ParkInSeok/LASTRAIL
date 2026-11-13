using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardClaimRequestTests
    {
        [Test]
        public void Create_BuildsPostRequestWithEndpointAndIdempotencyHeader()
        {
            var request = RunRewardClaimRequest.Create("run_001", "preview_001", "claim_run_001_attempt_001");

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/reward-preview/preview_001/claim"));
            Assert.That(request.RunId, Is.EqualTo("run_001"));
            Assert.That(request.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("claim_run_001_attempt_001"));
            Assert.That(request.BodyJson, Is.EqualTo("{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\"}"));
            Assert.That(request.BodyJson.Contains("quantity"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("rewardLines"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("currencyGrant"), Is.EqualTo(false));
        }

        [Test]
        public void Create_RejectsEmptyInputs()
        {
            Assert.Throws<System.ArgumentException>(() => RunRewardClaimRequest.Create(null, "preview_001", "key"));
            Assert.Throws<System.ArgumentException>(() => RunRewardClaimRequest.Create("run_001", " ", "key"));
            Assert.Throws<System.ArgumentException>(() => RunRewardClaimRequest.Create("run_001", "preview_001", ""));
        }
    }
}
