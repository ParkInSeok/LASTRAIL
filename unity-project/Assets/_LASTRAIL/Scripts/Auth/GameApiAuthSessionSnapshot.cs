using System;

namespace LastRail.Auth
{
    public sealed class GameApiAuthSessionSnapshot
    {
        public GameApiAuthSessionSnapshot(
            string accountId,
            string accountStatus,
            GameApiAccessToken accessToken,
            DateTime accessTokenExpiresAt,
            GameApiRefreshToken refreshToken,
            DateTime refreshTokenExpiresAt)
        {
            RequireText(accountId, nameof(accountId));
            RequireText(accountStatus, nameof(accountStatus));

            AccountId = accountId;
            AccountStatus = accountStatus;
            AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            AccessTokenExpiresAt = accessTokenExpiresAt;
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            RefreshTokenExpiresAt = refreshTokenExpiresAt;
        }

        public string AccountId { get; }
        public string AccountStatus { get; }
        public GameApiAccessToken AccessToken { get; }
        public DateTime AccessTokenExpiresAt { get; }
        public GameApiRefreshToken RefreshToken { get; }
        public DateTime RefreshTokenExpiresAt { get; }

        public static GameApiAuthSessionSnapshot FromGuestResponse(GameApiGuestAuthResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new GameApiAuthSessionSnapshot(
                response.Account.Id,
                response.Account.Status,
                response.AccessToken,
                response.AccessTokenExpiresAt,
                response.RefreshToken,
                response.RefreshTokenExpiresAt);
        }

        public static GameApiAuthSessionSnapshot FromGoogleAuthResponse(GameApiGoogleAuthResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return FromGuestResponse(response.TokenBundle);
        }

        public static GameApiAuthSessionSnapshot FromGoogleLinkResponse(GameApiGoogleLinkResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return FromGuestResponse(response.TokenBundle);
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
