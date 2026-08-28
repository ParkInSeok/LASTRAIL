using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleLinkResponseDto
    {
        public GameApiGoogleLinkResponseDto(GameApiGuestAuthResponseDto tokenBundle, GameApiGoogleLinkStatus linkStatus)
        {
            TokenBundle = tokenBundle ?? throw new ArgumentNullException(nameof(tokenBundle));
            LinkStatus = linkStatus;
        }

        public GameApiGuestAuthResponseDto TokenBundle { get; }
        public GameApiGoogleLinkStatus LinkStatus { get; }
        public GameApiTokenType TokenType => TokenBundle.TokenType;
        public GameApiAccountDto Account => TokenBundle.Account;
        public GameApiAccessToken AccessToken => TokenBundle.AccessToken;
        public DateTime AccessTokenExpiresAt => TokenBundle.AccessTokenExpiresAt;
        public GameApiRefreshToken RefreshToken => TokenBundle.RefreshToken;
        public DateTime RefreshTokenExpiresAt => TokenBundle.RefreshTokenExpiresAt;
        public bool ContainsSecretStorageMaterial => false;
    }
}
