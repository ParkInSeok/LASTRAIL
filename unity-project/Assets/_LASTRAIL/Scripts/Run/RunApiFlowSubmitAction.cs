namespace LastRail.Run
{
    public enum RunApiFlowSubmitAction
    {
        ShowRewardReview = 1,
        SyncRecognizedResult = 2,
        ShowRecoverableRejected = 3,
        ShowFatalRejected = 4,
        RetryWithSameIdempotencyKey = 5
    }
}
