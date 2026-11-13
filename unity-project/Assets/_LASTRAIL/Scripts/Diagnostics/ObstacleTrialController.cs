#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using LastRail.Battle;
using LastRail.Core.Simulation;

namespace LastRail.Diagnostics {
 public sealed class ObstacleTrialController : IDisposable {
  readonly Func<ObstacleTrialModel> factory;
  readonly SimulationClock clock=new SimulationClock();
  double debt;
  public ObstacleTrialModel Model {get;private set;}
  public bool Paused=>clock.IsPaused;
  public float Rate=>clock.EffectiveSpeedMultiplier;
  public ObstacleTrialController(Func<ObstacleTrialModel> factory){this.factory=factory;Model=factory();}
  public void Throttle(int bps)=>Model.SetThrottle(bps);
  public void TogglePause()=>clock.SetPaused(!clock.IsPaused);
  public void CycleSpeed()=>clock.SetRequestedSpeed(clock.RequestedSpeed==SimulationSpeed.Normal?SimulationSpeed.OnePointFive:clock.RequestedSpeed==SimulationSpeed.OnePointFive?SimulationSpeed.Double:SimulationSpeed.Normal);
  public void Advance(float realSeconds){
   debt+=clock.Tick(realSeconds)*1000;
   int elapsed=(int)(debt+1e-5);debt-=elapsed;
   while(elapsed>0){int part=Math.Min(elapsed,1000);Model.Advance(part);elapsed-=part;}
  }
  public void Reenter(){Model.Retire();Model=factory();debt=0;clock.SetPaused(false);}
  public void Dispose()=>Model.Retire();
 }
}
#endif
