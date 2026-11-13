using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Owns display bindings/receipts only. Formation mapping and projection are supplied
 // by the existing upper owner; reference clones can never become runtime actors.
 public sealed class FiveAgentVisualController:IDisposable {
  // Original identities live for this combat context, including same-context rebinds.
  // A context change or Dispose releases them; presentation imposes no attack-count ceiling.
  readonly FiveAgentVisualReference model;readonly Dictionary<string,JObject> delivered=new Dictionary<string,JObject>();
  readonly Dictionary<int,JObject> poses=new Dictionary<int,JObject>(),impacts=new Dictionary<int,JObject>();
  Dictionary<string,int> mapping;FiveAgentVisualFeed feed;Action<JObject> show;Action clear;FiveAgentVisualView view;string context;int facing;long clock;bool held,disposed;
  public FiveAgentVisualController(FiveAgentVisualReference model){this.model=model??throw new ArgumentNullException(nameof(model));}
  public void Bind(FiveAgentVisualFeed source,Action<JObject> present,Action clearOwnedView,Dictionary<string,int> verifiedSlots,string originalContextId,int initialDirection){
   if(disposed)throw new ObjectDisposedException(nameof(FiveAgentVisualController));if(source==null||present==null||clearOwnedView==null||verifiedSlots==null||string.IsNullOrWhiteSpace(originalContextId)||initialDirection<0||initialDirection>3)throw new ArgumentException("Explicit visual binding/context/direction required.");
   var copy=new Dictionary<string,int>(verifiedSlots);var ids=new[]{"op_kyle","op_reina","op_eve"};if(copy.Any(p=>p.Value<0||p.Value>=3||ids[p.Value]!=p.Key))throw new ArgumentException("Mapping cannot assign reference copies or unknown roster.");
   Unbind();if(context!=originalContextId)delivered.Clear();context=originalContextId;mapping=copy;facing=initialDirection;feed=source;show=present;clear=clearOwnedView;
   source.Pose+=OnPose;source.Impact+=OnImpact;source.Cancel+=OnCancel;source.TargetLost+=OnTargetLost;source.Hold+=OnHold;source.Clock+=OnClock;Publish();
  }
  public void BindView(FiveAgentVisualFeed source,FiveAgentVisualView target,Dictionary<string,int> verifiedSlots,string originalContextId,int initialDirection){if(target==null||!target.isActiveAndEnabled)throw new ArgumentException("Active configured view required.");Bind(source,target.Present,target.Clear,verifiedSlots,originalContextId,initialDirection);view=target;view.BindingEnded+=Unbind;}
  void OnPose(CombatUnitAnimationState state,int direction,long age){if(show==null||held)return;if(state==null||!mapping.TryGetValue(state.UnitId,out int slot))return;if(state.UnitRole!=CombatUnitRole.Operator||(state.AnimationKey!="idle"&&state.AnimationKey!="attack")||age<0||direction<0||direction>3)throw new ArgumentException("Original display cue required.");var frame=model.SampleReference(direction,state.AnimationKey=="idle"?0:age,false);poses[slot]=frame;Publish();}
  void OnImpact(BDirectAttackReceipt receipt,Vector2 point,long ms){if(show==null||receipt==null||!mapping.TryGetValue(receipt.SourceId,out int slot))return;if(ms<0||!float.IsFinite(point.x)||!float.IsFinite(point.y))throw new ArgumentException("Original projected impact required.");var fact=new JObject{["source"]=receipt.SourceId,["target"]=receipt.Hp.TargetId,["tick"]=receipt.Tick,["point"]=new JArray(point.x,point.y),["ms"]=ms};if(delivered.TryGetValue(receipt.EventId,out var old)){if(!JToken.DeepEquals(old,fact))throw new ArgumentException("Original impact identity conflict.");return;}delivered.Add(receipt.EventId,fact);impacts[slot]=new JObject{["kind"]="glow",["role"]="authoritativeImpact",["slot"]=slot,["order"]=0,["x"]=point.x,["y"]=point.y,["w"]=32,["h"]=32,["angle"]=0,["color"]=new JArray(1,.85f,.3f,1),["path"]=JValue.CreateNull(),["originalEventId"]=receipt.EventId,["targetId"]=receipt.Hp.TargetId,["at"]=ms};if(!held)Publish();}
  void OnCancel(string id){if(mapping==null||!mapping.TryGetValue(id,out int slot))return;poses.Remove(slot);impacts.Remove(slot);if(!held)Publish();}
  void OnTargetLost(string id){foreach(var slot in impacts.Where(p=>(string)p.Value["targetId"]==id).Select(p=>p.Key).ToArray()){impacts.Remove(slot);poses.Remove(slot);}if(!held)Publish();}
  void OnHold(bool value)=>held=value;
  void OnClock(long ms){if(show==null||held)return;if(ms<clock)throw new ArgumentException("Original display clock cannot rewind without a new binding.");clock=ms;foreach(var key in impacts.Where(p=>ms>=(long)p.Value["at"]+160).Select(p=>p.Key).ToArray())impacts.Remove(key);Publish();}
  void Publish(){if(show==null)return;var baseline=model.SampleReference(facing,0,false);var layers=new JArray();foreach(var pair in mapping){var frame=poses.TryGetValue(pair.Value,out var pose)?pose:baseline;foreach(var l in frame["layers"].Where(l=>(int)l["slot"]==pair.Value))layers.Add(l.DeepClone());}foreach(var i in impacts.Values)layers.Add(i.DeepClone());var actors=new JArray(baseline["actors"].Where(a=>mapping.ContainsValue((int)a["slot"])).Select(a=>{var row=(JObject)a.DeepClone();row["runtimeId"]=mapping.Single(p=>p.Value==(int)a["slot"]).Key;return row;}));show(new JObject{["version"]="unity.five-agent-reference-frame.v1.20261017",["classification"]="MAPPED_EVENT_DISPLAY_NOT_GEOMETRY_AUTHORITY",["direction"]=facing,["gameplayAuthority"]=false,["actors"]=actors,["layers"]=new JArray(layers.OrderBy(l=>(int)l["order"])),["receiptCount"]=delivered.Count});}
  public void Unbind(){if(feed!=null){feed.Pose-=OnPose;feed.Impact-=OnImpact;feed.Cancel-=OnCancel;feed.TargetLost-=OnTargetLost;feed.Hold-=OnHold;feed.Clock-=OnClock;}if(view!=null)view.BindingEnded-=Unbind;view=null;feed=null;show=null;var owned=clear;clear=null;mapping=null;poses.Clear();impacts.Clear();held=false;clock=0;owned?.Invoke();}
  public void Dispose(){if(disposed)return;Unbind();delivered.Clear();context=null;disposed=true;}
 }
}