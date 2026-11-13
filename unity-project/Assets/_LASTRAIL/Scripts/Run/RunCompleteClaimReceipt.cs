using System;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Immutable validated original response. Publishing owner state is a separate,
 // atomic consumer responsibility; this parser never credits balances or unlocks.
 public sealed class RunCompleteClaimReceipt {
  public const string Version="run.reward-claim.v2.20261019";
  public const int MaximumUtf8Bytes=512*1024;
  // Json.NET quotes literal U+0085 as six characters for two UTF-8 bytes.
  // Three characters per byte plus quotes also covers the other JSON escapes.
  internal const int MaximumEncodedCharacters=3*MaximumUtf8Bytes+2;
  internal static JObject ReadJson(string json){
   if(string.IsNullOrWhiteSpace(json)||json.Length>MaximumUtf8Bytes)throw new FormatException("Complete claim capacity");
   try{if(new UTF8Encoding(false,true).GetByteCount(json)>MaximumUtf8Bytes)throw new FormatException("Complete claim capacity");}
   catch(EncoderFallbackException ex){throw new FormatException("Invalid claim Unicode",ex);}
   using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=32}){
    var root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});
    if(reader.Read()||root.Descendants().Any(t=>t.Type==JTokenType.Comment))throw new FormatException("Invalid complete claim JSON");
    return root;
   }
  }
  readonly JObject original;
  RunCompleteClaimReceipt(JObject value){original=(JObject)value.DeepClone();}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static RunCompleteClaimReceipt Read(string json,string account,string run,string preview,string command){
   var p=ReadJson(json);
   Keys(p,"version accountId runId rewardPreviewId rewardClaimId commandId status serverClaimedAt balanceVersion rewardLines transactionReceipt nextAction owners errorCode message");
   if(Id(p,"version")!=Version||Id(p,"accountId")!=account||Id(p,"runId")!=run||Id(p,"rewardPreviewId")!=preview||Id(p,"commandId")!=command||Id(p,"status")!="CLAIMED"||Id(p,"nextAction")!="RETURN_TO_LOBBY")throw new FormatException("Complete claim identity/version");
   Id(p,"rewardClaimId");Id(p,"balanceVersion");Time(p,"serverClaimedAt");Null(p,"errorCode");Null(p,"message");
   var lines=Array(p,"rewardLines");foreach(var value in lines)Line(Object(value));
   var receipt=Object(p["transactionReceipt"]);Keys(receipt,"rewardTransactionId runId serverClaimedAt rewardPlan");
   foreach(var pair in new[]{("rewardTransactionId","rewardClaimId"),("runId","runId"),("serverClaimedAt","serverClaimedAt")})if(!JToken.DeepEquals(receipt[pair.Item1],p[pair.Item2]))throw new FormatException("Original transaction mismatch");
   var planned=Array(Object(receipt["rewardPlan"]),"rewardLines");if(planned.Count!=lines.Count)throw new FormatException("Reward plan count");
   foreach(var row in planned)Line(Object(row));
   string Signature(JToken row)=>new JArray(row["id"],row["kind"],row["source"],row["quantity"]).ToString(Newtonsoft.Json.Formatting.None);
   if(!planned.Select(Signature).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(lines.Select(Signature).OrderBy(x=>x,StringComparer.Ordinal)))throw new FormatException("Reward plan mismatch");
   var owners=Object(p["owners"]);Keys(owners,"account wallet progress");var a=Object(owners["account"]);
   Keys(a,"kind coverage fuel beforeMutationSequence mutationSequence currencies items");Snapshot(a);Coverage(a,"CURRENCIES","ITEMS","FUEL");
   long before=Nat(a,"beforeMutationSequence"),sequence=Nat(a,"mutationSequence");if(sequence!=before+1)throw new FormatException("Account sequence");
   Rows(a,"currencies","currency","balance");Rows(a,"items","itemId","quantity");
   if(a["fuel"].Type!=JTokenType.Null){var fuel=Object(a["fuel"]);Keys(fuel,"current max lastRegenAt updatedAt");Nat(fuel,"current");Nat(fuel,"max");Time(fuel,"lastRegenAt");Time(fuel,"updatedAt");}
   var w=Object(owners["wallet"]);if(Id(w,"kind")=="UNCHANGED")Keys(w,"kind");else{
    Snapshot(w);Keys(w,"kind coverage currencyId policyVersion freeBalance paidBalance totalBalance sourceMutation");Coverage(w,"BALANCES");Id(w,"currencyId");Id(w,"policyVersion");
    if(checked(Nat(w,"freeBalance")+Nat(w,"paidBalance"))!=Nat(w,"totalBalance"))throw new FormatException("Wallet sum");
    var m=Object(w["sourceMutation"]);Keys(m,"versionSource operationId basisAccountSequence mutationSequence");if(Id(m,"versionSource")!="CRYSTAL_OPERATION_MUTATION"||Nat(m,"basisAccountSequence")!=before||Nat(m,"mutationSequence")!=sequence)throw new FormatException("Wallet original mutation");Id(m,"operationId");
   }
   ValidateProgress(Object(owners["progress"]),sequence);
   return new RunCompleteClaimReceipt(p);
  }
  internal static void ValidateProgress(JObject g,long? accountSequence=null){
   Keys(g,"kind beforeMutationSequence mutationSequence versionSource contentHash state");Snapshot(g);
   long progress=Nat(g,"mutationSequence");if(Nat(g,"beforeMutationSequence")!=progress||(accountSequence.HasValue&&progress>=accountSequence.Value))throw new FormatException("Independent progress sequence");
   var state=Object(g["state"]);Keys(state,"story unlocks");var unlocks=Array(state,"unlocks");
   if(Id(g,"contentHash")!=SafeTransitionWire.Digest(state))throw new FormatException("Progress content hash");
   string source=Id(g,"versionSource");if(source=="VERIFIED_EMPTY"){if(progress!=0||state["story"].Type!=JTokenType.Null||unlocks.Count!=0)throw new FormatException("False empty progress");}
   else if(source!="STORY_PROGRESS_MUTATION"||progress==0||state["story"] is not JObject)throw new FormatException("Progress original source");
   if(unlocks.Select(x=>Id(Object(x),"unlockId")).Distinct().Count()!=unlocks.Count)throw new FormatException("Duplicate unlock");
   // This owner snapshot preserves the source SQL millisecond value, unlike the
   // account display endpoint's ISO timestamp projection.
   foreach(var row in unlocks){Keys(Object(row),"unlockId unlockedAt");Nat(Object(row),"unlockedAt");}
  }
  static JObject Object(JToken value)=>value as JObject??throw new FormatException("Expected object");
  static JArray Array(JObject value,string key)=>value[key] as JArray??throw new FormatException("Expected array: "+key);
  static void Keys(JObject p,string keys){var expected=keys.Split(' ');if(p.Count!=expected.Length||p.Properties().Any(x=>!expected.Contains(x.Name)))throw new FormatException("Complete claim fields");}
  static string Id(JObject p,string key){if(p[key]?.Type!=JTokenType.String)throw new FormatException(key);var s=(string)p[key];if(string.IsNullOrWhiteSpace(s)||s.Length>200)throw new FormatException(key);return s;}
  static long Nat(JObject p,string key){if(p[key]?.Type!=JTokenType.Integer)throw new FormatException(key);long n=(long)p[key];if(n<0||n>9007199254740991L)throw new FormatException(key);return n;}
  static void Time(JObject p,string key){string s=Id(p,key);if(!s.EndsWith("Z",StringComparison.Ordinal)||!DateTimeOffset.TryParse(s,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal,out _))throw new FormatException("UTC time");}
  static void Null(JObject p,string key){if(p[key]?.Type!=JTokenType.Null)throw new FormatException(key);}
  static void Snapshot(JObject p){if(Id(p,"kind")!="SNAPSHOT")throw new FormatException("Snapshot kind");}
  static void Coverage(JObject p,params string[] names){if(!JToken.DeepEquals(p["coverage"],new JArray(names)))throw new FormatException("Owner coverage");}
  static void Rows(JObject p,string key,string id,string quantity){var rows=Array(p,key);foreach(var r in rows){var row=Object(r);Keys(row,id+" "+quantity);Id(row,id);Nat(row,quantity);}if(rows.Select(r=>(string)r[id]).Distinct().Count()!=rows.Count)throw new FormatException("Duplicate owner row");}
  static void Line(JObject row){Id(row,"id");Id(row,"kind");Id(row,"source");Nat(row,"quantity");if(row["balanceAfter"]!=null)Nat(row,"balanceAfter");}
 }
}
