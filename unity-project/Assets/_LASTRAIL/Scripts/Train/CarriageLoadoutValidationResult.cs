namespace LastRail.Train
{
    public readonly struct CarriageLoadoutValidationResult
    {
        public CarriageLoadoutValidationResult(bool isValid, string error)
        {
            IsValid = isValid;
            Error = error ?? string.Empty;
        }

        public bool IsValid { get; }
        public string Error { get; }

        public static CarriageLoadoutValidationResult Valid()
        {
            return new CarriageLoadoutValidationResult(true, string.Empty);
        }

        public static CarriageLoadoutValidationResult Invalid(string error)
        {
            return new CarriageLoadoutValidationResult(false, error);
        }
    }
}
