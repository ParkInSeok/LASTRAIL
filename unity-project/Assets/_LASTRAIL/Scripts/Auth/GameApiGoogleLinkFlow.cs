using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public static class GameApiGoogleLinkFlow
    {
        public static async Task<GameApiGoogleLinkFlowResult> LinkAsync(
            IGoogleIdTokenProvider provider,
            IGameApiGoogleLinkClient client,
            GameApiAuthSessionSnapshot existingSession)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (existingSession == null)
            {
                throw new ArgumentNullException(nameof(existingSession));
            }

            var providerResult = await provider.GetIdTokenAsync();
            if (providerResult == null)
            {
                throw new InvalidOperationException("Google ID token provider returned null.");
            }

            if (providerResult.Outcome != GoogleIdTokenProviderOutcome.Success)
            {
                return GameApiGoogleLinkFlowResult.FromProviderFailure(providerResult);
            }

            var clientResult = await client.LinkAsync(GameApiGoogleLinkRequest.Create(providerResult.IdToken), existingSession.AccessToken);
            return GameApiGoogleLinkFlowResult.FromClientResult(providerResult.Outcome, clientResult);
        }
    }
}
