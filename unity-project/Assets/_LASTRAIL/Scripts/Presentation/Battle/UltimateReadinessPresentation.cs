using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LastRail.Presentation.Battle
{
 // Display vocabulary only. These are not new gameplay/API rejection codes.
 public enum UltimateReadinessReason { Unsupported, Unconfirmed, Down, Busy, InsufficientCharge, Cooldown, NoTarget, NoEffect }

 // Explicit read-only input from the ability owner; missing facts remain nullable.
 // A native B adapter must supply real charge/eligibility and a common source revision.
 public sealed class UltimateReadinessSnapshot
 {
  public string ActorId { get; }
  public long Generation { get; }
  public long Revision { get; }
  public string OpportunityId { get; }
  public int? Charge { get; }
  public int? Capacity { get; }
  public bool? ChargeSufficient { get; }
  public bool? Eligible { get; }
  public IReadOnlyList<UltimateReadinessReason> Reasons { get; }
  public UltimateReadinessReason? Representative { get; }
  public UltimateReadinessSnapshot(string actorId,long generation,long revision,string opportunityId,int? charge,int? capacity,bool? chargeSufficient,bool? eligible,UltimateReadinessReason[] reasons,UltimateReadinessReason? representative)
  {
   if(string.IsNullOrWhiteSpace(actorId)||string.IsNullOrWhiteSpace(opportunityId)||generation<0||revision<0)throw new ArgumentException("Readiness source identity is required.");
   if(charge<0||capacity<=0||charge.HasValue&&capacity.HasValue&&charge.Value>capacity.Value)throw new ArgumentException("Invalid original charge range.");
   if(reasons==null)throw new ArgumentNullException(nameof(reasons));
   var copy=reasons.Distinct().ToArray();
   if(copy.Any(x=>!Enum.IsDefined(typeof(UltimateReadinessReason),x))||representative.HasValue&&!copy.Contains(representative.Value)||eligible==true&&copy.Length!=0)throw new ArgumentException("Conflicting original eligibility reasons.");
   ActorId=actorId;Generation=generation;Revision=revision;OpportunityId=opportunityId;Charge=charge;Capacity=capacity;ChargeSufficient=chargeSufficient;Eligible=eligible;Reasons=new ReadOnlyCollection<UltimateReadinessReason>(copy);Representative=representative;
  }
  internal bool SameFacts(UltimateReadinessSnapshot other)=>ActorId==other.ActorId&&Generation==other.Generation&&Revision==other.Revision&&OpportunityId==other.OpportunityId&&Charge==other.Charge&&Capacity==other.Capacity&&ChargeSufficient==other.ChargeSufficient&&Eligible==other.Eligible&&Representative==other.Representative&&Reasons.SequenceEqual(other.Reasons);
 }

 // Share this immutable projection between the card and its button.
 public sealed class UltimateReadinessState
 {
  public string ActorId { get; }
  public long Generation { get; }
  public long Revision { get; }
  public string OpportunityId { get; }
  public int? Charge { get; }
  public int? Capacity { get; }
  public bool StrongReady { get; }
  public UltimateReadinessReason? Reason { get; }
  public string ReasonText { get; }
  internal UltimateReadinessState(UltimateReadinessSnapshot source,UltimateReadinessReason? reason,string text)
  {
   ActorId=source.ActorId;Generation=source.Generation;Revision=source.Revision;OpportunityId=source.OpportunityId;Charge=source.Charge;Capacity=source.Capacity;
   StrongReady=source.Eligible==true&&source.ChargeSufficient==true;Reason=reason;ReasonText=text;
  }
 }

 // One presentation binding per actor/card. Owns presentation callbacks only;
 // deliberately has no skill command, simulation clock, AUTO or charge mutation.
 public sealed class UltimateReadinessPresentation : IDisposable
 {
  static readonly UltimateReadinessReason[] Priority={UltimateReadinessReason.Unsupported,UltimateReadinessReason.Unconfirmed,UltimateReadinessReason.Down,UltimateReadinessReason.Busy,UltimateReadinessReason.InsufficientCharge,UltimateReadinessReason.Cooldown,UltimateReadinessReason.NoTarget,UltimateReadinessReason.NoEffect};
  readonly HashSet<Tuple<string,string,UltimateReadinessReason>> notices=new HashSet<Tuple<string,string,UltimateReadinessReason>>();
  long generation;Action<UltimateReadinessState> render;Action<string> manualNotice;UltimateReadinessSnapshot current;
  public UltimateReadinessState Current { get; private set; }
  public void Bind(long sceneGeneration,Action<UltimateReadinessState> display,Action<string> showManualNotice)
  {
   if(sceneGeneration<0)throw new ArgumentOutOfRangeException(nameof(sceneGeneration));
   if(display==null)throw new ArgumentNullException(nameof(display));if(showManualNotice==null)throw new ArgumentNullException(nameof(showManualNotice));
   Unbind();generation=sceneGeneration;render=display;manualNotice=showManualNotice;
  }
  public bool Observe(UltimateReadinessSnapshot source)
  {
   if(source==null)throw new ArgumentNullException(nameof(source));
   if(render==null||source.Generation!=generation)return false;
   if(current!=null)
   {
    if(source.ActorId!=current.ActorId)throw new InvalidOperationException("A readiness binding belongs to one actor.");
    if(source.Revision<current.Revision)return false;
    if(source.Revision==current.Revision){if(!source.SameFacts(current))throw new InvalidOperationException("Conflicting readiness facts at the same source revision.");return false;}
   }
   UltimateReadinessReason? reason=source.Representative;
   if(!reason.HasValue)foreach(var candidate in Priority)if(source.Reasons.Contains(candidate)){reason=candidate;break;}
   current=source;Current=new UltimateReadinessState(source,reason,Text(reason));render(Current);return true;
  }
  // Called only for an actual original manual rejection; never evaluates an ability.
  // Blocked fullscreen input is dropped here and is never buffered for later replay.
  public bool ReportRejection(UltimateReadinessSnapshot source,bool automatic,bool fullscreenBlocked)
  {
   if(automatic||fullscreenBlocked||manualNotice==null||source==null||current==null||!source.SameFacts(current)||source.Eligible!=false||!Current.Reason.HasValue)return false;
   var key=Tuple.Create(source.ActorId,source.OpportunityId,Current.Reason.Value);if(!notices.Add(key))return false;
   manualNotice(Current.ReasonText);return true;
  }
  static string Text(UltimateReadinessReason? reason)
  {
   switch(reason){case UltimateReadinessReason.Unsupported:return "현재 전투에서 사용 불가";case UltimateReadinessReason.Unconfirmed:return "상태 확인 중";case UltimateReadinessReason.Down:return "다운 중";case UltimateReadinessReason.Busy:return "행동 중";case UltimateReadinessReason.InsufficientCharge:return "충전 부족";case UltimateReadinessReason.Cooldown:return "재사용 대기";case UltimateReadinessReason.NoTarget:return "대상 없음";case UltimateReadinessReason.NoEffect:return "적용할 효과 없음";default:return "";}
  }
  public void Unbind(){render=null;manualNotice=null;current=null;Current=null;notices.Clear();}
  public void Dispose()=>Unbind();
 }
}
