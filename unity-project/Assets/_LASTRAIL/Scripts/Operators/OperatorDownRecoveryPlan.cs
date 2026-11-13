namespace LastRail.Operators
{
    public readonly struct OperatorDownRecoveryPlan
    {
        public OperatorDownRecoveryPlan(OperatorDownState state, float recoveryDelaySeconds, float recoveryHpRatio)
        {
            State = state;
            RecoveryDelaySeconds = recoveryDelaySeconds;
            RecoveryHpRatio = recoveryHpRatio;
        }

        public OperatorDownState State { get; }
        public float RecoveryDelaySeconds { get; }
        public float RecoveryHpRatio { get; }
    }
}
