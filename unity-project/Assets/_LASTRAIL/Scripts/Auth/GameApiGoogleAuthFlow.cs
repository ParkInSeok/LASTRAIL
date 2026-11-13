using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public static class GameApiGoogleAuthFlow
    {
        public static async Task<GameApiGoogleAuthFlowResult> SignInAsync(IGoogleIdTokenProvider provider, IGameApiGoogleAuthClient client)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var providerResult = await provider.GetIdTokenAsync();
            if (providerResult == null)
            {
                throw new InvalidOperationException("Google ID token provider returned null.");
            }

            if (providerResult.Outcome != GoogleIdTokenProviderOutcome.Success)
            {
                return GameApiGoogleAuthFlowResult.FromProviderFailure(providerResult);
            }

            var clientResult = await client.SignInAsync(GameApiGoogleAuthRequest.Create(providerResult.IdToken));
            return GameApiGoogleAuthFlowResult.FromClientResult(providerResult.Outcome, clientResult);
        }
    }
}
