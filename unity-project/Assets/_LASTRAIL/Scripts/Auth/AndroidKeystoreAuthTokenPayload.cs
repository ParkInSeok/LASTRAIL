using System;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreAuthTokenPayload
    {
        public AndroidKeystoreAuthTokenPayload(
            string accountId,
            string accountStatus,
            string accessTokenValue,
            DateTime accessTokenExpiresAt,
            string refreshTokenValue,
            DateTime refreshTokenExpiresAt)
        {
            AccountId = RequireText(accountId, nameof(accountId));
            AccountStatus = RequireText(accountStatus, nameof(accountStatus));
            AccessTokenValue = RequireText(accessTokenValue, nameof(accessTokenValue));
            AccessTokenExpiresAt = accessTokenExpiresAt;
            RefreshTokenValue = RequireText(refreshTokenValue, nameof(refreshTokenValue));
            RefreshTokenExpiresAt = refreshTokenExpiresAt;
        }

        public string AccountId { get; }
        public string AccountStatus { get; }
        public string AccessTokenValue { get; }
        public DateTime AccessTokenExpiresAt { get; }
        public string RefreshTokenValue { get; }
        public DateTime RefreshTokenExpiresAt { get; }

        public static AndroidKeystoreAuthTokenPayload FromSnapshot(GameApiAuthSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new AndroidKeystoreAuthTokenPayload(
                snapshot.AccountId,
                snapshot.AccountStatus,
                snapshot.AccessToken.Value,
                snapshot.AccessTokenExpiresAt,
                snapshot.RefreshToken.Value,
                snapshot.RefreshTokenExpiresAt);
        }

        public GameApiAuthSessionSnapshot ToSnapshot()
        {
            return new GameApiAuthSessionSnapshot(
                AccountId,
                AccountStatus,
                new GameApiAccessToken(AccessTokenValue),
                AccessTokenExpiresAt,
                new GameApiRefreshToken(RefreshTokenValue),
                RefreshTokenExpiresAt);
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
