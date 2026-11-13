#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode
{
    // Test-only environment boundary. Never configures the installed game's server.
    public sealed class ApiVerificationConfiguration
    {
        public string Origin { get; private set; }
        public string AccountId { get; private set; }
        public string Token { get; private set; }
        public string Profile { get; private set; }
        public string EnvironmentName { get; private set; }
        public string ServerRevision { get; private set; }
        public long VersionCode { get; private set; }
        public static ApiVerificationConfiguration Parse(JObject input)
        {
            try {
                if (input == null || input["disposable"]?.Type != JTokenType.Boolean || (bool)input["disposable"] != true)
                    throw new ArgumentException();
                var origin=Text(input,"origin");
                if (!Uri.TryCreate(origin,UriKind.Absolute,out var uri) || uri.UserInfo.Length!=0
                    || uri.AbsolutePath!="/" || uri.Query.Length!=0 || uri.Fragment.Length!=0
                    || !(uri.Scheme=="http" && uri.Host=="127.0.0.1" && uri.Port>0
                        || uri.Scheme=="https" && uri.Host=="dev-api.lastrail.app" && uri.Port==443)) throw new ArgumentException();
                var profile=Text(input,"profile"); var environment=Text(input,"environment");
                if (profile!="ACCOUNT_READ_LOCAL" && profile!="SUPPORT_LOCAL_DEV") throw new ArgumentException();
                if (environment!="LOCAL_SQLJS" && environment!="DEV_ISOLATED") throw new ArgumentException();
                if (uri.Scheme=="http" && environment!="LOCAL_SQLJS" || uri.Scheme=="https" && environment!="DEV_ISOLATED"
                    || profile=="ACCOUNT_READ_LOCAL" && environment!="LOCAL_SQLJS") throw new ArgumentException();
                var authorization=Text(input,"authorization");
                if (!authorization.StartsWith("Bearer ",StringComparison.Ordinal) || authorization.Length<=7
                    || authorization.Contains("\r") || authorization.Contains("\n")) throw new ArgumentException();
                var revision=Text(input,"serverRevision");
                if (revision.Length!=40) throw new ArgumentException();
                foreach(var c in revision) if (!(c>='0'&&c<='9'||c>='a'&&c<='f')) throw new ArgumentException();
                if (input["appVersionCode"]?.Type!=JTokenType.Integer || input["expiresAt"]?.Type!=JTokenType.Integer)
                    throw new ArgumentException();
                var code=(long)input["appVersionCode"];
                if (code<=0 || code>int.MaxValue || (long)input["expiresAt"]<=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) throw new ArgumentException();
                return new ApiVerificationConfiguration { Origin=uri.GetLeftPart(UriPartial.Authority), AccountId=Text(input,"accountId"),
                    Token=authorization.Substring(7),Profile=profile,EnvironmentName=environment,ServerRevision=revision,VersionCode=code };
            } catch { throw new ArgumentException("Invalid isolated API verification configuration."); }
        }
        public static ApiVerificationConfiguration ParseOther(JObject input,ApiVerificationConfiguration primary) {
            var other=Parse(input);
            if(primary==null||primary.Profile!="SUPPORT_LOCAL_DEV"||primary.EnvironmentName!="LOCAL_SQLJS"
                ||other.Profile!=primary.Profile||other.EnvironmentName!=primary.EnvironmentName||other.Origin!=primary.Origin
                ||other.ServerRevision!=primary.ServerRevision||other.VersionCode!=primary.VersionCode||other.AccountId==primary.AccountId||other.Token==primary.Token)
                throw new ArgumentException("Secondary isolated account must share the same supplier boundary.");
            return other;
        }
        static string Text(JObject input,string key) {
            if (input[key]?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)input[key])) throw new ArgumentException();
            return (string)input[key];
        }
        public JObject SafeMetadata() => new JObject { ["origin"]=Origin,["profile"]=Profile,["environment"]=EnvironmentName,
            ["serverRevision"]=ServerRevision,["appVersionCode"]=VersionCode,["clientEnvironment"]="UNITY_EDITOR",
            ["transport"]="UnityWebRequestRunApiHttpTransport/InstalledAdmissionTransport",["disposable"]=true };
    }
}
#endif
