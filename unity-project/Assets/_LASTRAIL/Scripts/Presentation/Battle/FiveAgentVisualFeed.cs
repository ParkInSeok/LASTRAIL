using System;
using LastRail.Battle;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // A local composition port. Existing upper owner forwards its results/cues here;
 // this source never schedules an attack or changes its receipt/HP.
 public sealed class FiveAgentVisualFeed {
  public event Action<CombatUnitAnimationState,int,long> Pose;
  public event Action<BDirectAttackReceipt,Vector2,long> Impact;
  public event Action<string> Cancel,TargetLost;
  public event Action<bool> Hold;
  public event Action<long> Clock;
  public void PublishPose(CombatUnitAnimationState original,int direction,long referenceAgeMs)=>Pose?.Invoke(original,direction,referenceAgeMs);
  public void PublishResult(BDirectAttackBasicLoopResult original,Func<string,Vector2?> projectTarget,Func<long,long> originalTickToMs){if(original==null||projectTarget==null||originalTickToMs==null)throw new ArgumentNullException();foreach(var hit in original.Hits){var point=projectTarget(hit.Attack.Hp.TargetId);if(point.HasValue)Impact?.Invoke(hit.Attack,point.Value,originalTickToMs(hit.Attack.Tick));}}
  public void PublishImpact(BDirectAttackReceipt original,Vector2 point,long originalMs)=>Impact?.Invoke(original,point,originalMs);
  public void PublishCancel(string runtimeId)=>Cancel?.Invoke(runtimeId);
  public void PublishTargetLost(string targetId)=>TargetLost?.Invoke(targetId);
  public void PublishHold(bool value)=>Hold?.Invoke(value);
  public void PublishClock(long originalMs)=>Clock?.Invoke(originalMs);
 }
}