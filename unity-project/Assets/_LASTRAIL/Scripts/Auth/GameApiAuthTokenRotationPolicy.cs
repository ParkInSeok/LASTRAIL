using System;

namespace LastRail.Auth
{
    public static class GameApiAuthTokenRotationPolicy
    {
        public static GameApiAuthSessionSnapshot RotateFromRefreshResponse(
            GameApiAuthSessionSnapshot existing,
            GameApiGuestAuthResponseDto refreshResponse)
        {
            if (existing == null)
            {
                throw new ArgumentNullException(nameof(existing));
            }

            if (refreshResponse == null)
            {
                throw new ArgumentNullException(nameof(refreshResponse));
            }

            if (!string.Equals(existing.AccountId, refreshResponse.Account.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException("Refresh response account must match stored account.", nameof(refreshResponse));
            }

            return new GameApiAuthSessionSnapshot(
                refreshResponse.Account.Id,
                refreshResponse.Account.Status,
                refreshResponse.AccessToken,
                refreshResponse.AccessTokenExpiresAt,
                refreshResponse.RefreshToken,
                refreshResponse.RefreshTokenExpiresAt);
        }
    }
}
