using System;
using LastRail.Auth;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    // Server observation only. This snapshot never authorizes a reward or creates a store transaction.
    public sealed class AdSkipSnapshot
    {
        public bool Owned { get; }
        public string Status { get; }
        public long Revision { get; }
        public bool PurchaseAvailable { get; }
        public bool CatalogConfigured { get; }
        public string OfflineAvailability { get; }
        public string CombatAvailability { get; }
        public string RerollAvailability { get; }
        AdSkipSnapshot(JObject root)
        {
            if(!GameApiJson.Boolean(root,"accepted")||GameApiJson.String(root,"contractVersion")!="ad-skip-v1")throw new FormatException("Entitlement contract version");
            Owned=GameApiJson.Boolean(root,"owned");PurchaseAvailable=GameApiJson.Boolean(root,"purchaseAvailable");
            Number(root,"serverTime");
            if(root["entitlement"] is JObject entitlement)
            {
                Status=GameApiJson.String(entitlement,"status");
                if(Status!="ACTIVE"&&Status!="REVOKED")throw new FormatException("Entitlement status");
                Revision=Number(entitlement,"revision");
                if(!GameApiJson.Boolean(entitlement,"permanent")||Owned!=(Status=="ACTIVE"))throw new FormatException("Entitlement ownership");
            }
            else if(root["entitlement"]?.Type!=JTokenType.Null||Owned)throw new FormatException("Missing entitlement");
            if(root["catalog"] is JObject catalog)
            {
                string provider=GameApiJson.String(catalog,"provider");
                if(provider!="GOOGLE_PLAY"&&provider!="APP_STORE")throw new FormatException("Store provider");
                OfflineAdIntent.Text(catalog,"version");OfflineAdIntent.Text(catalog,"appId");OfflineAdIntent.Text(catalog,"productId");
                if(catalog["price"]?.Type!=JTokenType.Null||GameApiJson.String(catalog,"priceSource")!="STORE_REQUIRED")throw new FormatException("Store price source");
                CatalogConfigured=true;
            }
            else if(root["catalog"]?.Type!=JTokenType.Null||PurchaseAvailable)throw new FormatException("Missing catalog");
            var features=GameApiJson.Object(root,"featureAvailability");
            OfflineAvailability=OfflineAdIntent.Text(features,"OFFLINE");
            CombatAvailability=OfflineAdIntent.Text(features,"COMBAT");
            RerollAvailability=OfflineAdIntent.Text(features,"REROLL");
        }
        static long Number(JObject root,string key)
        {
            if(root[key]?.Type!=JTokenType.Integer)throw new FormatException(key);
            long value=root[key].Value<long>();if(value<0||value>9007199254740991L)throw new FormatException(key);return value;
        }
        public static AdSkipSnapshot Parse(string json)=>new AdSkipSnapshot(GameApiJson.ReadObject(json));
    }
}
