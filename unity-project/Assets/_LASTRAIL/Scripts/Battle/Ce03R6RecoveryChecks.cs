#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace LastRail.Battle {
// Synthetic Server-exported transport fixture ONLY. Never creates a simulation,
// resolves fixture assets, issues admission, or claims this is an actual run.
public static class Ce03R6RecoveryChecks {
 public static JObject ReadJson(string raw){using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None})return JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
 static string C(JToken j)=>Ce03CommandContract.Canonical(j);
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
 static void Rejected<T>(Action f) where T:Exception{try{f();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
 sealed class Wire:IRunApiHttpTransport {
  public string Raw;public bool Lost;public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);if(Lost)return Task.FromResult(RunApiHttpResponse.NetworkError("injected lost reply"));return Task.FromResult(RunApiHttpResponse.Success(200,r.EndpointPath.EndsWith("start-result",StringComparison.Ordinal)?C(new JObject{["lookupStatus"]="COMMITTED",["startResponse"]=ReadJson(Raw),["newAttemptAuthorized"]=false}):Raw));}
 }
 public static void Run(JObject fixture){
  Check((string)fixture["scope"]=="SYNTHETIC_VERIFIER_TRANSPORT_ONLY"&&(bool?)fixture["actualConsumerVerified"]==false&&(bool?)fixture["actualBattleExecuted"]==false,"Not a labeled synthetic transport fixture.");
  var p=(JObject)fixture["prepare"];var response=(JObject)fixture["startResponse"];string owner=(string)p["ownedSelection"]["accountId"],quote=(string)fixture["startRequest"]["operationPreviewId"],key=(string)fixture["startIdempotencyKey"];
  var accepted=new Ce03AcceptedCombat(response,p,owner,quote);
  Check((string)response["combat"]["contentManifest"]["version"]!=(string)response["combat"]["profile"]["baseProfile"]["profileId"],"Fixture must cover distinct r6 content version.");
  var bad=(JObject)response.DeepClone();bad["combat"]["contentManifest"]["version"]=bad["combat"]["profile"]["baseProfile"]["profileId"].DeepClone();RehashContent(bad);Rejected<FormatException>(()=>new Ce03AcceptedCombat(bad,p,owner,quote));
  bad=(JObject)response.DeepClone();bad["combat"]["snapshotSha256"]=new string('0',64);Rejected<FormatException>(()=>new Ce03AcceptedCombat(bad,p,owner,quote));
  var origin=new Uri("http://127.0.0.1:12345/");var token=new GameApiAccessToken("lst1.synthetic-transport-test");
  var pending=new JObject{["kind"]="start",["key"]=key,["body"]=fixture["startRequest"].DeepClone(),["prepared"]=p.DeepClone()};
  string saved=C(new JObject{["schema"]="ce03-local-client-journal-v1",["owner"]=owner,["origin"]=origin.GetLeftPart(UriPartial.Authority),["pending"]=pending,["receipts"]=new JObject()});
  var wire=new Wire{Raw=C(response),Lost=true};var client=new Ce03LocalClient(origin,owner,wire,token,s=>saved=s,saved);
  Rejected<InvalidOperationException>(()=>client.RetryAsync().GetAwaiter().GetResult());Check(client.HasPending,"Lost response cleared original.");
  Check(wire.Requests.Count==1,"Legacy journal must not invent an unused retry budget.");
  Rejected<FormatException>(()=>new Ce03LocalClient(origin,owner+".foreign",wire,token,_=>{},saved));Rejected<FormatException>(()=>new Ce03LocalClient(new Uri("http://127.0.0.1:12346/"),owner,wire,token,_=>{},saved));
  wire.Lost=false;var restored=new Ce03LocalClient(origin,owner,wire,token,s=>saved=s,saved);var lookup=restored.LookupStartAsync().GetAwaiter().GetResult();Check(restored.HasPending&&JToken.DeepEquals(lookup["startResponse"],response),"Lookup lost original response.");
  var lookupRequest=wire.Requests.Single(r=>r.EndpointPath.EndsWith("start-result",StringComparison.Ordinal));var lookupBody=JObject.Parse(lookupRequest.BodyJson);Check((string)lookupBody["startIdempotencyKey"]==key,"Lookup key changed.");lookupBody.Remove("startIdempotencyKey");Check(JToken.DeepEquals(lookupBody,fixture["startRequest"]),"Lookup body changed.");
  string resolved=saved;var resumed=new Ce03LocalClient(origin,owner,wire,token,s=>saved=s,saved);var replay=resumed.RetryAsync().GetAwaiter().GetResult();
  Check(!resumed.HasPending&&wire.Requests.Count==3&&wire.Requests[0].BodyJson==wire.Requests.Last().BodyJson&&wire.Requests[0].Headers.Select(x=>x.Value).SequenceEqual(wire.Requests.Last().Headers.Select(x=>x.Value)),"START replay key/body changed.");
  var reparsed=new Ce03AcceptedCombat(replay,p,owner,quote);Check(reparsed.Seed==accepted.Seed&&reparsed.SnapshotHash==accepted.SnapshotHash,"Original seed/snapshot changed.");
  var receipt=JObject.Parse(saved)["receipts"][key];Check((string)receipt["responseRaw"]==wire.Raw&&JToken.DeepEquals(receipt["request"]["prepared"],p),"Full prepare/raw response not retained.");
  var identity=resumed.ReadRetainedStart(key);var identityReload=new Ce03OriginalStart(identity.Json,owner,origin);Check(identityReload.Seed==accepted.Seed&&identityReload.SnapshotHash==accepted.SnapshotHash&&identityReload.Key==key,"Typed original START roundtrip changed.");Rejected<FormatException>(()=>new Ce03OriginalStart(identity.Json,owner+".foreign",origin));Rejected<FormatException>(()=>new Ce03OriginalStart(identity.Json,owner,new Uri("http://127.0.0.1:12346/")));Check((string)identity.Copy()["responseRaw"]==wire.Raw,"Original response bytes changed.");string immutable=identity.Json;var detached=identity.Copy();detached["key"]="changed-key-123456";Check(identity.Json==immutable,"Original identity mutated through copy.");
  bad=(JObject)response.DeepClone();bad["combat"]["seed"]["value"]=((long)bad["combat"]["seed"]["value"]+1)%4294967296L;bad["combat"]["snapshot"]["seed"]=bad["combat"]["seed"].DeepClone();bad["combat"]["snapshotSha256"]=Ce03CommandContract.Sha(C(bad["combat"]["snapshot"]));wire.Raw=C(bad);
  var divergent=new Ce03LocalClient(origin,owner,wire,token,_=>{},resolved);Rejected<FormatException>(()=>divergent.RetryAsync().GetAwaiter().GetResult());Check(divergent.HasPending,"Changed replay discarded original.");
  wire.Raw=C(response);int writes=0;var ackFailure=new Ce03LocalClient(origin,owner,wire,token,_=>{if(++writes==1)throw new IOException("injected ACK write failure");},resolved);Rejected<IOException>(()=>ackFailure.RetryAsync().GetAwaiter().GetResult());Check(ackFailure.HasPending,"ACK failure cleared original.");ackFailure.RetryAsync().GetAwaiter().GetResult();Check(!ackFailure.HasPending,"ACK retry did not recover.");
  var silent=new Wire{Raw=C(response)};var saveFailure=new Ce03LocalClient(origin,owner,silent,token,_=>throw new IOException("injected pre-send failure"));Rejected<IOException>(()=>saveFailure.StartPreparedAsync(quote,p).GetAwaiter().GetResult());Check(silent.Requests.Count==0,"START sent before prepare save.");
 }
 static void RehashContent(JObject r){var c=r["combat"];c["contentSha256"]=Ce03CommandContract.Sha(C(c["contentManifest"]));c["snapshot"]["contentSha256"]=c["contentSha256"].DeepClone();c["snapshotSha256"]=Ce03CommandContract.Sha(C(c["snapshot"]));}
}
}
#endif
