using System;

namespace LastRail.Run
{
    public sealed class RunApiFlowSubmitResult
    {
        public RunApiFlowSubmitResult(
            RunApiFlowSubmitOutcome outcome,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorMessage = errorMessage;
        }

        public RunApiFlowSubmitOutcome Outcome { get; }
        public RunResultSubmitResponseDto Response { get; }
        public string ErrorMessage { get; }

        public static RunApiFlowSubmitResult FromClientResult(RunResultSubmitClientResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new RunApiFlowSubmitResult(
                MapOutcome(result.Outcome),
                result.Response,
                result.ErrorMessage);
        }

        private static RunApiFlowSubmitOutcome MapOutcome(RunResultSubmitClientOutcome outcome)
        {
            switch (outcome)
            {
                case RunResultSubmitClientOutcome.Accepted:
                    return RunApiFlowSubmitOutcome.Accepted;
                case RunResultSubmitClientOutcome.Rejected:
                    return RunApiFlowSubmitOutcome.Rejected;
                case RunResultSubmitClientOutcome.DuplicateReplay:
                    return RunApiFlowSubmitOutcome.DuplicateReplay;
                case RunResultSubmitClientOutcome.NetworkError:
                    return RunApiFlowSubmitOutcome.NetworkError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported run result submit outcome.");
            }
        }
    }
}
