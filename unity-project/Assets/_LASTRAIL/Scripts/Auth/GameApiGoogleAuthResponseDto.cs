using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleAuthResponseDto
    {
        public GameApiGoogleAuthResponseDto(GameApiGuestAuthResponseDto tokenBundle)
        {
            TokenBundle = tokenBundle ?? throw new ArgumentNullException(nameof(tokenBundle));
        }

        public GameApiGuestAuthResponseDto TokenBundle { get; }
        public GameApiTokenType TokenType => TokenBundle.TokenType;
        public GameApiAccountDto Account => TokenBundle.Account;
        public GameApiAccessToken AccessToken => TokenBundle.AccessToken;
        public DateTime AccessTokenExpiresAt => TokenBundle.AccessTokenExpiresAt;
        public GameApiRefreshToken RefreshToken => TokenBundle.RefreshToken;
        public DateTime RefreshTokenExpiresAt => TokenBundle.RefreshTokenExpiresAt;
        public bool ContainsSecretStorageMaterial => false;
    }
}
