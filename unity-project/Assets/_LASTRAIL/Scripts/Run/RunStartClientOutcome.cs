namespace LastRail.Run
{
    public enum RunStartClientOutcome
    {
        Started,
        DuplicateReplay,
        OperationPreviewAlreadyConsumed,
        StateConflict,
        AuthRequired,
        Rejected,
        NetworkError
    }
}
