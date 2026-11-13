namespace LastRail.Run
{
    // Supported client transport subset, not the server's complete acceptance grammar.
    // Never normalize an effective key during composition or journal recovery.
    internal static class RunRequestKey
    {
        internal static bool IsSupported(string key, int minimumLength = 16, int maximumLength = 128)
        {
            if (key == null || key.Length < minimumLength || key.Length > maximumLength) return false;
            if (key[0] == ' ' || key[key.Length - 1] == ' ') return false;
            foreach (var c in key) if (c < 0x20 || c > 0x7e) return false;
            return true;
        }
    }
}
