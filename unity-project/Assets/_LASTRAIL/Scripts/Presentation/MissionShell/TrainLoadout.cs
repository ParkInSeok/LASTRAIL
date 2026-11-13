using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class TrainLoadoutSnapshot
    {
        readonly JObject row;
        public JObject Train=>row["train"] as JObject==null?null:(JObject)row["train"].DeepClone();
        public JArray Carriages=>(JArray)row["ownedCarriages"].DeepClone();public JArray Turrets=>(JArray)row["ownedTurrets"].DeepClone();
        public int? SlotCount=>(int?)row["unlockedSlotCount"];public string ProgressError=>(string)row["progressError"];public string LoadoutError=>(string)row["loadoutError"];
        public long Revision=>(long)row["revision"];public bool OpenRun=>(bool)row["openRun"];public string UpdatedAt=>(string)row["updatedAt"];
        public JObject Loadout=>row["loadout"] as JObject==null?null:(JObject)row["loadout"].DeepClone();
        public JObject Limits=>(JObject)row["typeLimits"].DeepClone();
        public TrainLoadoutSnapshot(string json)
        {
            row=GameApiJson.ReadObject(json);if(GameApiJson.String(row,"rulesVersion")!=TrainLoadoutWrite.Rules)throw new FormatException();
            if(row["train"]?.Type!=JTokenType.Null){var train=GameApiJson.Object(row,"train");TrainLoadoutWrite.Id(train,"trainId");Level(train);}
            var ids=new HashSet<string>();foreach(var token in RecruitmentJson.Array(row,"ownedCarriages")){
                var c=(JObject)token;if(!ids.Add(TrainLoadoutWrite.Id(c,"carriageId")))throw new FormatException();Level(c);var type=RecruitmentJson.NullableString(c,"type");if(type!=null&&!TrainLoadoutWrite.Types.Contains(type))throw new FormatException();
                var supported=GameApiJson.Boolean(c,"installationSupported");var eligible=GameApiJson.Boolean(c,"installable");var reason=RecruitmentJson.NullableString(c,"reason");if(eligible&&(!supported||type==null||reason!=null)||!eligible&&reason==null)throw new FormatException();
            }
            ids.Clear();foreach(var token in RecruitmentJson.Array(row,"ownedTurrets")){var t=(JObject)token;if(!ids.Add(TrainLoadoutWrite.Id(t,"turretId")))throw new FormatException();Level(t);if(GameApiJson.Boolean(t,"installationSupported")||GameApiJson.String(t,"reason")!="turret_installation_undefined")throw new FormatException();}
            var error=RecruitmentJson.NullableString(row,"progressError");if(error!=null&&error!="story_progress_invalid"&&error!="story_progress_inconsistent")throw new FormatException();
            if(row["unlockedSlotCount"]?.Type==JTokenType.Null){if(error==null)throw new FormatException();}else{var count=AccountStateSnapshot.Number(row,"unlockedSlotCount");if(count<2||count>4||error!=null)throw new FormatException();}
            var limits=GameApiJson.Object(row,"typeLimits");foreach(var type in TrainLoadoutWrite.Types){var t=GameApiJson.Object(limits,type);if(AccountStateSnapshot.Number(t,"min")>AccountStateSnapshot.Number(t,"max"))throw new FormatException();}
            AccountStateSnapshot.Number(row,"revision");GameApiJson.Boolean(row,"openRun");RecruitmentJson.NullableString(row,"loadoutError");RecruitmentJson.NullableString(row,"updatedAt");if(row["updatedAt"].Type!=JTokenType.Null)RecruitmentJson.Utc(row,"updatedAt");
            if(row["loadout"]?.Type!=JTokenType.Null)TrainLoadoutWrite.ValidateSelection(GameApiJson.Object(row,"loadout"));
        }
        static void Level(JObject r){if(AccountStateSnapshot.Number(r,"level")<1)throw new FormatException();AccountStateSnapshot.Number(r,"revision");}
    }
    public sealed class TrainLoadoutReceipt
    {
        public string Id {get;}public long Revision {get;}public bool Changed {get;}
        internal TrainLoadoutReceipt(string id,long revision,bool changed){Id=id;Revision=revision;Changed=changed;}
    }
    public sealed class TrainLoadoutWriteResult
    {
        public TrainLoadoutReceipt Receipt {get;}public string Error {get;}public bool Terminal {get;}
        internal TrainLoadoutWriteResult(TrainLoadoutReceipt receipt,string error,bool terminal){Receipt=receipt;Error=error;Terminal=terminal;}
    }
    public static class TrainLoadoutWrite
    {
        public const string Rules="train-loadout-v1";
        internal static readonly string[] Types={"COMBAT","GUN","CARGO","DEFENSE","MEDICAL"};
        internal static string Id(JObject r,string field){var id=RecruitmentJson.Id(r,field);if(id.Length>100)throw new FormatException();return id;}
        public static string[] ValidateSelection(JObject r)
        {
            if(r.Properties().Count()!=3||GameApiJson.String(r,"rulesVersion")!=Rules)throw new FormatException();Id(r,"trainId");var rows=RecruitmentJson.Array(r,"carriages");if(rows.Count<2||rows.Count>4)throw new FormatException();var slots=new string[4];var ids=new HashSet<string>();
            foreach(var token in rows){var c=(JObject)token;if(c.Properties().Count()!=2)throw new FormatException();var slot=AccountStateSnapshot.Number(c,"slot");var id=Id(c,"carriageId");if(slot>=rows.Count||slots[(int)slot]!=null||!ids.Add(id))throw new FormatException();slots[(int)slot]=id;}
            if(slots.Take(rows.Count).Any(s=>s==null))throw new FormatException();return slots;
        }
        public static JObject Validate(string target,string payload)
        {
            if(payload==null||payload.Length>5000)throw new FormatException();var r=GameApiJson.ReadObject(payload);if(r.Properties().Count()!=4||Id(r,"trainId")!=target||AccountStateSnapshot.Number(r,"expectedRevision")>=9007199254740991L)throw new FormatException();
            var selection=(JObject)r.DeepClone();selection.Remove("expectedRevision");ValidateSelection(selection);return r;
        }
        public static JObject Selection(string train,IReadOnlyList<string> slots,int count)
        {var rows=new JArray();for(int i=0;i<count;i++)rows.Add(new JObject{["slot"]=i,["carriageId"]=slots[i]});return new JObject{["rulesVersion"]=Rules,["trainId"]=train,["carriages"]=rows};}
        public static async Task<TrainLoadoutWriteResult> SendAsync(IRunApiHttpTransport wire,GameApiAccessToken token,PendingAccountAction action)
        {
            try{
                var request=Validate(action.Target,action.Payload);var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/trains/loadout",action.Payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),token));
                if(response.IsNetworkError||response.StatusCode>=500)return new TrainLoadoutWriteResult(null,"unknown",false);var r=GameApiJson.ReadObject(response.BodyJson);
                if(response.StatusCode==200&&GameApiJson.Boolean(r,"accepted")){
                    var selection=GameApiJson.Object(r,"loadout");var selected=ValidateSelection(selection);var expected=(JObject)request.DeepClone();expected.Remove("expectedRevision");if(Id(selection,"trainId")!=action.Target||!selected.SequenceEqual(ValidateSelection(expected)))throw new FormatException();
                    var changed=GameApiJson.Boolean(r,"changed");var revision=AccountStateSnapshot.Number(r,"revision");if(revision!=AccountStateSnapshot.Number(request,"expectedRevision")+(changed?1:0)||!GameApiJson.Boolean(r,"refreshRequired"))throw new FormatException();
                    return new TrainLoadoutWriteResult(new TrainLoadoutReceipt(GameApiJson.String(r,"receiptId"),revision,changed),null,true);
                }
                return new TrainLoadoutWriteResult(null,GameApiJson.String(r,"errorCode"),!GameApiJson.Boolean(r,"accepted")&&(response.StatusCode==400||response.StatusCode==403||response.StatusCode==409));
            }catch{return new TrainLoadoutWriteResult(null,"invalid_response",false);}
        }
    }
}
