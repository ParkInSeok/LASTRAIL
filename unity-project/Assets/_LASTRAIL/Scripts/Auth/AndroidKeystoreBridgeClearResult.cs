namespace LastRail.Auth
{
    public sealed class AndroidKeystoreBridgeClearResult
    {
        private AndroidKeystoreBridgeClearResult(bool isCleared, string errorCode)
        {
            IsCleared = isCleared;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        }

        public bool IsCleared { get; }
        public string ErrorCode { get; }

        public static AndroidKeystoreBridgeClearResult Cleared()
        {
            return new AndroidKeystoreBridgeClearResult(true, null);
        }

        public static AndroidKeystoreBridgeClearResult Failed(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                throw new System.ArgumentException("Value cannot be empty.", nameof(errorCode));
            }

            return new AndroidKeystoreBridgeClearResult(false, errorCode);
        }
    }
}
