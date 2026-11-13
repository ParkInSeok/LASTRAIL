using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
 // Explicit display fixtures, not a native selection/eligibility producer.
 public sealed class FocusFeedbackPresentationTests
 {
  object Create(){var t=typeof(CombatPresentationPolicy).Assembly.GetType("LastRail.Presentation.Battle.FocusFeedbackPresentation");Assert.That(t,Is.Not.Null,"Focus consumer missing");return Activator.CreateInstance(t);}
  object Call(object p,string m,params object[] a)=>p.GetType().GetMethod(m).Invoke(p,a);
  JObject Source(long revision=1,string target="enemy",long generation=2)=>new JObject{["revision"]=revision,["targetId"]=target,["targetGeneration"]=generation,["informationVisible"]=true,["participants"]=new JArray(new JObject{["actorId"]="ally",["eligible"]=true,["reason"]=null})};
  JObject Projection(bool visible=true)=>new JObject{["onScreen"]=visible,["x"]=12,["y"]=34};
  Action<JObject> Bind(object p,Action<JObject> render,Func<string,long,JObject> project=null,Action<string> select=null)=> (Action<JObject>)Call(p,"Bind",render,project??((id,g)=>Projection()),select??(_=>{}));
  [Test] public void OffscreenRetainsSelectionAndEligibility(){var p=Create();JObject frame=null;Bind(p,x=>frame=x,(id,g)=>Projection(false))(Source());Assert.That((string)frame["targetId"],Is.EqualTo("enemy"));Assert.That((bool)frame["showFrame"],Is.False);Assert.That((bool)frame["participants"][0]["highlight"],Is.True);Assert.That((string)frame["notice"],Is.EqualTo("집중 대상 화면 밖"));}
  [Test] public void UnknownEligibilityNeverHighlights(){var p=Create();JObject frame=null;var s=Source();s["participants"][0]["eligible"]=null;Bind(p,x=>frame=x)(s);Assert.That((bool)frame["participants"][0]["highlight"],Is.False);}
  [Test] public void ReprojectUsesCurrentCameraWithoutNewSelection(){var p=Create();JObject frame=null;int x=12,commands=0;Bind(p,f=>frame=f,(id,g)=>new JObject{["onScreen"]=true,["x"]=x,["y"]=34},_=>commands++)(Source());x=80;Call(p,"RefreshProjection");Assert.That((int)frame["x"],Is.EqualTo(80));Assert.That(commands,Is.Zero);}
  [Test] public void MissingProjectionClearsOldCoordinates(){var p=Create();JObject frame=null;bool known=true;Bind(p,f=>frame=f,(id,g)=>known?Projection():null)(Source());known=false;Call(p,"RefreshProjection");Assert.That((bool)frame["showFrame"],Is.False);Assert.That(frame["x"].Type,Is.EqualTo(JTokenType.Null));}
  [Test] public void HiddenInformationDoesNotQueryPosition(){var p=Create();int queries=0;JObject frame=null;var s=Source();s["informationVisible"]=false;Bind(p,f=>frame=f,(id,g)=>{queries++;return Projection();})(s);Assert.That(queries,Is.Zero);Assert.That((bool)frame["showFrame"],Is.False);}
  [Test] public void OldRevisionCannotOverwriteNewGeneration(){var p=Create();JObject frame=null;var observe=Bind(p,f=>frame=f);observe(Source(2,generation:3));observe(Source(1));Assert.That((long)frame["targetGeneration"],Is.EqualTo(3));}
  [Test] public void OldBindingCallbackCannotUpdateOrInvokeProjection(){var p=Create();int old=0,fresh=0;var stale=Bind(p,_=>old++);Bind(p,_=>fresh++);stale(Source());Assert.That(old+fresh,Is.Zero);}
  [Test] public void UnbindStopsAllCallbacksAndInput(){var p=Create();int n=0;var observe=Bind(p,_=>n++,select:_=>n++);Call(p,"Unbind");observe(Source());Call(p,"RefreshProjection");Call(p,"RequestSelection","enemy",false);Assert.That(n,Is.Zero);}
  [Test] public void BlockedInputIsNeverQueuedAndAcceptedInputDelegatesOnce(){var p=Create();int n=0;Bind(p,_=>{},select:_=>n++);Call(p,"RequestSelection","enemy",true);Call(p,"RefreshProjection");Assert.That(n,Is.Zero);Call(p,"RequestSelection","enemy",false);Assert.That(n,Is.EqualTo(1));}
  [Test] public void DetachedInputAndOutputCannotMutateRetainedSource(){var p=Create();JObject frame=null;var s=Source();Bind(p,f=>frame=f)(s);s["targetId"]="mutated";frame["participants"][0]["highlight"]=false;Call(p,"RefreshProjection");Assert.That((string)frame["targetId"],Is.EqualTo("enemy"));Assert.That((bool)frame["participants"][0]["highlight"],Is.True);}
  [Test] public void OwnerClearRemovesFrameAndParticipation(){var p=Create();JObject frame=null;var observe=Bind(p,f=>frame=f);observe(Source());observe(Source(2,null));Assert.That((bool)frame["showFrame"],Is.False);Assert.That((bool)frame["participants"][0]["highlight"],Is.False);}
  [Test] public void ProjectionReentranceCannotRenderAfterUnbind(){var p=Create();int n=0;Bind(p,_=>n++,(id,g)=>{Call(p,"Unbind");return Projection();})(Source());Assert.That(n,Is.Zero);}
  [Test] public void ConflictingSameRevisionIsRejected(){var p=Create();var observe=Bind(p,_=>{});observe(Source());Assert.Throws<InvalidOperationException>(()=>observe(Source(target:"other")));}
  [Test] public void AllIneligibleNoticeRequiresKnownNonemptyRoster(){var p=Create();JObject f=null;var observe=Bind(p,x=>f=x);var s=Source();s["participants"][0]["eligible"]=false;observe(s);Assert.That((string)f["notice"],Is.EqualTo("현재 공격 가능한 요원 없음"));s=Source(2);s["participants"]=new JArray();observe(s);Assert.That((string)f["notice"],Is.EqualTo(""));}
  [Test] public void NullStringSourceIsJsonNullButKeepsStringTokenType(){var s=Source(target:null);Assert.That(s["targetId"].Type,Is.EqualTo(JTokenType.String));Assert.That(((JValue)s["targetId"]).Value,Is.Null);Assert.That(s.ToString(),Does.Contain("\"targetId\": null"));}
  [Test] public void InvalidProjectionClearsPreviousMarkerAndReportsFailure(){var p=Create();JObject f=null;bool valid=true;Bind(p,x=>f=x,(id,g)=>valid?Projection():new JObject{["onScreen"]=true,["x"]=double.NaN,["y"]=34})(Source());valid=false;Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(p,"RefreshProjection"));Assert.That((bool)f["showFrame"],Is.False);Assert.That((string)f["targetId"],Is.EqualTo("enemy"));}
  [Test] public void ThrowingProjectionClearsPreviousMarkerAndReportsFailure(){var p=Create();JObject f=null;bool valid=true;Bind(p,x=>f=x,(id,g)=>valid?Projection():throw new InvalidOperationException("camera unavailable"))(Source());valid=false;Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(p,"RefreshProjection"));Assert.That((bool)f["showFrame"],Is.False);}
 }
}
