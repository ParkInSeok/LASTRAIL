using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Parent binds Observe to its existing node callback alongside other Views.
 // Does not own or remove the parent's callback, clock, targeting, stats, HP or scene.
 public sealed class MiraFirstFeedbackController {
  readonly HashSet<string> consumed=new HashSet<string>(StringComparer.Ordinal);
  Action<JObject> present;Action clear;string context;
  public void Bind(Action<JObject> show,Action clearOwnedView,JObject initial){
   if(show==null||clearOwnedView==null)throw new ArgumentNullException();var root=Root(initial);Unbind();
   present=show;clear=clearOwnedView;context=Key(root);consumed.Clear();Publish(root,true);
  }
  public void Unbind(){var cleanup=clear;present=null;clear=null;context=null;consumed.Clear();cleanup?.Invoke();}
  public void Observe(JObject accepted){if(present==null)return;var root=Root(accepted);if(Key(root)!=context)throw new ArgumentException("mira_feedback_context_changed_rebind_required");Publish(root,false);}
  static JObject Root(JObject value){var root=value?["runtime"] as JObject??value;if(root==null||((string)root["version"]!=BDirectAttackBasicLoop.MiraFirstVersion&&(string)root["version"]!=BDirectAttackBasicLoop.MiraEquippedVersion)||root["miraFirstTrial"] is not JObject)throw new ArgumentException("mira_feedback_original_runtime_required");return root;}
  static string Key(JObject root)=>(string)root["bindingHash"]+":"+(string)root["miraFirstTrial"]["contextHash"];
  void Publish(JObject root,bool baseline){
   var state=root["miraFirstTrial"];var clock=root["state"]["direct"]["clock"];long tick=(long)clock["completedSteps"],step=(long)clock["stepTicks"];
   var flights=new JArray();var pulses=new JArray();
   JObject pulseShot=null;
   void Pulse(string eventId,string kind,JToken point,JToken receipt=null){
    string key=new JArray(eventId,pulseShot?["sourceGeneration"],pulseShot?["targetId"],pulseShot?["targetGeneration"],kind).ToString(Newtonsoft.Json.Formatting.None);
    if(!consumed.Add(key)||baseline)return;
    var pulse=new JObject{["id"]=key,["kind"]=kind,["pointMm"]=point.DeepClone(),["targetId"]=pulseShot?["targetId"]?.DeepClone(),["targetGeneration"]=pulseShot?["targetGeneration"]?.DeepClone(),["sourceGeneration"]=pulseShot?["sourceGeneration"]?.DeepClone()};
    if(receipt!=null){pulse["receipt"]=receipt.DeepClone();if(kind=="CONTACT"){
     var actual=(JObject)receipt["hp"].DeepClone();actual["criticalState"]=(string)state["context"]?["critical"]?["status"]??"UNKNOWN";
     pulse["labels"]=CombatResultLabels.Read(new JObject{["kind"]="DAMAGE",["actualResult"]=actual});
    }}pulses.Add(pulse);
   }
   foreach(var raw in (JArray)state["shots"]){var shot=(JObject)raw;pulseShot=shot;string id=(string)shot["id"],status=(string)shot["status"];var ray=(JObject)shot["ray"];
    Pulse(id,"FIRE",ray["originMm"]);
    if(status=="FLYING")flights.Add(new JObject{["id"]=id,["kind"]=(string)shot["kind"]??"MAIN",["positionMm"]=new JArray(MiraJudgementGeometry.Position(ray,checked((tick-(long)shot["launchTick"])*step/10))),["originMm"]=ray["originMm"].DeepClone(),["anchorMm"]=ray["anchorMm"].DeepClone()});
    else if(status=="HIT"){
     var receipt=((JArray)state["contacts"]).Single(c=>(string)c["eventId"]==id+":hp");var point=shot["contact"]["pointMm"];Pulse(id,"CONTACT",point,receipt);
     if((int)receipt["hp"]["absorbed"]>0)Pulse(id,"SHIELD",point,receipt);
     if((int)receipt["hp"]["hpLoss"]>0)Pulse(id,"HP",point,receipt);
    }else if(status=="BLOCKED")Pulse(id,"BLOCKED",shot["contact"]["pointMm"]);
    // MISS/expiry has no hit flash, damage text or repeated MISS label.
   }
   present(new JObject{["version"]="mira.first-feedback-frame.v1.20261015",["context"]=context,["tick"]=tick,["held"]=(bool)clock["paused"],["basis"]=state["lastObservation"]["basis"].DeepClone(),["flights"]=flights,["pulses"]=pulses});
  }
 }
}