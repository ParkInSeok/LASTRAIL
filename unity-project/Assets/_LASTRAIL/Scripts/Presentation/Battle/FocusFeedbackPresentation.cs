using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle
{
 // Local display input contract, not a server schema or a native B producer.
 // The host supplies committed selection, basic-attack eligibility, target generation,
 // information permission and current camera projection. Never derive these from art.
 public sealed class FocusFeedbackPresentation : IDisposable
 {
  Action<JObject> render;
  Func<string,long,JObject> project;
  Action<string> select;
  JObject source;
  long binding;

  // Each binding gets its own observer. A queued observer from a previous scene or
  // account cannot write into a later binding, even when revisions restart at zero.
  public Action<JObject> Bind(Action<JObject> render,Func<string,long,JObject> project,Action<string> select)
  {
   if(render==null||project==null||select==null)throw new ArgumentNullException();
   Unbind();this.render=render;this.project=project;this.select=select;
   long ticket=binding;
   return value=>{if(ticket==binding&&this.render!=null)Observe(value);};
  }
  void Observe(JObject value)
  {
   var next=Validate(value);
   if(source!=null)
   {
    long revision=(long)next["revision"],current=(long)source["revision"];
    if(revision<current)return;
    if(revision==current)
    {
     if(!JToken.DeepEquals(next,source))throw new InvalidOperationException("Conflicting focus source revision.");
     return;
    }
   }
   source=next;RefreshProjection();
  }
  public void RefreshProjection()
  {
   if(render==null||source==null)return;
   var original=source;long ticket=binding;
   string target=(string)original["targetId"];
   bool disclosed=original["informationVisible"].Type==JTokenType.Boolean&&(bool)original["informationVisible"];
   JObject position=null;
   Exception projectionFailure=null;
   try
   {
    if(target!=null&&disclosed)position=project(target,(long)original["targetGeneration"]);
    if(position!=null)ValidateProjection(position);
   }
   catch(Exception error){position=null;projectionFailure=error;}
   // A projection port may synchronously unbind/rebind or publish a newer source.
   if(ticket!=binding||render==null||!ReferenceEquals(original,source))return;
   bool onscreen=position!=null&&(bool)position["onScreen"];
   var participants=new JArray(((JArray)original["participants"]).Select(p=>new JObject{
    ["actorId"]=p["actorId"].DeepClone(),
    ["highlight"]=target!=null&&p["eligible"].Type==JTokenType.Boolean&&(bool)p["eligible"],
    ["reason"]=p["reason"].DeepClone()}));
   var roster=(JArray)original["participants"];
   bool none=roster.Count>0&&roster.All(p=>p["eligible"].Type==JTokenType.Boolean&&!(bool)p["eligible"]);
   string notice=target==null?"":!disclosed||position==null?"상태 확인 중":!onscreen?"집중 대상 화면 밖":none?"현재 공격 가능한 요원 없음":"";
   render(new JObject{
    ["revision"]=original["revision"].DeepClone(),["targetId"]=target,
    ["targetGeneration"]=original["targetGeneration"].DeepClone(),
    ["showFrame"]=onscreen,["x"]=onscreen?position["x"].DeepClone():JValue.CreateNull(),
    ["y"]=onscreen?position["y"].DeepClone():JValue.CreateNull(),["participants"]=participants,["notice"]=notice});
   // Remove obsolete coordinates first, then preserve the provider's original error.
   if(projectionFailure!=null)ExceptionDispatchInfo.Capture(projectionFailure).Throw();
  }
  // Existing hit-test and Controller own the command. No local target prediction,
  // automatic retry, alternate gesture, attack interruption or selection persistence.
  public bool RequestSelection(string hitTargetId,bool inputBlocked)
  {
   if(select==null||inputBlocked)return false;
   if(string.IsNullOrWhiteSpace(hitTargetId))throw new ArgumentException("Original hit target required.");
   select(hitTargetId);return true;
  }
  public void Unbind(){binding++;render=null;project=null;select=null;source=null;}
  public void Dispose()=>Unbind();

  static JObject Validate(JObject value)
  {
   if(value==null)throw new ArgumentNullException(nameof(value));
   var p=(JObject)value.DeepClone();
   // Newtonsoft's implicit conversion of a null string creates a String-typed
   // JValue whose payload is null. Normalize the same JSON null at this boundary.
   if(p["targetId"] is JValue targetValue&&targetValue.Value==null)p["targetId"]=JValue.CreateNull();
   RequireInteger(p["revision"]);RequireInteger(p["targetGeneration"]);
   if(p["targetId"]==null||p["targetId"].Type!=JTokenType.Null&&(p["targetId"].Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)p["targetId"])))throw new ArgumentException("Explicit selection required.");
   RequireNullableBoolean(p["informationVisible"]);
   if(p["participants"] is not JArray rows)throw new ArgumentException("Explicit participant snapshot required.");
   var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in rows)
   {
    if(row is not JObject||row["actorId"]?.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)row["actorId"])||!ids.Add((string)row["actorId"]))throw new ArgumentException("Unique participant identity required.");
    RequireNullableBoolean(row["eligible"]);
    if(row["reason"]==null||row["reason"].Type!=JTokenType.Null&&row["reason"].Type!=JTokenType.String)throw new ArgumentException("Original reason or explicit unknown required.");
   }
   return p;
  }
  static void RequireInteger(JToken v){if(v?.Type!=JTokenType.Integer||(long)v<0)throw new ArgumentException("Nonnegative source revision/generation required.");}
  static void RequireNullableBoolean(JToken v){if(v==null||v.Type!=JTokenType.Null&&v.Type!=JTokenType.Boolean)throw new ArgumentException("Explicit boolean or unknown required.");}
  static void ValidateProjection(JObject p)
  {
   if(p["onScreen"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Explicit camera visibility required.");
   foreach(string key in new[]{"x","y"})
   {
    var v=p[key];if(v==null||v.Type!=JTokenType.Integer&&v.Type!=JTokenType.Float||double.IsNaN((double)v)||double.IsInfinity((double)v))throw new ArgumentException("Finite current projection required.");
   }
  }
 }
}
