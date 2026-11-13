using System;
using System.Threading.Tasks;
using LastRail.Gacha;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullDiagnosticLogSinkTests
    {
        [Test]
        public async Task WriteAsync_StoresRecentUxStatesAndReturnsWrittenWithinCapacity()
        {
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 2);
            var first = State("batch_001");
            var second = State("batch_002");

            var firstResult = await sink.WriteAsync(first);
            var secondResult = await sink.WriteAsync(second);

            Assert.That(firstResult.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Written));
            Assert.That(secondResult.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Written));
            Assert.That(sink.States.Count, Is.EqualTo(2));
            Assert.That(sink.States[0].Batch.BatchId, Is.EqualTo("batch_001"));
            Assert.That(sink.States[1].Batch.BatchId, Is.EqualTo("batch_002"));
            Assert.That(firstResult.IsBlockingGachaPull, Is.EqualTo(false));
        }

        [Test]
        public async Task WriteAsync_DropsOldestStateAndReturnsBufferedWhenCapacityIsExceeded()
        {
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 2);

            await sink.WriteAsync(State("batch_001"));
            await sink.WriteAsync(State("batch_002"));
            var result = await sink.WriteAsync(State("batch_003"));

            Assert.That(result.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Buffered));
            Assert.That(result.CanRetryLater, Is.EqualTo(true));
            Assert.That(result.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(sink.States.Count, Is.EqualTo(2));
            Assert.That(sink.States[0].Batch.BatchId, Is.EqualTo("batch_002"));
            Assert.That(sink.States[1].Batch.BatchId, Is.EqualTo("batch_003"));
        }

        [Test]
        public async Task WriteJsonAsync_StoresRecentJsonEntriesAndKeepsSensitiveDataOutOfGeneratedJson()
        {
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 2);
            var generatedJson = GachaPullUxStateJsonAdapter.ToJson(State("batch_001"));

            await sink.WriteJsonAsync(generatedJson);
            await sink.WriteJsonAsync("{\"kind\":\"RetryableNetworkError\"}");
            var result = await sink.WriteJsonAsync("{\"kind\":\"AuthRequired\"}");

            Assert.That(result.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Buffered));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(2));
            Assert.That(sink.JsonEntries[0], Is.EqualTo("{\"kind\":\"RetryableNetworkError\"}"));
            Assert.That(sink.JsonEntries[1], Is.EqualTo("{\"kind\":\"AuthRequired\"}"));
            Assert.That(generatedJson.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(generatedJson.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(generatedJson.Contains("secret"), Is.EqualTo(false));
            Assert.That(generatedJson.Contains("rawRng"), Is.EqualTo(false));
        }

        [Test]
        public async Task Writer_WritesGeneratedJsonWhenSinkIsProvidedAndNoOpsWhenMissing()
        {
            var state = State("batch_001");
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 1);

            var written = await GachaPullDiagnosticLogWriter.WriteJsonAsync(state, sink);
            var noOp = await GachaPullDiagnosticLogWriter.WriteJsonAsync(state, null);

            Assert.That(written.WasSinkProvided, Is.EqualTo(true));
            Assert.That(written.WasWriteAttempted, Is.EqualTo(true));
            Assert.That(written.SinkResult.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Written));
            Assert.That(written.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(written.DiagnosticJson.Contains("\"kind\":\"ResultReviewReady\""), Is.EqualTo(true));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(1));
            Assert.That(noOp.WasSinkProvided, Is.EqualTo(false));
            Assert.That(noOp.WasWriteAttempted, Is.EqualTo(false));
            Assert.That(noOp.SinkResult == null, Is.EqualTo(true));
            Assert.That(noOp.IsBlockingGachaPull, Is.EqualTo(false));
        }

        [Test]
        public void ResultFactoriesAndInputs_RejectInvalidValues()
        {
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryGachaPullDiagnosticLogSink(0));
            Assert.Throws<ArgumentException>(() => GachaPullDiagnosticLogSinkResult.Buffered(""));
            Assert.Throws<ArgumentException>(() => GachaPullDiagnosticLogSinkResult.Failed(""));
            Assert.Throws<ArgumentNullException>(() => sink.WriteAsync(null).GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => sink.WriteJsonAsync("").GetAwaiter().GetResult());
            Assert.Throws<ArgumentNullException>(() => GachaPullDiagnosticLogWriter.WriteJsonAsync(null, sink).GetAwaiter().GetResult());
        }

        private static GachaPullUxState State(string batchId)
        {
            return GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(new GachaPullResponseDto(
                "banner_act1_launch",
                true,
                batchId,
                GachaPullResultStatus.Accepted,
                GachaPullCount.FromValue(1),
                "1",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPostPityStateDto(11, 1, 11, 1, 2),
                new[]
                {
                    new GachaPullResultDto(
                        1,
                        GachaRarity.Elite,
                        GachaPullResultKind.Operator,
                        "result_001",
                        new GachaRewardDto("OPERATOR", "op_vex", 1),
                        true,
                        null,
                        false,
                        null)
                },
                new DateTime(2026, 8, 26, 6, 15, 0, DateTimeKind.Utc),
                null,
                null)));
        }
    }
}
