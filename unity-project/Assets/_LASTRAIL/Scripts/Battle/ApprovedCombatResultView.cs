using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 sealed class ResultMark {public CombatResultGlyph Glyph;public string Target;public float Start,Duration;}
 readonly List<ResultMark> resultMarks=new List<ResultMark>();
 sealed class SourceLine {public CombatSourceTrace Graphic;public string Target,Source;public float Start,Duration;}
 readonly List<SourceLine> sourceLines=new List<SourceLine>();readonly HashSet<string> linkedCommands=new HashSet<string>();
 public event Action<string,string,string> ResolvedVisualCue;
 public int ResolvedVisualCount{get;private set;}
 double lastVisualClock;float resultClock;
 Vector2 RootPoint(Vector3 world){Vector2 local=root.InverseTransformPoint(world);return local-new Vector2(root.rect.xMin,root.rect.yMax);}
 Vector2 ShieldPoint(string id,Vector2 fallback){if(!units.TryGetValue(id,out var unit))return fallback;var bar=unit.Hp.rectTransform;return RootPoint(bar.TransformPoint(new Vector2(bar.rect.xMax+16,bar.rect.center.y)));}
 bool ActorPoint(JObject state,string id,out Vector2 point){
  point=default;var actor=((JArray)state["actors"]).FirstOrDefault(r=>(string)r["id"]==id);
  if(actor==null||(int)actor["hp"]<=0||(long)actor["availableAt"]>(long)state["tick"])return false;
  RectTransform transform=null;
  if(units.TryGetValue(id,out var unit))transform=unit.Art.rectTransform;
  else if((bool)actor["train"])transform=trainLayer.rectTransform;
  else transform=mounts.FirstOrDefault(m=>m.ActorId==id)?.Image.rectTransform;
  if(transform==null)return false;point=RootPoint(transform.TransformPoint(transform.rect.center));return true;
 }
 void ResultVisual(JObject state,JObject row){
  if(supportFire!=null&&(string)row["abilityId"]=="support_fire"&&(string)row["kind"]=="DAMAGE")return;
  if(repairDrone!=null&&(string)row["abilityId"]=="emergency_repair"&&(string)row["kind"]=="REPAIR")return;
  string shape=CombatResolvedVisual.Shape(row),target=(string)row["targetId"];
  if(electromagnetic!=null&&shape=="ShieldIssue"&&((JArray)state["actors"]).Any(a=>(bool)a["train"]&&(string)a["id"]==target))return;
  if(!ActorPoint(state,target,out var point))return;
  AddKyleGrant(state,row);
  string source=(string)row["sourceId"],command=(string)row["commandId"];
  bool warning=((JArray)state["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=(long)state["tick"]+4);
  if(UltimateAbility(state,(string)row["abilityId"])&&presentation.Mode!=CombatPresentationMode.Simplified&&!warning&&source!=target&&!linkedCommands.Contains(command)&&ActorPoint(state,source,out var start)){linkedCommands.Add(command);AddSourceLine(start,point,source,target,false,.22f);}
  if(shape==null)return;
  if(shape=="ShieldIssue")point=ShieldPoint(target,point);
  float size=shape=="ShieldIssue"?(units.ContainsKey(target)?40:118):shape=="Repair"?100:86;
  bool exterior=((JArray)state["actors"]).Any(a=>(string)a["id"]==target&&((bool)a["exterior"]||(bool)a["train"]||(bool)a["device"]));
  var node=Rect("Actual_"+shape+"_"+target,exterior?exteriorActors:root,point.x-size/2,-point.y-size/2,size,size);
  var glyph=node.gameObject.AddComponent<CombatResultGlyph>();glyph.Shape=(CombatResultShape)Enum.Parse(typeof(CombatResultShape),shape);glyph.raycastTarget=false;glyph.SetProgress(0);
  resultMarks.Add(new ResultMark{Glyph=glyph,Target=target,Start=resultClock,Duration=CombatResolvedVisual.Duration(shape)});ResolvedVisualCount++;ResolvedVisualCue?.Invoke(shape,(string)row["id"],target);

 }
 void AddSourceLine(Vector2 from,Vector2 to,string source,string target,bool cannon,float duration){var line=Rect(cannon?"ActualCannonTrace":"LeaderResolvedRecipient",cannon?exteriorActors:root,0,0,720,1280).gameObject.AddComponent<CombatSourceTrace>();line.raycastTarget=false;line.From=from;line.To=to;line.Cannon=cannon;line.SetProgress(0);sourceLines.Add(new SourceLine{Graphic=line,Target=target,Source=source,Start=resultClock,Duration=duration});}
 void CannonContact(JObject state,JToken hit,Mounted mount){if(mount?.Muzzle==null||((int)hit["HpLoss"]<=0&&(int)hit["Absorbed"]<=0)||!ActorPoint(state,(string)hit["Target"],out var point))return;var muzzle=mount.Muzzle.rectTransform;Vector2 start=RootPoint(muzzle.TransformPoint(muzzle.rect.center));AddSourceLine(start,point,mount.ActorId,(string)hit["Target"],true,.1f);}
 static bool ImminentEnemyWarning(JObject state)=>((JArray)state["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=(long)state["tick"]+4);
 void UpdateResultVisuals(JObject state){
  if((string)state["outcome"]!="Running"){
   // Terminal committed state ends cosmetic work even when its clock is paused.
   // Keep the authoritative result journal and its presentation acknowledgements.
   foreach(var mark in resultMarks)DisposeVisual(mark.Glyph.gameObject);resultMarks.Clear();
   foreach(var line in sourceLines)DisposeVisual(line.Graphic.gameObject);sourceLines.Clear();
   foreach(var cue in kyleGrants)DisposeVisual(cue.Graphic.gameObject);kyleGrants.Clear();
   linkedCommands.Clear();return;
  }
  double now=presentationClock();if(lastVisualClock!=0&&!(bool)state["paused"])resultClock+=(float)Math.Min(.1,Math.Max(0,now-lastVisualClock));lastVisualClock=now;
  UpdateKyleGrants(state);
  foreach(var mark in resultMarks.ToArray()){
   float progress=(resultClock-mark.Start)/mark.Duration;
   if(progress>=1||!ActorPoint(state,mark.Target,out var point)){DisposeVisual(mark.Glyph.gameObject);resultMarks.Remove(mark);continue;}
   if(mark.Glyph.Shape==CombatResultShape.ShieldIssue)point=ShieldPoint(mark.Target,point);
   var rect=mark.Glyph.rectTransform;rect.anchoredPosition=new Vector2(point.x-rect.sizeDelta.x/2,point.y+rect.sizeDelta.y/2);mark.Glyph.SetProgress(progress);
  }
  foreach(var line in sourceLines.ToArray()){float progress=(resultClock-line.Start)/line.Duration;if(progress>=1||!line.Graphic.Cannon&&ImminentEnemyWarning(state)||!ActorPoint(state,line.Target,out var target)){DisposeVisual(line.Graphic.gameObject);sourceLines.Remove(line);continue;}line.Graphic.To=target;if(!line.Graphic.Cannon&&ActorPoint(state,line.Source,out var from))line.Graphic.From=from;line.Graphic.SetProgress(progress);}
 }
}
}
