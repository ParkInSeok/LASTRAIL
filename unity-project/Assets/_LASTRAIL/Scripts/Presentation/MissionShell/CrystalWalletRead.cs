using System;
using LastRail.Auth;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public enum CrystalWalletReadStatus { Unavailable, Ready, Unconfigured, AuthRequired, Invalid }

    // Decodes already-received data only. No transport, session access or mutation.
    public sealed class CrystalWalletRead
    {
        public CrystalWalletReadStatus Status { get; private set; }
        public string CurrencyId { get; private set; }
        public string PolicyVersion { get; private set; }
        public long? FreeBalance { get; private set; }
        public long? PaidBalance { get; private set; }
        public long? TotalBalance { get; private set; }
        public CrystalTicketExchangeRead TicketExchange { get; private set; }
        public bool? PurchaseConfigured { get; private set; }
        internal long? MutationSequence { get; private set; }
        internal string AccountId { get; private set; }

        internal bool TryApplyClaim(JObject snapshot, out CrystalWalletRead next)
        {
            next = this;
            if ((string)snapshot["kind"] == "UNCHANGED") return true;
            if (Status != CrystalWalletReadStatus.Ready || CurrencyId != (string)snapshot["currencyId"] ||
                PolicyVersion != (string)snapshot["policyVersion"]) return false;
            long version = (long)snapshot["sourceMutation"]["mutationSequence"];
            long free = (long)snapshot["freeBalance"], paid = (long)snapshot["paidBalance"], total = (long)snapshot["totalBalance"];
            // Never infer a legacy observation's sequence from matching balances.
            if (!MutationSequence.HasValue) return false;
            if (version < MutationSequence.Value) return true;
            if (MutationSequence == version) return FreeBalance == free && PaidBalance == paid && TotalBalance == total;
            next = (CrystalWalletRead)MemberwiseClone();
            next.MutationSequence = version; next.FreeBalance = free; next.PaidBalance = paid; next.TotalBalance = total;
            return true;
        }

        private CrystalWalletRead(CrystalWalletReadStatus status) { Status = status; }

        internal CrystalWalletRead PreserveNewerBalances(CrystalWalletRead previous)
        {
            if(previous?.MutationSequence==null)return this;
            if(Status!=CrystalWalletReadStatus.Ready){var failed=(CrystalWalletRead)previous.MemberwiseClone();failed.Status=Status;return failed;}
            if(!MutationSequence.HasValue){var invalid=(CrystalWalletRead)previous.MemberwiseClone();invalid.Status=CrystalWalletReadStatus.Invalid;return invalid;}
            // Policy/configuration is not ordered by the account mutation watermark.
            if(AccountId!=previous.AccountId||CurrencyId!=previous.CurrencyId||PolicyVersion!=previous.PolicyVersion)return this;
            if(MutationSequence>previous.MutationSequence)return this;
            var next=(CrystalWalletRead)MemberwiseClone();
            if(MutationSequence==previous.MutationSequence&&(FreeBalance!=previous.FreeBalance||PaidBalance!=previous.PaidBalance||TotalBalance!=previous.TotalBalance))next.Status=CrystalWalletReadStatus.Invalid;
            next.MutationSequence=previous.MutationSequence;next.FreeBalance=previous.FreeBalance;next.PaidBalance=previous.PaidBalance;next.TotalBalance=previous.TotalBalance;
            return next;
        }

        public static CrystalWalletRead Parse(int httpStatus,string json)=>ParseForAccount(httpStatus,json,null);
        public static CrystalWalletRead ParseForAccount(int httpStatus, string json, string expectedAccount)
        {
            if (httpStatus == 401) return new CrystalWalletRead(CrystalWalletReadStatus.AuthRequired);
            if (httpStatus != 200 && httpStatus != 503) return new CrystalWalletRead(CrystalWalletReadStatus.Unavailable);
            try
            {
                var root = GameApiJson.ReadObject(json);
                if (httpStatus == 503)
                    return new CrystalWalletRead(!GameApiJson.Boolean(root, "accepted")
                        && GameApiJson.String(root, "errorCode") == "crystal_policy_unconfigured"
                        ? CrystalWalletReadStatus.Unconfigured : CrystalWalletReadStatus.Unavailable);
                if (root["accepted"] != null && (root["accepted"].Type != JTokenType.Boolean || !(bool)root["accepted"]))
                    throw new InvalidOperationException();
                if (root["errorCode"] != null && root["errorCode"].Type != JTokenType.Null) throw new InvalidOperationException();
                var free = Amount(root, "freeBalance", 0, 9007199254740991L);
                var paid = Amount(root, "paidBalance", 0, 9007199254740991L);
                var total = Amount(root, "totalBalance", 0, 9007199254740991L);
                if (checked(free + paid) != total) throw new InvalidOperationException();
                var order = root["spendOrder"] as JArray;
                if (order == null || order.Count != 2 || order[0].Type != JTokenType.String || order[1].Type != JTokenType.String
                    || (string)order[0] != "FREE" || (string)order[1] != "PAID") throw new InvalidOperationException();
                if (root["ticketExchange"] == null) throw new InvalidOperationException();
                CrystalTicketExchangeRead exchange = null;
                if (root["ticketExchange"].Type != JTokenType.Null)
                {
                    var value = GameApiJson.Object(root, "ticketExchange");
                    var id = Text(value, "ticketItemId");
                    if (id != "cur_command_ticket") throw new InvalidOperationException();
                    exchange = new CrystalTicketExchangeRead(id, Amount(value, "crystalsPerTicket", 1, int.MaxValue),
                        Amount(value, "maxTickets", 1, int.MaxValue));
                }
                string account=null;long? sequence=null;
                if(root["stateVersion"]!=null){
                    var version=GameApiJson.Object(root,"stateVersion");
                    if(version.Count!=4||Text(version,"version")!="crystal-wallet.snapshot.v1.20261022"||
                        !(version["coverage"] is JArray coverage)||coverage.Count!=1||(string)coverage[0]!="BALANCES")throw new FormatException("Wallet observation coverage/version");
                    account=Text(version,"accountId");if(expectedAccount!=null&&account!=expectedAccount)throw new FormatException("Wallet observation account");
                    sequence=Amount(version,"mutationSequence",0,9007199254740991L);
                }
                return new CrystalWalletRead(CrystalWalletReadStatus.Ready)
                {
                    AccountId=account,MutationSequence=sequence,
                    CurrencyId = Text(root, "currencyId"), PolicyVersion = Text(root, "policyVersion"),
                    FreeBalance = free, PaidBalance = paid, TotalBalance = total, TicketExchange = exchange,
                    PurchaseConfigured = GameApiJson.Boolean(root, "purchaseConfigured")
                };
            }
            catch { return new CrystalWalletRead(httpStatus == 200 ? CrystalWalletReadStatus.Invalid : CrystalWalletReadStatus.Unavailable); }
        }

        public static CrystalRewardRead ParseReward(string json, string origin)
        {
            try
            {
                CrystalPolicyPresentation.RewardSourceLabel(origin);
                var root = GameApiJson.ReadObject(json);
                if (GameApiJson.String(root, "kind") != "CRYSTAL" || GameApiJson.String(root, "source") != "FREE")
                    throw new InvalidOperationException();
                var linked = GameApiJson.Boolean(root, "purchaseLinked");
                if (linked != (origin == "PREMIUM_PASS")) throw new InvalidOperationException();
                return new CrystalRewardRead(Text(root, "id"), Amount(root, "quantity", 1, int.MaxValue), linked);
            }
            catch { throw new InvalidOperationException("Invalid crystal reward contract."); }
        }

        private static long Amount(JObject root, string name, long minimum, long maximum)
        {
            if (root[name]?.Type != JTokenType.Integer) throw new InvalidOperationException();
            var value = root[name].Value<long>();
            if (value < minimum || value > maximum) throw new InvalidOperationException();
            return value;
        }

        private static string Text(JObject root, string name)
        {
            var value = GameApiJson.String(root, name);
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw new InvalidOperationException();
            foreach (var letter in value) if (char.IsControl(letter)) throw new InvalidOperationException();
            return value;
        }
    }

    public sealed class CrystalTicketExchangeRead
    {
        public string TicketItemId { get; }
        public long CrystalsPerTicket { get; }
        public long MaxTickets { get; }
        internal CrystalTicketExchangeRead(string id, long cost, long maximum) { TicketItemId = id; CrystalsPerTicket = cost; MaxTickets = maximum; }
    }

    public sealed class CrystalRewardRead
    {
        public string CurrencyId { get; }
        public long Quantity { get; }
        public string Source => "FREE";
        public bool PurchaseLinked { get; }
        internal CrystalRewardRead(string id, long quantity, bool linked) { CurrencyId = id; Quantity = quantity; PurchaseLinked = linked; }
    }
}
