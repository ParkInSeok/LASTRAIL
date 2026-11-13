using System;

namespace LastRail.Auth
{
    public sealed class GameApiGuestAuthResponseDto
    {
        public GameApiGuestAuthResponseDto(
            GameApiTokenType tokenType,
            GameApiAccountDto account,
            GameApiAccessToken accessToken,
            DateTime accessTokenExpiresAt,
            GameApiRefreshToken refreshToken,
            DateTime refreshTokenExpiresAt)
        {
            if (accessTokenExpiresAt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Access token expiry must be UTC.", nameof(accessTokenExpiresAt));
            }

            if (refreshTokenExpiresAt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Refresh token expiry must be UTC.", nameof(refreshTokenExpiresAt));
            }

            TokenType = tokenType;
            Account = account ?? throw new ArgumentNullException(nameof(account));
            AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            AccessTokenExpiresAt = accessTokenExpiresAt;
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            RefreshTokenExpiresAt = refreshTokenExpiresAt;
        }

        public GameApiTokenType TokenType { get; }
        public GameApiAccountDto Account { get; }
        public GameApiAccessToken AccessToken { get; }
        public DateTime AccessTokenExpiresAt { get; }
        public GameApiRefreshToken RefreshToken { get; }
        public DateTime RefreshTokenExpiresAt { get; }
        public bool ContainsSecretStorageMaterial => false;
    }
}
