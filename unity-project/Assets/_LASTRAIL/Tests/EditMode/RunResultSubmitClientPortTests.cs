using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunResultSubmitClientPortTests
    {
        [Test]
        public async Task SubmitAsync_PortAcceptsRequestAndReturnsAcceptedResponseResult()
        {
            var request = Request();
            IRunResultSubmitClient client = new CapturingRunResultSubmitClient(RunResultSubmitClientResult.Accepted(Response("ACCEPTED", true)));

            var result = await client.SubmitAsync(request);

            Assert.That(((CapturingRunResultSubmitClient)client).LastRequest, Is.EqualTo(request));
            Assert.That(result.Outcome, Is.EqualTo(RunResultSubmitClientOutcome.Accepted));
            Assert.That(result.Response.RunId, Is.EqualTo("run_001"));
            Assert.That(result.Response.Accepted, Is.EqualTo(true));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void Rejected_StoresServerRejectedResponseWithoutThrowing()
        {
            var response = Response("REJECTED", false);

            var result = RunResultSubmitClientResult.Rejected(response);

            Assert.That(result.Outcome, Is.EqualTo(RunResultSubmitClientOutcome.Rejected));
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void DuplicateReplay_StoresIdempotentReplayResponseWithoutThrowing()
        {
            var response = Response("DUPLICATE_REPLAY", false);

            var result = RunResultSubmitClientResult.DuplicateReplay(response);

            Assert.That(result.Outcome, Is.EqualTo(RunResultSubmitClientOutcome.DuplicateReplay));
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void NetworkError_StoresMessageWithoutServerResponse()
        {
            var result = RunResultSubmitClientResult.NetworkError("timeout");

            Assert.That(result.Outcome, Is.EqualTo(RunResultSubmitClientOutcome.NetworkError));
            Assert.That(result.Response, Is.EqualTo(null));
            Assert.That(result.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void ResultFactories_RejectNullOrInvalidInputs()
        {
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitClientResult.Accepted(null));
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitClientResult.Rejected(null));
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitClientResult.DuplicateReplay(null));
            Assert.Throws<ArgumentException>(() => RunResultSubmitClientResult.NetworkError(""));
        }

        private static RunResultSubmitRequest Request()
        {
            var response = new RunRuntimeStateExport(
                "run_001",
                "balance_v1",
                "opn_03_water_stop",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                2,
                "waveplan_act1_opn03_seed1234",
                1234u,
                "reward_act1_opn03_risk2",
                new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
                LastRail.Sortie.SortieStage.Complete,
                0,
                15,
                true,
                Array.Empty<TacticSelectionExport>());

            return RunResultSubmitRequest.FromExport(response, "submit_run_001_attempt_001");
        }

        private static RunResultSubmitResponseDto Response(string resultStatus, bool accepted)
        {
            return new RunResultSubmitResponseDto(
                "run_001",
                accepted,
                resultStatus,
                accepted ? "claim_001" : null,
                "preview_001",
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                null,
                null);
        }

        private sealed class CapturingRunResultSubmitClient : IRunResultSubmitClient
        {
            private readonly RunResultSubmitClientResult result;

            public CapturingRunResultSubmitClient(RunResultSubmitClientResult result)
            {
                this.result = result;
            }

            public RunResultSubmitRequest LastRequest { get; private set; }

            public Task<RunResultSubmitClientResult> SubmitAsync(RunResultSubmitRequest request)
            {
                LastRequest = request;
                return Task.FromResult(result);
            }
        }
    }
}
