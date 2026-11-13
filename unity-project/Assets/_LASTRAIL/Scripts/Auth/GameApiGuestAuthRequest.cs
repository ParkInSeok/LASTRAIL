using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiGuestAuthRequest
    {
        public const string MethodConstant = "POST";
        public const string EndpointPathConstant = "/v1/auth/guest";

        private GameApiGuestAuthRequest()
        {
            Headers = new List<RunApiHttpHeader>().AsReadOnly();
        }

        public string Method => MethodConstant;
        public string EndpointPath => EndpointPathConstant;
        public string BodyJson => null;
        public IReadOnlyList<RunApiHttpHeader> Headers { get; }

        public static GameApiGuestAuthRequest Create()
        {
            return new GameApiGuestAuthRequest();
        }
    }
}
