namespace LastRail.Auth
{
    public sealed class AndroidKeystoreBridgeAvailability
    {
        private AndroidKeystoreBridgeAvailability(bool isAvailable, string providerName, string errorCode, string message)
        {
            IsAvailable = isAvailable;
            ProviderName = Optional(providerName);
            ErrorCode = Optional(errorCode);
            Message = Optional(message);
        }

        public bool IsAvailable { get; }
        public string ProviderName { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static AndroidKeystoreBridgeAvailability Available(string providerName)
        {
            return new AndroidKeystoreBridgeAvailability(true, Require(providerName), null, null);
        }

        public static AndroidKeystoreBridgeAvailability Unavailable(string errorCode, string message)
        {
            return new AndroidKeystoreBridgeAvailability(false, null, Require(errorCode), Require(message));
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
