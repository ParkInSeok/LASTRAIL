namespace LastRail.Gacha
{
    public enum GachaPullUxStateKind
    {
        ResultReviewReady,
        RecognizedDuplicate,
        RecoverableRejected,
        AuthRequired,
        FatalRejected,
        RetryableNetworkError
    }
}
