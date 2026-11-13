namespace LastRail.Run
{
    public enum RunEndUxStateKind
    {
        RewardReviewReady = 1,
        RecognizedDuplicate = 2,
        RecoverableRejected = 3,
        FatalRejected = 4,
        RetryableNetworkError = 5
    }
}
