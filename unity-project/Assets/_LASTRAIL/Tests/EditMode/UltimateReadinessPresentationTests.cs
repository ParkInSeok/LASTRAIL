using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
 // Explicit presentation fixtures; no native B eligibility producer or five-operator integration claim.
 public sealed class UltimateReadinessPresentationTests
 {
  Type Type(string name){var t=typeof(CombatPresentationPolicy).Assembly.GetType("LastRail.Presentation.Battle."+name);Assert.That(t,Is.Not.Null,"Approved readiness presentation is missing.");return t;}
  object Reason(string name)=>Enum.Parse(Type("UltimateReadinessReason"),name);
  object Snapshot(long revision=3,string opportunity="fixture-op",bool? eligible=false,string[] reasons=null,string representative=null,long generation=7,int? charge=100,bool? sufficient=true){var reasonType=Type("UltimateReadinessReason");var values=Array.CreateInstance(reasonType,(reasons??Array.Empty<string>()).Length);for(int i=0;i<values.Length;i++)values.SetValue(Reason(reasons[i]),i);return Activator.CreateInstance(Type("UltimateReadinessSnapshot"),new object[]{"fixture-actor",generation,revision,opportunity,charge,(int?)100,sufficient,eligible,values,representative==null?null:Reason(representative)});}
  object Create()=>Activator.CreateInstance(Type("UltimateReadinessPresentation"));
  object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
  object Get(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
  void Bind(object p,Action<object> render,Action<string> notice){var stateType=Type("UltimateReadinessState");var method=GetType().GetMethod(nameof(TypedRender),System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).MakeGenericMethod(stateType);var typed=method.Invoke(this,new object[]{render});Call(p,"Bind",7L,typed,notice);}
  Action<T> TypedRender<T>(Action<object> callback)=>value=>callback(value);
  [Test] public void ChargeStaysFullWhenAuthoritativeDownDisablesEmphasis(){var p=Create();object state=null;Bind(p,x=>state=x,_=>{});Call(p,"Observe",Snapshot(reasons:new[]{"Down"}));Assert.That(Get(state,"Charge"),Is.EqualTo(100));Assert.That(Get(state,"StrongReady"),Is.False);Assert.That(Get(state,"ReasonText"),Is.EqualTo("다운 중"));Assert.That(Get(state,"Revision"),Is.EqualTo(3));}
  [Test] public void StrongReadyRequiresBothAuthoritativeEligibilityAndSufficientCharge(){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(eligible:true));Assert.That(Get(s,"StrongReady"),Is.True);Call(p,"Observe",Snapshot(revision:4,eligible:null));Assert.That(Get(s,"StrongReady"),Is.False);Assert.That(Get(s,"ReasonText"),Is.EqualTo(""));}
  [Test] public void OwnerRepresentativeOverridesDisplayPriority(){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(reasons:new[]{"Unsupported","Down","NoEffect"},representative:"NoEffect"));Assert.That(Get(s,"ReasonText"),Is.EqualTo("적용할 효과 없음"));}
  [TestCase("Unsupported","현재 전투에서 사용 불가")][TestCase("Unconfirmed","상태 확인 중")][TestCase("NoTarget","대상 없음")]
  public void UnsupportedUnconfirmedAndNoTargetStayDistinct(string reason,string text){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(reasons:new[]{reason}));Assert.That(Get(s,"ReasonText"),Is.EqualTo(text));}
  [Test] public void MissingRepresentativeUsesApprovedDisplayPriority(){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(reasons:new[]{"NoTarget","Down","Busy","Cooldown"}));Assert.That(Get(s,"ReasonText"),Is.EqualTo("다운 중"));}
  [Test] public void RepeatedManualNoticeOncePerOpportunityAndAutoAlwaysSilent(){var p=Create();int n=0;Bind(p,_=>{},_=>n++);var a=Snapshot(reasons:new[]{"NoTarget"});Call(p,"Observe",a);Call(p,"ReportRejection",a,true,false);Call(p,"ReportRejection",a,false,false);Call(p,"ReportRejection",a,false,false);Assert.That(n,Is.EqualTo(1));var b=Snapshot(4,"next",reasons:new[]{"NoTarget"});Call(p,"Observe",b);Call(p,"ReportRejection",b,false,false);Assert.That(n,Is.EqualTo(2));}
  [Test] public void FullscreenBlockedInputIsDroppedWithoutNoticeOrDeferredAction(){var p=Create();int n=0;Bind(p,_=>{},_=>n++);var a=Snapshot(reasons:new[]{"Busy"});Call(p,"Observe",a);Assert.That(Call(p,"ReportRejection",a,false,true),Is.EqualTo(false));Call(p,"Observe",Snapshot(4,eligible:true));Assert.That(n,Is.Zero);}
  [Test] public void StaleRevisionGenerationAndUnboundCallbacksCannotUpdate(){var p=Create();int n=0;Bind(p,_=>n++,_=>n++);var current=Snapshot(reasons:new[]{"Down"});Call(p,"Observe",current);Call(p,"Observe",Snapshot(2));Call(p,"Observe",Snapshot(4,generation:8));Call(p,"ReportRejection",Snapshot(2,reasons:new[]{"Down"}),false,false);Assert.That(n,Is.EqualTo(1));Call(p,"Unbind");Call(p,"Observe",Snapshot(5));Call(p,"ReportRejection",current,false,false);Assert.That(n,Is.EqualTo(1));}
  [Test] public void SameRevisionConflictKeepsPreviousProjection(){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(reasons:new[]{"Down"}));var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(p,"Observe",Snapshot(reasons:new[]{"NoTarget"})));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(Get(s,"ReasonText"),Is.EqualTo("다운 중"));}
  [Test] public void InvalidChargeIsRejectedRatherThanClampedOrCleared(){var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Snapshot(charge:101));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
  [Test] public void ReadyCannotBeInferredFromChargeWhenEligibilityMissing(){var p=Create();object s=null;Bind(p,x=>s=x,_=>{});Call(p,"Observe",Snapshot(eligible:null));Assert.That(Get(s,"Charge"),Is.EqualTo(100));Assert.That(Get(s,"StrongReady"),Is.False);Assert.That(Get(s,"ReasonText"),Is.EqualTo(""));}
  [Test] public void RebindReplacesOwnedCallbacksAndResetsOpportunityLifetime(){var p=Create();int old=0,fresh=0;Bind(p,_=>old++,_=>old++);Bind(p,_=>fresh++,_=>fresh++);var a=Snapshot(reasons:new[]{"Down"});Call(p,"Observe",a);Call(p,"ReportRejection",a,false,false);Assert.That(old,Is.Zero);Assert.That(fresh,Is.EqualTo(2));}
 }
}

