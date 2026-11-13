using System;

namespace LastRail.Narrative
{
    public sealed class StoryProgressReadClientResult
    {
        private StoryProgressReadClientResult(
            StoryProgressReadClientOutcome outcome,
            StoryProgressReadResponseDto response,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public StoryProgressReadClientOutcome Outcome { get; }
        public StoryProgressReadResponseDto Response { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        public static StoryProgressReadClientResult Found(StoryProgressReadResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new StoryProgressReadClientResult(StoryProgressReadClientOutcome.Found, response, null, null);
        }

        public static StoryProgressReadClientResult Rejected(string errorCode, string errorMessage)
        {
            return new StoryProgressReadClientResult(
                StoryProgressReadClientOutcome.Rejected,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static StoryProgressReadClientResult NetworkError(string errorMessage)
        {
            return new StoryProgressReadClientResult(
                StoryProgressReadClientOutcome.NetworkError,
                null,
                null,
                Required(errorMessage, nameof(errorMessage)));
        }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
