using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullClientResult
    {
        private GachaPullClientResult(GachaPullClientOutcome outcome, GachaPullResponseDto response, string errorMessage)
        {
            if (outcome == GachaPullClientOutcome.NetworkError)
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    throw new ArgumentException("Network error message is required.", nameof(errorMessage));
                }
            }
            else if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            Outcome = outcome;
            Response = response;
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
        }

        public GachaPullClientOutcome Outcome { get; }
        public GachaPullResponseDto Response { get; }
        public string ErrorMessage { get; }

        public static GachaPullClientResult Accepted(GachaPullResponseDto response)
        {
            return new GachaPullClientResult(GachaPullClientOutcome.Accepted, response, null);
        }

        public static GachaPullClientResult DuplicateReplay(GachaPullResponseDto response)
        {
            return new GachaPullClientResult(GachaPullClientOutcome.DuplicateReplay, response, null);
        }

        public static GachaPullClientResult Rejected(GachaPullResponseDto response)
        {
            return new GachaPullClientResult(GachaPullClientOutcome.Rejected, response, null);
        }

        public static GachaPullClientResult NetworkError(string errorMessage)
        {
            return new GachaPullClientResult(GachaPullClientOutcome.NetworkError, null, errorMessage);
        }
    }
}
