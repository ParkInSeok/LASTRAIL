using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class FormationReceipt
    {
        public string Id {get;} public string PresetId {get;} public string Kind {get;} public long PresetRevision {get;}
        public bool HasCommittedState {get;}
        public bool InvalidatesSelection {get;}
        readonly JObject accountPatch;
        internal JObject AccountPatch=>accountPatch==null?null:(JObject)accountPatch.DeepClone();
        internal FormationReceipt(string id,string preset,string kind,long revision,bool complete=false,JObject patch=null,bool invalidatesSelection=false){Id=id;PresetId=preset;Kind=kind;PresetRevision=revision;HasCommittedState=complete;accountPatch=patch==null?null:(JObject)patch.DeepClone();InvalidatesSelection=invalidatesSelection;}
    }
    public sealed class FormationWriteResult
    {
        public FormationReceipt Receipt {get;} public string Error {get;} public bool Terminal {get;}
        internal FormationWriteResult(FormationReceipt receipt,string error,bool terminal){Receipt=receipt;Error=error;Terminal=terminal;}
    }
    public static class FormationWrite
    {
        public const string CompleteVersion="formation.command.v2.20261021";
        public static JObject Validate(string kind,string target,string payload)
        {
            if((kind!="formation-save"&&kind!="formation-activate")||target==null||!Regex.IsMatch(target,"^[A-Za-z0-9_-]{1,100}$")||payload==null||Encoding.UTF8.GetByteCount(payload)>18000)throw new FormatException();
            var r=GameApiJson.ReadObject(payload);var save=kind=="formation-save";
            var expected=save?new[]{"expectedRevision","name","operators"}:new[]{"expectedRevision","expectedFormationRevision"};
            if(r["responseVersion"]!=null){if(GameApiJson.String(r,"responseVersion")!=CompleteVersion)throw new FormatException();expected=expected.Concat(new[]{"responseVersion"}).ToArray();}
            if(r.Properties().Count()!=expected.Length||r.Properties().Any(p=>!expected.Contains(p.Name)))throw new FormatException();
            AccountStateSnapshot.Number(r,"expectedRevision");
            if(save){var name=GameApiJson.String(r,"name");if(name.Trim()!=name||name.Length>80)throw new FormatException();Selection(r["operators"]);}
            else AccountStateSnapshot.Number(r,"expectedFormationRevision");
            return r;
        }
        public static string[] Selection(JToken token)
        {
            if(!(token is JArray list)||list.Count!=5)throw new FormatException();var output=new string[5];
            foreach(var t in list){if(!(t is JObject r)||r.Properties().Count()!=2)throw new FormatException();var slot=AccountStateSnapshot.Number(r,"slot");if(slot>4||output[slot]!=null)throw new FormatException();output[slot]=GameApiJson.String(r,"operatorId");}
            if(output.Distinct().Count()!=5)throw new FormatException();return output;
        }
        public static JArray Operators(string[] selection)=>new JArray(selection.Select((id,slot)=>new JObject{["slot"]=slot,["operatorId"]=id}));
        public static async Task<FormationWriteResult> SendAsync(IRunApiHttpTransport wire,GameApiAccessToken token,PendingAccountAction action)
        {
            try{
                var submitted=Validate(action.Kind,action.Target,action.Payload);bool save=action.Kind=="formation-save";
                bool complete=(string)submitted["responseVersion"]==CompleteVersion;
                var request=new RunApiHttpRequest("POST","/v1/formation/presets/"+action.Target+(save?"/save":"/activate"),action.Payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)});
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(request,token));
                if(response.IsNetworkError||response.StatusCode>=500)return new FormationWriteResult(null,"unknown",false);
                var r=GameApiJson.ReadObject(response.BodyJson);
                if(response.StatusCode==200&&GameApiJson.Boolean(r,"accepted")){
                    var kind=GameApiJson.String(r,"kind");var preset=GameApiJson.String(r,"presetId");if(kind!=(save?"SAVE":"ACTIVATE")||preset!=action.Target||GameApiJson.Boolean(r,"refreshRequired")==complete)throw new FormatException();
                    if(complete&&GameApiJson.String(r,"contractVersion")!=CompleteVersion)throw new FormatException();
                    var revision=AccountStateSnapshot.Number(r,"presetRevision");if(revision==0)throw new FormatException();
                    if(complete&&revision!=AccountStateSnapshot.Number(submitted,"expectedRevision")+(save?1:0))throw new FormatException();
                    bool invalidates=false;
                    if(!save){var formationRevision=AccountStateSnapshot.Number(r,"formationRevision");GameApiJson.Boolean(r,"formationChanged");var selection=Selection(r["operators"]);
                        if(complete){var patch=GameApiJson.Object(GameApiJson.Object(r,"owners"),"account");var formation=GameApiJson.Object(patch,"formation");var expectedFormation=AccountStateSnapshot.Number(submitted,"expectedFormationRevision");
                            if(formationRevision<expectedFormation||formationRevision>expectedFormation+1||GameApiJson.String(formation,"activePresetId")!=preset||AccountStateSnapshot.Number(formation,"activePresetRevision")!=revision||AccountStateSnapshot.Number(formation,"revision")!=formationRevision||!Selection(formation["operators"]).SequenceEqual(selection))throw new FormatException();
                            invalidates=formationRevision!=expectedFormation;
                            if(!(r["invalidates"] is JArray list)||!list.Select(t=>(string)t).OrderBy(x=>x).SequenceEqual(invalidates?new[]{"DEPARTURE_PREVIEW","ULTIMATE_SELECTION"}:Array.Empty<string>()))throw new FormatException();
                        }
                    }
                    return new FormationWriteResult(new FormationReceipt(GameApiJson.String(r,"receiptId"),preset,kind,revision,complete,r["owners"]?["account"] as JObject,invalidates),null,true);
                }
                var error=GameApiJson.String(r,"errorCode");
                bool definitive=!GameApiJson.Boolean(r,"accepted")&&(response.StatusCode==400||response.StatusCode==409||response.StatusCode==403&&error=="formation_operator_not_owned");
                return new FormationWriteResult(null,error,definitive);
            }catch{return new FormationWriteResult(null,"invalid_response",false);}
        }
    }
}
