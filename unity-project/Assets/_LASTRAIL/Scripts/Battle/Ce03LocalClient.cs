using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
namespace LastRail.Battle {
// One explicitly composed disposable-host sender. Persist callback must durably
// store owner-bound journal bytes before sending and before acknowledging receipt.
public sealed class Ce03LocalClient {
 readonly IRunApiHttpTransport wire;readonly GameApiAccessToken token;readonly string owner,origin;readonly Action<string> persist;
 JObject journal,receivedJournal,unstoredStart;bool busy;
 bool legacyStartRequiresOwner;
 public bool RequiresCheckpointReconciliation=>legacyStartRequiresOwner;
 public event Action StartProgressChanged;
 public bool IsConfirmingStart {get;private set;}
 public bool HasReceivedStart=>receivedJournal!=null;
 public long? KnownSupportRevision {get;private set;}
 void RememberSupportRevision(long? revision){if(revision.HasValue&&(!KnownSupportRevision.HasValue||revision.Value>KnownSupportRevision.Value))KnownSupportRevision=revision;}
 public bool HasUnstoredStart=>unstoredStart!=null;
 public string PendingKind=>unstoredStart!=null?"start":(string)(journal?["pending"] as JObject)?["kind"];
 public bool HasPending=>unstoredStart!=null||journal?["pending"] is JObject;
 public string JournalJson=>Ce03CommandContract.Canonical(journal);
 public string AwaitingCheckpointKey=>(string)journal["awaitingCheckpoint"];
 // The checkpoint owner validates the complete original identity and reads durable
 // state before this sender clears only its handoff marker. Receipts stay intact.
 public void CompleteCheckpointHandoff(Ce03OriginalStart original,CombatCheckpointStore checkpoints){
  if(busy||HasPending||original==null||checkpoints==null)throw new InvalidOperationException("Checkpoint handoff unavailable.");
  if(original.Json!=ReadRetainedStart(original.Key).Json)throw new InvalidOperationException("Checkpoint original differs.");
  if(AwaitingCheckpointKey!=null&&AwaitingCheckpointKey!=original.Key)throw new InvalidOperationException("Another START awaits its checkpoint.");
  checkpoints.RequireDurableLocalStart(original);
  if(AwaitingCheckpointKey==null&&!legacyStartRequiresOwner)return;
  busy=true;try{var next=(JObject)journal.DeepClone();next["awaitingCheckpoint"]=null;Save(next);legacyStartRequiresOwner=false;}finally{busy=false;}
 }
 public Ce03OriginalStart ReadRetainedStart(string key){if(busy||HasPending)throw new InvalidOperationException("Resolve original pending request first.");var receipt=ReadReceipt(journal,key);return Ce03OriginalStart.FromReceipt(owner,new Uri(origin+"/"),receipt);}
 static JObject ReadReceipt(JObject source,string key){
  var token=key==null?null:source["receipts"]?[key];
  return token is JRaw raw?ReadObject((string)raw.Value):token as JObject??throw new InvalidOperationException("Original receipt missing.");
 }
 // Immutable canonical receipt bytes remain available for recovery. Old response
 // trees are not live owner state and must not be deep-cloned on every command.
 static void CompactReceipts(JObject source){foreach(var receipt in ((JObject)source["receipts"]).Properties())if(receipt.Value is not JRaw)receipt.Value=new JRaw(Ce03CommandContract.Canonical(receipt.Value));}
 const int MaximumJournalBytes=24*1024*1024;
 public Ce03LocalClient(Uri localOrigin,string owner,IRunApiHttpTransport wire,GameApiAccessToken token,Action<string> persist,string saved=null){
  if(localOrigin==null||localOrigin.Scheme!="http"||localOrigin.Host!="127.0.0.1"||localOrigin.AbsolutePath!="/"||localOrigin.Query!=""||localOrigin.UserInfo!=""||localOrigin.Fragment!=""||string.IsNullOrWhiteSpace(owner))throw new ArgumentException("Explicit loopback owner required.");
  this.owner=owner;origin=localOrigin.GetLeftPart(UriPartial.Authority);this.wire=wire??throw new ArgumentNullException(nameof(wire));this.token=token??throw new ArgumentNullException(nameof(token));this.persist=persist??throw new ArgumentNullException(nameof(persist));
  if(saved!=null&&System.Text.Encoding.UTF8.GetByteCount(saved)>MaximumJournalBytes)throw new FormatException("Local journal exceeds capacity.");
  journal=saved==null?new JObject{["schema"]="ce03-local-client-journal-v1",["owner"]=owner,["origin"]=origin,["pending"]=null,["receipts"]=new JObject()}:ReadObject(saved);
  Ce03CommandContract.Fields(journal,journal.Property("awaitingCheckpoint")==null?new[]{"schema","owner","origin","pending","receipts"}:new[]{"schema","owner","origin","pending","receipts","awaitingCheckpoint"});Ce03CommandContract.Equal(journal,"schema","ce03-local-client-journal-v1");Ce03CommandContract.Equal(journal,"owner",owner);Ce03CommandContract.Equal(journal,"origin",origin);
  if(journal["awaitingCheckpoint"]!=null&&journal["awaitingCheckpoint"].Type!=JTokenType.Null){
   if(journal["awaitingCheckpoint"].Type!=JTokenType.String||HasPending)throw new FormatException("Invalid checkpoint handoff.");
   ReadRetainedStart(AwaitingCheckpointKey);
  }
  if(HasPending)ValidateEnvelope((JObject)journal["pending"]);
  foreach(var receipt in ((JObject)journal["receipts"]).Properties())if((string)receipt.Value["request"]?["kind"]=="support"){long? revision=(long?)receipt.Value["response"]?["revision"];if(revision.HasValue&&(!KnownSupportRevision.HasValue||revision>KnownSupportRevision))KnownSupportRevision=revision;}
  legacyStartRequiresOwner=journal.Property("awaitingCheckpoint")==null&&((JObject)journal["receipts"]).Properties().Any(r=>(string)r.Value["request"]?["kind"]=="start");
  CompactReceipts(journal);
 }
 void Save(JObject next){string raw=Ce03CommandContract.Canonical(next);if(System.Text.Encoding.UTF8.GetByteCount(raw)>MaximumJournalBytes)throw new InvalidOperationException("Local journal exceeds capacity; preserve original.");persist(raw);journal=next;}
 Task<RunApiHttpResponse> Send(string method,string path,JObject body=null,string key=null)=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest(method,path,body==null?null:Ce03CommandContract.Canonical(body),key==null?null:new[]{new RunApiHttpHeader("Idempotency-Key",key)}),token));
 static JObject ReadObject(string raw){using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None})return JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
 static JObject Response(RunApiHttpResponse r){if(r.IsNetworkError||r.StatusCode<200||r.StatusCode>=300)throw new InvalidOperationException("Local request unresolved: "+r.StatusCode);return ReadObject(r.BodyJson);}
 public async Task<JObject> ReadSupportAsync(){var result=Response(await Send("GET","/__local/ce03/support-loadout"));RememberSupportRevision((long?)result["selection"]?["revision"]);return result;}
 public async Task<Ce03RailPreparation> PrepareRailAsync(JObject selection,long supportRevision){
  if(selection==null||HasPending||busy||supportRevision<1)throw new InvalidOperationException("Resolve pending request first.");
  busy=true;try{return new Ce03RailPreparation(Response(await Send("POST","/__local/ce03/rail-prepare",new JObject{["selection"]=selection.DeepClone(),["expectedSupportRevision"]=supportRevision})),owner,supportRevision);}finally{busy=false;}
 }
 public async Task<JObject> PrepareAsync(JObject selection,long supportRevision){if(HasPending||busy||supportRevision<1)throw new InvalidOperationException("Resolve pending request first.");var result=Response(await Send("POST","/__local/ce03/prepare",new JObject{["selection"]=selection.DeepClone(),["expectedSupportRevision"]=supportRevision}));var c=new Ce03CommandContract(result["commandContract"] as JObject);c.ValidatePrepared(result,owner,supportRevision);return result;}
 public Task<JObject> SaveSupportAsync(long expectedRevision,string[] skillIds){
  if(expectedRevision<0||expectedRevision>=9007199254740991L||skillIds==null||skillIds.Length!=3||skillIds.Any(string.IsNullOrWhiteSpace)||skillIds.Distinct().Count()!=3)throw new ArgumentException();
  return Begin("support",new JObject{["rulesVersion"]="ce03-support-selection-v1",["catalogVersion"]="ce03-local-support-roles-v1",["expectedRevision"]=expectedRevision,["slots"]=new JArray(skillIds.Select((id,i)=>new JObject{["slot"]=i,["skillId"]=id}))});
 }
 public Task<JObject> QuoteAsync(JObject departure)=>Begin("quote",(JObject)departure.DeepClone());
 // Does not execute returned combat. Strict admitted-envelope validation is a
 // separate boundary; a synthetic or unverified response cannot bind the view here.
 public Task<JObject> StartAsync(string quoteId,long supportRevision,string prepareHash){if(string.IsNullOrWhiteSpace(quoteId)||supportRevision<1||prepareHash==null||prepareHash.Length!=64||prepareHash.Any(c=>!"0123456789abcdef".Contains(c)))throw new ArgumentException();return Begin("start",new JObject{["operationPreviewId"]=quoteId,["expectedSupportRevision"]=supportRevision,["prepareSha256"]=prepareHash});}
 public Task<JObject> StartPreparedAsync(string quoteId,JObject prepared){
  if(string.IsNullOrWhiteSpace(quoteId))throw new ArgumentException();var c=new Ce03CommandContract(prepared?["commandContract"] as JObject);c.ValidatePrepared(prepared,owner,c.Revision);if(prepared.Property("presentationManifest")!=null)new Ce03PresentationManifest(prepared["presentationManifest"] as JObject);
  return Begin("start",new JObject{["operationPreviewId"]=quoteId,["expectedSupportRevision"]=c.Revision,["prepareSha256"]=prepared["prepareSha256"].DeepClone()},prepared);
 }
 async Task<JObject> Begin(string kind,JObject body,JObject prepared=null){
  if(HasPending||busy||AwaitingCheckpointKey!=null||legacyStartRequiresOwner)throw new InvalidOperationException("Original request and checkpoint handoff must be resolved.");
  var pending=new JObject{["kind"]=kind,["key"]=Guid.NewGuid().ToString("N"),["body"]=body};if(kind=="start")pending["automaticRetryUsed"]=false;if(prepared!=null)pending["prepared"]=prepared.DeepClone();ValidateEnvelope(pending);
  var next=(JObject)journal.DeepClone();next["pending"]=pending;
  if(kind=="start")unstoredStart=pending;
  busy=true;try{Save(next);unstoredStart=null;}finally{busy=false;}
  return await RetryAsync();
 }
 // Only an initial persistence failure proves this owner has never dispatched.
 // Persist cancellation before forgetting it: the failed writer may have written
 // bytes and then failed readback. No input can race this synchronous transition.
 public bool CancelUnsentStart(){
  if(busy||unstoredStart==null)return false;
  busy=true;try{Save((JObject)journal.DeepClone());unstoredStart=null;return true;}finally{busy=false;}
 }
 static string Path(string kind)=>kind=="support"?"/__local/ce03/support-loadout":kind=="quote"?"/v1/operations/preview":kind=="start"?"/__local/ce03/start":throw new FormatException("Unknown local request.");
 void ValidateEnvelope(JObject p){
  var fields=new System.Collections.Generic.List<string>{"kind","key","body"};if(p?.Property("prepared")!=null)fields.Add("prepared");if(p?.Property("resolvedStart")!=null)fields.Add("resolvedStart");if(p?.Property("automaticRetryUsed")!=null)fields.Add("automaticRetryUsed");Ce03CommandContract.Fields(p,fields.ToArray());Path((string)p["kind"]);if(p["body"] is not JObject||p["key"]?.Type!=JTokenType.String||!RunRequestKey.IsSupported((string)p["key"]))throw new FormatException("Invalid original request.");
  if(p.Property("automaticRetryUsed")!=null&&((string)p["kind"]!="start"||p["automaticRetryUsed"].Type!=JTokenType.Boolean))throw new FormatException("Invalid retry budget.");
  if(p.Property("prepared")!=null){if((string)p["kind"]!="start")throw new FormatException();var data=p["prepared"] as JObject;var c=new Ce03CommandContract(data?["commandContract"] as JObject);c.ValidatePrepared(data,owner,c.Revision);Ce03CommandContract.Fields(p["body"] as JObject,"operationPreviewId","expectedSupportRevision","prepareSha256");if((long?)p["body"]["expectedSupportRevision"]!=c.Revision||!JToken.DeepEquals(p["body"]["prepareSha256"],data["prepareSha256"]))throw new FormatException("Original prepare changed.");}
  if(p.Property("resolvedStart")!=null){if((string)p["kind"]!="start"||p["prepared"] is not JObject)throw new FormatException();new Ce03AcceptedCombat(p["resolvedStart"] as JObject,(JObject)p["prepared"],owner,(string)p["body"]["operationPreviewId"]);}
 }
 public async Task<JObject> RetryAsync(){
  if(!HasPending||busy)throw new InvalidOperationException();busy=true;
  try{
   if(unstoredStart!=null){var stored=(JObject)journal.DeepClone();stored["pending"]=unstoredStart.DeepClone();Save(stored);unstoredStart=null;}
   var p=(JObject)journal["pending"];
   if(receivedJournal!=null){var receipt=ReadReceipt(receivedJournal,(string)p["key"]);Save(receivedJournal);receivedJournal=null;return (JObject)receipt["response"].DeepClone();}
   var http=await Send("POST",Path((string)p["kind"]),(JObject)p["body"],(string)p["key"]);
   // One bounded network retry of the durably retained START. HTTP rejection and
   // receipt-storage failure are separate boundaries and must not trigger it.
   if((string)p["kind"]=="start"&&http.IsNetworkError){
    IsConfirmingStart=true;StartProgressChanged?.Invoke();
    // Spend before sending, so a crash or view round-trip cannot replenish it.
    // Legacy pending journals have no proven unused budget and are conservative.
    if((bool?)p["automaticRetryUsed"]==false){var spent=(JObject)journal.DeepClone();spent["pending"]["automaticRetryUsed"]=true;Save(spent);p=(JObject)journal["pending"];http=await Send("POST",Path("start"),(JObject)p["body"],(string)p["key"]);}
   }
   var response=Response(http);
   string kind=(string)p["kind"];
   if(kind=="support"){
    Ce03CommandContract.Fields(response,"rulesVersion","catalogVersion","revision","slots");
    foreach(string field in new[]{"rulesVersion","catalogVersion","slots"})if(!JToken.DeepEquals(response[field],p["body"][field]))throw new FormatException("Saved selection mismatch.");
    if(response["revision"]?.Type!=JTokenType.Integer||(long)response["revision"]!=(long)p["body"]["expectedRevision"]+1)throw new FormatException("Saved revision mismatch.");
   }else if(kind=="quote"){if((bool?)response["accepted"]!=true||string.IsNullOrWhiteSpace((string)response["operationPreviewId"]))throw new FormatException();}
   else {if((bool?)response["accepted"]!=true||(string)response["scope"]!="DISPOSABLE_LOCAL"||(string)response["startStatus"]!="STARTED"||response["combat"] is not JObject)throw new FormatException();if(p["prepared"] is JObject original)new Ce03AcceptedCombat(response,original,owner,(string)p["body"]["operationPreviewId"]);if(p["resolvedStart"]!=null&&!JToken.DeepEquals(p["resolvedStart"],response))throw new FormatException("Original resolved START changed.");}
   var next=(JObject)journal.DeepClone();next["receipts"][(string)p["key"]]=new JObject{["request"]=p.DeepClone(),["response"]=response.DeepClone(),["responseRaw"]=http.BodyJson};next["pending"]=null;
   if(kind=="start"&&p["prepared"] is JObject)next["awaitingCheckpoint"]=(string)p["key"];
   CompactReceipts(next);
   if(kind=="start")receivedJournal=next;Save(next);receivedJournal=null;if(kind=="support")RememberSupportRevision((long)response["revision"]);return response;
  }finally{busy=false;IsConfirmingStart=false;}
 }
 public async Task<JObject> LookupStartAsync(){
  if(busy||unstoredStart!=null||!HasPending||(string)journal["pending"]["kind"]!="start")throw new InvalidOperationException();busy=true;
  try{var p=journal["pending"];var body=(JObject)p["body"].DeepClone();body["startIdempotencyKey"]=(string)p["key"];var result=Response(await Send("POST","/__local/ce03/start-result",body));
   Ce03CommandContract.Fields(result,"lookupStatus","startResponse","newAttemptAuthorized");if(result["newAttemptAuthorized"]?.Type!=JTokenType.Boolean||(bool)result["newAttemptAuthorized"])throw new FormatException("Lookup cannot authorize new START.");
   if((string)result["lookupStatus"]=="NOT_FOUND"){if(result["startResponse"]?.Type!=JTokenType.Null||p["resolvedStart"]!=null)throw new FormatException("Original START lookup changed.");}
   else if((string)result["lookupStatus"]=="COMMITTED"){
    if(p["prepared"] is not JObject original)throw new InvalidOperationException("Full original prepare required.");new Ce03AcceptedCombat(result["startResponse"] as JObject,original,owner,(string)p["body"]["operationPreviewId"]);
    if(p["resolvedStart"]!=null&&!JToken.DeepEquals(p["resolvedStart"],result["startResponse"]))throw new FormatException("Resolved START changed.");var next=(JObject)journal.DeepClone();next["pending"]["resolvedStart"]=result["startResponse"].DeepClone();Save(next);
   }else throw new FormatException("Unknown lookup status.");return result;
  }finally{busy=false;}
 }
}
}
