namespace LastRail.Auth
{
    public sealed class AndroidKeystoreBridgeWriteResult
    {
        private AndroidKeystoreBridgeWriteResult(bool isSaved, string errorCode)
        {
            IsSaved = isSaved;
            ErrorCode = Optional(errorCode);
        }

        public bool IsSaved { get; }
        public string ErrorCode { get; }

        public static AndroidKeystoreBridgeWriteResult Saved()
        {
            return new AndroidKeystoreBridgeWriteResult(true, null);
        }

        public static AndroidKeystoreBridgeWriteResult Failed(string errorCode)
        {
            return new AndroidKeystoreBridgeWriteResult(false, Require(errorCode));
        }

        private static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new System.ArgumentException("Value cannot be empty.");
            }

            return value.Trim();
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
