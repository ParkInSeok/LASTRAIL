using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Credential-free immutable request. The private transport port alone supplies Authorization.
 public sealed class WorldBossAuthorityRequest {
  public const string Version="world-boss.authority-local-http.v1.20261002",JournalVersion="world-boss.rail-journal.v1.20261002",HealthVersion="world-boss.rail-health.v1.20261002";
  readonly JObject body;readonly Dictionary<string,string> headers;
  public string Method {get;}public string Path {get;}
  public IReadOnlyDictionary<string,string> Headers=>new ReadOnlyDictionary<string,string>(headers);
  public JObject Body=>body==null?null:(JObject)body.DeepClone();
  public long OriginalJournalRevision=>Method=="POST"?long.Parse(headers["X-World-Journal-Revision"],CultureInfo.InvariantCulture):0;
  public WorldBossAuthorityRequest(string attempt,string content,JObject originalBody=null,long journalRevision=0){
   if(string.IsNullOrEmpty(attempt)||attempt.Length>200||string.IsNullOrEmpty(content)||content.Length>200)throw new ArgumentException("Authority identity required");
   Method=originalBody==null?"GET":"POST";Path="/_local/world-boss/attempts/"+Uri.EscapeDataString(attempt)+"/authority"+(Method=="POST"?"/commands":"");
   headers=new Dictionary<string,string>(StringComparer.Ordinal){["X-World-Authority-Version"]=Version,["X-World-Rail-Version"]=WorldBossRailSnapshot.Version,["X-World-Journal-Version"]=JournalVersion,["X-World-Health-Version"]=HealthVersion,["X-World-Content-Version"]=content};
   if(originalBody==null){if(journalRevision!=0)throw new ArgumentException("GET has no journal command header");return;}
   WorldBossRailSnapshot.ValidateCommand(originalBody,attempt,content);string key=(string)originalBody["commandId"];ValidateKey(key);if(journalRevision<=0||journalRevision>9007199254740991)throw new ArgumentException("Original command headers invalid");
   body=(JObject)originalBody.DeepClone();headers["Content-Type"]="application/json";headers["Idempotency-Key"]=key;headers["X-World-Journal-Revision"]=journalRevision.ToString(CultureInfo.InvariantCulture);
  }
  internal static void ValidateKey(string key){if(key==null||!Regex.IsMatch(key,"^[\x21-\x7e]{1,128}$"))throw new ArgumentException("Original command key invalid");}
  public JObject Capture()=>new JObject{["method"]=Method,["path"]=Path,["headers"]=JObject.FromObject(headers),["body"]=body==null?JValue.CreateNull():(JToken)Body};
  internal static WorldBossAuthorityRequest Restore(JObject input,string attempt,string content){WorldBossRailSnapshot.Fields(input,"method","path","headers","body");long revision=0;if(input["body"].Type!=JTokenType.Null){var text=(string)input["headers"]?["X-World-Journal-Revision"];if(text==null||!Regex.IsMatch(text,"^[1-9][0-9]*$")||!long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out revision))throw new ArgumentException("Original revision header required");}var restored=new WorldBossAuthorityRequest(attempt,content,input["body"].Type==JTokenType.Null?null:input["body"] as JObject,revision);WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(input,restored.Capture()));return restored;}
 }
 public sealed class WorldBossAuthorityError {
  readonly JObject original;
  public int HttpStatus {get;}public string Code=>(string)original["errorCode"];
  public bool RequiresAuthentication=>HttpStatus==401;public bool Forbidden=>HttpStatus==403;
  public bool RequiresRead=>HttpStatus==409;public bool PreserveOriginalRetry=>HttpStatus==503;
  internal static void ValidateKey(string key){if(key==null||!Regex.IsMatch(key,"^[\x21-\x7e]{1,128}$"))throw new ArgumentException("Original command key invalid");}
  public JObject Capture()=>(JObject)original.DeepClone();
  public WorldBossAuthorityError(int status,JObject body){WorldBossRailSnapshot.Fields(body,"accepted","errorCode");WorldBossAuthoritySnapshot.Need(body["accepted"].Type==JTokenType.Boolean&&!(bool)body["accepted"]&&Array.IndexOf(new[]{400,401,403,404,409,503},status)>=0);WorldBossRailSnapshot.Text(body,"errorCode");HttpStatus=status;original=(JObject)body.DeepClone();}
 }
}
