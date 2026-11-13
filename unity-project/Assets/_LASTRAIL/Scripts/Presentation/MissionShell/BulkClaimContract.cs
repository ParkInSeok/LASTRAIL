using System;
using System.Linq;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class BulkClaimResult {
  readonly JObject data;
  public string Id=>(string)data["bulkId"];
  public bool Complete=>(string)data["status"]=="COMPLETE";
  public int Success=>(int)data["successCount"];
  public int Failed=>(int)data["failureCount"];
  public int Pending=>(int)data["pendingCount"];
  public int Total=>(int)data["totalCount"];
  public string RequestJson=>data["request"].ToString();
  public JArray Items=>(JArray)data["items"].DeepClone();
  public BulkClaimResult(string json,string original,string expectedId=null){var request=BulkClaimContract.Request(original);data=GameApiJson.ReadObject(json);RecurringContract.Accept(data);if(GameApiJson.String(data,"version")!="bulk-claim-v1"||!JToken.DeepEquals(GameApiJson.Object(data,"request"),request))throw new FormatException();MonthlyAttendanceContract.Id(data,"bulkId");if(expectedId!=null&&Id!=expectedId)throw new FormatException();AccountStateSnapshot.Number(data,"createdAt");var actions=BulkClaimContract.Actions(request);var items=RecruitmentJson.Array(data,"items");if(items.Count!=actions.Count||MonthlyAttendanceContract.Range(data,"totalCount",1,50)!=actions.Count)throw new FormatException();int success=0,failed=0,pending=0;
   for(int i=0;i<items.Count;i++){var item=(JObject)items[i];if(AccountStateSnapshot.Number(item,"index")!=i||!JToken.DeepEquals(item["action"],actions[i]))throw new FormatException();var status=RecurringContract.One(item,"status","SUCCESS","REPLAYED","FAILED","RETRY_PENDING");var error=RecruitmentJson.NullableString(item,"errorCode");if(status=="SUCCESS"||status=="REPLAYED"){success++;if(error!=null)throw new FormatException();var action=(JObject)actions[i];var kind=(string)action["kind"];var pass=kind=="PASS_LEVEL";var body=pass?new JObject{["level"]=(int)action["level"],["track"]=(string)action["track"]}.ToString():null;RecurringContract.Grant(GameApiJson.Object(item,"receipt"),new PendingAccountAction("bulk_validation",pass?"recurring-pass":kind=="MAIL"?"recurring-mail":"recurring-quest",(string)action[pass?"policyId":"awardId"],0,"validate",body));}else{if(item["receipt"]?.Type!=JTokenType.Null)throw new FormatException();if(status=="FAILED"){failed++;if(string.IsNullOrEmpty(error))throw new FormatException();}else pending++;}}
   if(MonthlyAttendanceContract.Range(data,"successCount",0,50)!=success||MonthlyAttendanceContract.Range(data,"failureCount",0,50)!=failed||MonthlyAttendanceContract.Range(data,"pendingCount",0,50)!=pending||GameApiJson.String(data,"status")!=(pending==0?"COMPLETE":"PARTIAL")||GameApiJson.Boolean(data,"refreshRequired")!=(success>0))throw new FormatException();
  }
 }
 public static class BulkClaimContract {
  public static JObject Request(string json){if(json==null||json.Length>10000)throw new FormatException();var body=GameApiJson.ReadObject(json);var kind=RecurringContract.One(body,"kind","QUEST","MAIL","PASS_LEVEL");var pass=kind=="PASS_LEVEL";if(body.Count!=(pass?4:2))throw new FormatException();if(pass){MonthlyAttendanceContract.Id(body,"policyId");RecurringContract.One(body,"track","FREE","PREMIUM");}var list=RecruitmentJson.Array(body,pass?"levels":"awardIds");if(list.Count<1||list.Count>50||list.Select(x=>x.ToString()).Distinct().Count()!=list.Count)throw new FormatException();foreach(var value in list){if(pass){MonthlyAttendanceContract.Range(new JObject{["level"]=value},"level",1,50);}else{if(value.Type!=JTokenType.String)throw new FormatException();MonthlyAttendanceContract.Id(new JObject{["id"]=value},"id");}}return body;}
  internal static JArray Actions(JObject request){var kind=(string)request["kind"];return kind=="PASS_LEVEL"?new JArray(request["levels"].Select(level=>new JObject{["kind"]=kind,["policyId"]=(string)request["policyId"],["track"]=(string)request["track"],["level"]=(int)level})):new JArray(request["awardIds"].Select(id=>new JObject{["kind"]=kind,["awardId"]=(string)id}));}
 }
 public sealed class BulkClaimSelection {
  internal readonly object Owner;internal readonly long Generation;internal readonly string Kind,Policy,Track;readonly JArray rows;
  internal BulkClaimSelection(object owner,long generation,string kind,string policy,string track,JArray candidates){Owner=owner;Generation=generation;Kind=kind;Policy=policy;Track=track;rows=(JArray)candidates.DeepClone();}
  public JArray Rows=>(JArray)rows.DeepClone();
 }
 public sealed class BulkClaimCommand {
  internal readonly object Owner;internal readonly long Generation;internal readonly string Payload;
  internal BulkClaimCommand(object owner,long generation,string payload){Owner=owner;Generation=generation;Payload=payload;}
 }
}
