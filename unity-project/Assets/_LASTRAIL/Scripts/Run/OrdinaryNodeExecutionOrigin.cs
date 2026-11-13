using LastRail.Battle;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Validated part of a committed node snapshot; it does not grant runtime readiness.
 public sealed class OrdinaryNodeExecutionOrigin {
  public uint Seed {get;}
  public uint InitialState {get;}
  OrdinaryNodeExecutionOrigin(uint seed,uint state){Seed=seed;InitialState=state;}
  internal static OrdinaryNodeExecutionOrigin Read(JObject runtime){
   OrdinaryNodeStart.Need((string)runtime["executionVersion"]==OrdinaryFiveCombatFactory.Version);
   var value=runtime["determinism"] as JObject;
   BDirectAttackInput.Fields(value,"version","algorithm","seed","initialState");
   OrdinaryNodeStart.Need((string)value["version"]=="ordinary.node-random.v1.20261029"&&(string)value["algorithm"]=="XORSHIFT32_13_17_5_V1");
   uint seed=checked((uint)BDirectAttackInput.Nat(value,"seed",0,uint.MaxValue));
   uint state=checked((uint)BDirectAttackInput.Nat(value,"initialState",1,uint.MaxValue));
   OrdinaryNodeStart.Need(new RunRandom(seed).ExportState()==state);
   return new OrdinaryNodeExecutionOrigin(seed,state);
  }
  // Fresh node attempts start here; continuing one attempt retains its own random owner.
  public RunRandom CreateInitialRandom()=>new RunRandom(Seed);
 }
}
