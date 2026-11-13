using System;
using System.Threading.Tasks;

namespace LastRail.Run
{
    public static class RunApiFlowFacade
    {
        public static RunRuntimeState StartActOne(string responseJson, RunApiFlowStartOptions options)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(responseJson));
            }

            return StartActOne(RunStartResponseJsonAdapter.FromJson(responseJson), options);
        }

        public static RunRuntimeState StartActOne(RunStartResponseDto response, RunApiFlowStartOptions options)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var snapshot = RunStartResponseAdapter.ToRunSnapshot(
                response,
                options.SelectedOperatorIds,
                options.SelectedUltimateOperatorId,
                options.SelectedTrainCarSlots,
                options.CompletedOperationIds,
                options.EngineOutput);

            return RunRuntimeState.StartActOne(snapshot);
        }

        public static RunResultSubmitRequest CreateResultSubmitRequest(RunRuntimeState runtimeState, string idempotencyKey)
        {
            return RunResultSubmitRequest.FromRuntimeState(runtimeState, idempotencyKey);
        }

        public static RunResultSubmitRequest CreateResultSubmitRequest(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunResultOutcome outcome,
            DateTime clientCompletedAt)
        {
            return RunResultSubmitRequest.FromRuntimeState(runtimeState, idempotencyKey, outcome, clientCompletedAt);
        }

        public static async Task<RunApiFlowSubmitResult> SubmitResultAsync(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            IRunResultSubmitClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var request = CreateResultSubmitRequest(runtimeState, idempotencyKey);
            return await SubmitResultAsync(request, client);
        }

        public static async Task<RunApiFlowSubmitResult> SubmitResultAsync(
            RunResultSubmitRequest request,
            IRunResultSubmitClient client)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (client == null) throw new ArgumentNullException(nameof(client));
            var result = await client.SubmitAsync(request);
            return RunApiFlowSubmitResult.FromClientResult(result);
        }

        public static async Task<RunApiFlowSubmitPolicy> SubmitResultPolicyAsync(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            IRunResultSubmitClient client)
        {
            var result = await SubmitResultAsync(runtimeState, idempotencyKey, client);
            return RunApiFlowSubmitPolicy.Evaluate(result);
        }

        public static RunEndUxState CreateEndUxState(RunApiFlowSubmitPolicy submitPolicy)
        {
            return RunEndUxState.FromSubmitPolicy(submitPolicy);
        }

        public static async Task<RunEndUxState> SubmitResultEndUxStateAsync(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            IRunResultSubmitClient client)
        {
            var policy = await SubmitResultPolicyAsync(runtimeState, idempotencyKey, client);
            return CreateEndUxState(policy);
        }

        public static Task<RunApiFlowDiagnosticWriteResult> WriteDiagnosticSnapshotAsync(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunApiFlowSubmitResult submitResult,
            IRunApiFlowDiagnosticLogSink sink)
        {
            if (submitResult == null)
            {
                throw new ArgumentNullException(nameof(submitResult));
            }

            return WriteDiagnosticSnapshotAsync(
                runtimeState,
                idempotencyKey,
                RunApiFlowSubmitPolicy.Evaluate(submitResult),
                sink);
        }

        public static async Task<RunApiFlowDiagnosticWriteResult> WriteDiagnosticSnapshotAsync(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunApiFlowSubmitPolicy submitPolicy,
            IRunApiFlowDiagnosticLogSink sink)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (submitPolicy == null)
            {
                throw new ArgumentNullException(nameof(submitPolicy));
            }

            var snapshot = RunApiFlowDiagnosticSnapshot.FromSubmitPolicy(runtimeState, idempotencyKey, submitPolicy);
            if (sink == null)
            {
                return new RunApiFlowDiagnosticWriteResult(snapshot, false, false, null);
            }

            var sinkResult = await sink.WriteAsync(snapshot);
            return new RunApiFlowDiagnosticWriteResult(snapshot, true, true, sinkResult);
        }
    }
}
