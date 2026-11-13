using System;
using LastRail.Run;
namespace LastRail.Battle {
 public enum BEnemyBoardingPhase { Planned, Entrance, Approach, WaitingSocket, Attached, Climbing, Rooftop, GeometryPending, Dead }
 // Approved immutable v06 durations. Risk speeds apply only to entry opportunities.
 public sealed class BWaveBoardingPolicy {
  public const string Version="unity.combat-b.wave-boarding-policy.v06-v07.20261004";
  readonly BBoardingPolicyDefinition received;
  public string ReceivedDefinitionHash {get;}
  public int AliveCap {get;}public int FirstGroup {get;}public int MinimumCount {get;}public int MaximumCount {get;}
  public int EntranceMilliseconds=>received?.GraceMilliseconds??600;public int ApproachMilliseconds=>received?.ApproachMilliseconds??800;public int AttachedMilliseconds=>received?.AttachedMilliseconds??200;public int ClimbingMilliseconds=>received?.ClimbingMilliseconds??1800;public int WaveGapMilliseconds=>2000;public int NextGroupMaximum=>2;public int SocketsPerCarriage=>received==null?2:received.LeftSockets+received.RightSockets;
  public string Band {get;}
  BWaveBoardingPolicy(int band,BBoardingEventsDefinitionSnapshot definition=null){received=definition?.Boarding;ReceivedDefinitionHash=definition==null?null:(string)definition.Capture()["definition"]["definitionHash"];AliveCap=new[]{4,6,8}[band];FirstGroup=new[]{4,4,6}[band];MinimumCount=new[]{6,9,13}[band];MaximumCount=new[]{8,12,16}[band];Band=new[]{"EARLY","MIDDLE","LATE"}[band];}
  public static BWaveBoardingPolicy ForDepth(int depth){if(depth<1||depth>11)throw new ArgumentOutOfRangeException(nameof(depth));return new BWaveBoardingPolicy(depth<=4?0:depth<=8?1:2);}
  internal long DurationTicks(int milliseconds,long stepMicroseconds)=>CombatActionTiming.BasicAttackPeriodTicks(checked((long)milliseconds*1000),stepMicroseconds,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
  internal static BWaveBoardingPolicy FromReceived(int depth,BBoardingEventsDefinitionSnapshot definition){if(definition==null)throw new ArgumentNullException(nameof(definition));ForDepth(depth);return new BWaveBoardingPolicy(depth<=4?0:depth<=8?1:2,definition);}
  static string SourcePhase(BEnemyBoardingPhase phase){switch(phase){case BEnemyBoardingPhase.Entrance:return "GRACE";case BEnemyBoardingPhase.Attached:return "ATTACHED";case BEnemyBoardingPhase.Climbing:return "CLIMBING";case BEnemyBoardingPhase.Rooftop:return "ROOFTOP";default:return "APPROACH";}}
  internal bool PhaseTargetable(BEnemyBoardingPhase phase)=>phase!=BEnemyBoardingPhase.Planned&&phase!=BEnemyBoardingPhase.Dead&&(received==null?phase!=BEnemyBoardingPhase.Entrance:received.Eligibility(SourcePhase(phase)).Targetable);
  internal bool PhaseAttackEligible(BEnemyBoardingPhase phase)=>received==null?phase==BEnemyBoardingPhase.Rooftop:received.Eligibility(SourcePhase(phase)).AttackEligible;
  public bool ClimbingBonusAtImpact(BEnemyBoardingPhase phase)=>phase!=BEnemyBoardingPhase.Planned&&phase!=BEnemyBoardingPhase.Dead&&(received==null?phase==BEnemyBoardingPhase.Attached||phase==BEnemyBoardingPhase.Climbing:received.Eligibility(SourcePhase(phase)).ClimbBonusEligible);
 }
}
