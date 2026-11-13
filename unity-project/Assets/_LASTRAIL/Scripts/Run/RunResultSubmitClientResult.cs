using System;

namespace LastRail.Run
{
    public sealed class RunResultSubmitClientResult
    {
        private RunResultSubmitClientResult(
            RunResultSubmitClientOutcome outcome,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorMessage = errorMessage;
        }

        public RunResultSubmitClientOutcome Outcome { get; }
        public RunResultSubmitResponseDto Response { get; }
        public string ErrorMessage { get; }

        public static RunResultSubmitClientResult Accepted(RunResultSubmitResponseDto response)
        {
            RequireResponse(response, nameof(response));
            return new RunResultSubmitClientResult(RunResultSubmitClientOutcome.Accepted, response, null);
        }

        public static RunResultSubmitClientResult Rejected(RunResultSubmitResponseDto response)
        {
            RequireResponse(response, nameof(response));
            return new RunResultSubmitClientResult(RunResultSubmitClientOutcome.Rejected, response, null);
        }

        public static RunResultSubmitClientResult DuplicateReplay(RunResultSubmitResponseDto response)
        {
            RequireResponse(response, nameof(response));
            return new RunResultSubmitClientResult(RunResultSubmitClientOutcome.DuplicateReplay, response, null);
        }

        public static RunResultSubmitClientResult NetworkError(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(errorMessage));
            }

            return new RunResultSubmitClientResult(RunResultSubmitClientOutcome.NetworkError, null, errorMessage);
        }

        private static void RequireResponse(RunResultSubmitResponseDto response, string name)
        {
            if (response == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
