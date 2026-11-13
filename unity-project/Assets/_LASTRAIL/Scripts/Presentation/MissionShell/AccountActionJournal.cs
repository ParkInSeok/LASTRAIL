using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed class PendingAccountAction
    {
        public string Account {get;} public string Kind {get;} public string Target {get;} public int Count {get;} public string Key {get;} public string Payload {get;}
        public string StartResultJson {get;}
        public PendingAccountAction(string account,string kind,string target,int count,string key,string payload=null,string startResultJson=null)
        {
            bool approval=kind=="reward-approval";
            bool combat=kind=="combat-ad-create"||kind=="combat-ad-intent"||kind=="combat-ad-presented"||kind=="combat-ad-spin";
            bool ad=combat||kind=="ad-create"||kind=="ad-intent"||kind=="ad-presented"||kind=="ad-spin";
            bool formation=kind=="formation-save"||kind=="formation-activate";
            bool bulk=kind=="recurring-bulk";bool shop=kind=="shop-offer";bool recurring=!bulk&&kind!=null&&kind.StartsWith("recurring-");bool monthly=kind=="monthly-auto"||kind=="monthly-recovery";bool exchange=kind=="ticket-exchange";bool ultimate=kind=="ultimate-selection";bool trainLoadout=kind=="train-loadout";bool departure=kind=="departure-preview"||kind=="departure-start";
            bool growth=kind=="growth-core"||kind=="growth-skill"||kind=="growth-promotion"||kind=="growth-replace"||kind=="growth-sync"||kind=="growth-train";
            bool validKey=approval||ad||departure||ultimate||exchange||monthly||recurring||shop||bulk?key!=null&&Regex.IsMatch(key,@"\A[\x21-\x7e]{1,128}\z"):Id(key);
            if(!Id(account)||!validKey||(kind!="offline"&&kind!="gacha"&&!ad&&!formation&&!growth&&!trainLoadout&&!departure&&!ultimate&&!exchange&&!monthly&&!recurring&&!shop&&!bulk&&!approval)||kind=="gacha"&&(!Id(target)||(count!=1&&count!=10))||kind=="offline"&&(target!=""||count!=0)||!ad&&!formation&&!growth&&!trainLoadout&&!departure&&!ultimate&&!exchange&&!monthly&&!recurring&&!shop&&!bulk&&!approval&&payload!=null)throw new FormatException("Invalid pending action.");
            if(approval){var request=RewardApprovalRequest.Parse(account,payload);if(count!=0||target!=request.Target)throw new FormatException("approval target");}
            if(combat){if(count!=0)throw new FormatException();CombatAdIntent.ValidatePending(kind,target,payload);}
            else if(ad){if(count!=0||kind=="ad-create"&&(target!=""||payload!=null))throw new FormatException();if(kind!="ad-create"&&OfflineAdIntent.Parse(payload).Id!=target)throw new FormatException();}
            if(bulk){if(count!=0||target!="bulk")throw new FormatException();BulkClaimContract.Request(payload);}
            if(shop){if(count!=0||target!="cur_command_ticket")throw new FormatException();ShopOfferReceipt.Request(payload);}
            if(recurring){if(count!=0)throw new FormatException();RecurringContract.Endpoint(kind,target,payload);}
            if(monthly){if(count!=0)throw new FormatException();MonthlyAttendanceContract.Request(kind,target,payload);}
            if(exchange){if(count!=0)throw new FormatException();TicketExchangeReceipt.Validate(target,payload);}
            if(ultimate){if(count!=0)throw new FormatException();UltimateSelectionSnapshot.ValidateRequest(target,payload);}
            if(departure){if(count!=0)throw new FormatException();DepartureContract.Validate(kind,target,payload);}
            if(trainLoadout){if(count!=0)throw new FormatException();TrainLoadoutWrite.Validate(target,payload);}
            if(formation){if(count!=0)throw new FormatException();FormationWrite.Validate(kind,target,payload);}
            if(growth){if(count!=0)throw new FormatException();OperatorGrowthWrite.Validate(kind,target,payload);}
            if(startResultJson!=null&&(kind!="departure-start"||startResultJson.Length>PendingRunOperation.MaximumStartResponse||new DepartureStartResult(200,startResultJson,target).Status!="COMMITTED"))throw new FormatException();
            Account=account;Kind=kind;Target=target;Count=count;Key=key;Payload=payload;StartResultJson=startResultJson;
        }
        static bool Id(string s)=>s!=null&&Regex.IsMatch(s,"^[A-Za-z0-9_-]{1,128}$");
    }
    public interface IAccountActionJournal
    {
        PendingAccountAction Read(string account);
        bool Save(PendingAccountAction action);
        bool Remove(PendingAccountAction action);
    }
    // Dedicated encrypted file, never auth-token storage. Metadata only, no credentials.
    public sealed class AccountActionJournal : IAccountActionJournal,IAccountStartReceiptJournal,IAccountAdActionJournal,IAccountRewardApprovalJournal,ISecureStorageWriteDiagnostics
    {
        const int MaximumNewStartReceiptBytes=PendingRunOperation.MaximumStartResponse;
        // Keep historical character-bound records readable; enforce bytes only on new writes.
        static bool StartReceiptFits(string json)=>json==null||Encoding.UTF8.GetByteCount(json)<=MaximumNewStartReceiptBytes&&new JValue(json).ToString(Newtonsoft.Json.Formatting.None).Length<=MaximumNewStartReceiptBytes;
        readonly IAndroidKeystoreNativeStoragePort port;
        public SecureStorageWriteFailure LastWriteFailure {get;private set;}
        public AccountActionJournal(IAndroidKeystoreNativeStoragePort port){this.port=port??throw new ArgumentNullException(nameof(port));}
        List<PendingAccountAction> Load()
        {
            if(!port.IsAndroidRuntime||!port.IsKeystoreAvailable||port.UsedPlayerPrefsFallback)throw new InvalidOperationException("Secure action storage unavailable.");
            var saved=port.Load();if(saved==null)return new List<PendingAccountAction>();
            var root=JObject.Parse(port.Decrypt(saved));if((string)root["schema"]!="account-actions-v1"||!(root["actions"] is JArray rows)||rows.Count>32)throw new FormatException();
            var result=rows.Select(r=>new PendingAccountAction((string)r["account"],(string)r["kind"],(string)r["target"],(int)r["count"],(string)r["key"],(string)r["payload"],(string)r["startResultJson"])).ToList();
            if(result.Select(r=>r.Account).Distinct().Count()!=result.Count)throw new FormatException();return result;
        }
        bool Write(List<PendingAccountAction> rows){
            LastWriteFailure=SecureStorageWriteFailure.Unknown;
            bool saved=port.Save(port.Encrypt(new JObject{["schema"]="account-actions-v1",["actions"]=new JArray(rows.Select(a=>new JObject{["account"]=a.Account,["kind"]=a.Kind,["target"]=a.Target,["count"]=a.Count,["key"]=a.Key,["payload"]=a.Payload,["startResultJson"]=a.StartResultJson}))}.ToString()));
            LastWriteFailure=saved?SecureStorageWriteFailure.None:(port as ISecureStorageWriteDiagnostics)?.LastWriteFailure??SecureStorageWriteFailure.Unknown;
            return saved;
        }
        public PendingAccountAction Read(string account)=>Load().SingleOrDefault(a=>a.Account==account);
        public bool Save(PendingAccountAction action)
        {
            LastWriteFailure=SecureStorageWriteFailure.None;
            var rows=Load();var old=rows.SingleOrDefault(a=>a.Account==action.Account);
            if(old!=null)return old.Key==action.Key&&old.Kind==action.Kind&&old.Target==action.Target&&old.Count==action.Count&&old.Payload==action.Payload;
            if(rows.Count>=32||!StartReceiptFits(action.StartResultJson)){LastWriteFailure=SecureStorageWriteFailure.LogicalLimit;return false;}rows.Add(action);return Write(rows);
        }
        public bool Remove(PendingAccountAction action){var rows=Load();rows.RemoveAll(a=>a.Account==action.Account&&a.Key==action.Key);return Write(rows);}
        public bool ReplaceAd(PendingAccountAction expected,PendingAccountAction replacement)
        {
            if(expected==null||replacement==null||expected.Account!=replacement.Account)return false;
            bool combat=expected.Kind.StartsWith("combat-ad-");string prefix=combat?"combat-ad-":"ad-";
            if(!expected.Kind.StartsWith(prefix)||!replacement.Kind.StartsWith(prefix)||replacement.Kind==prefix+"create")return false;
            var rows=Load();int index=rows.FindIndex(a=>a.Account==expected.Account);if(index<0)return false;var old=rows[index];
            if(old.Kind!=expected.Kind||old.Key!=expected.Key||old.Target!=expected.Target||old.Payload!=expected.Payload)return false;
            if(combat){var prior=JObject.Parse(expected.Payload);var next=CombatAdIntent.Parse(replacement.Payload,(string)prior["runId"],(string)prior["rewardPreviewId"]);if(expected.Target!=replacement.Target)return false;if(expected.Kind!="combat-ad-create")next.RequireSame(CombatAdIntent.Parse(expected.Payload,next.RunId,next.PreviewId));}
            else if(expected.Kind!="ad-create")OfflineAdIntent.Parse(replacement.Payload).RequireSame(OfflineAdIntent.Parse(expected.Payload));
            rows[index]=replacement;return Write(rows);
        }
        public bool ReplaceApproval(PendingAccountAction expected,PendingAccountAction replacement)
        {
            if(expected==null||replacement==null||expected.Account!=replacement.Account)return false;
            if(replacement.Kind=="reward-approval"){
                var request=RewardApprovalRequest.Parse(replacement.Account,replacement.Payload);
                if(!RewardApprovalRequest.Same(expected,request.Prior))return false;
            }else if(expected.Kind=="reward-approval"){
                var request=RewardApprovalRequest.Parse(expected.Account,expected.Payload);
                if(!RewardApprovalRequest.Same(replacement,request.Prior))request.ValidateApproved(replacement);
            }else return false;
            var rows=Load();int index=rows.FindIndex(a=>a.Account==expected.Account);
            if(index<0||!RewardApprovalRequest.Same(rows[index],expected))return false;
            rows[index]=replacement;return Write(rows);
        }
        public bool ConfirmStart(PendingAccountAction original,string resultJson)
        {
            LastWriteFailure=SecureStorageWriteFailure.None;
            var rows=Load();var index=rows.FindIndex(a=>a.Account==original.Account);if(index<0)return false;var saved=rows[index];
            if(saved.Kind!="departure-start"||saved.Kind!=original.Kind||saved.Key!=original.Key||saved.Payload!=original.Payload||saved.Target!=original.Target)return false;
            if(saved.StartResultJson!=null){var before=new DepartureStartResult(200,saved.StartResultJson,saved.Target);var after=new DepartureStartResult(200,resultJson,saved.Target);return after.Status=="COMMITTED"&&before.RunId==after.RunId&&JToken.DeepEquals(before.StartResponse,after.StartResponse);}
            if(!StartReceiptFits(resultJson)){LastWriteFailure=SecureStorageWriteFailure.LogicalLimit;return false;}
            var confirmed=new PendingAccountAction(saved.Account,saved.Kind,saved.Target,saved.Count,saved.Key,saved.Payload,resultJson);
            rows[index]=confirmed;return Write(rows);
        }
    }
}



