using System;

namespace LastRail.Battle {
 public static class OrdinaryRailKinematics {
  // The same integer trapezoid and remainder used by ordinary node movement.
  public static (long Front,long Speed,long Remainder) Advance(long front,long speed,long target,long remainder,long milliseconds) {
   if(front<0||speed<0||target<0||remainder<0||remainder>=2000||milliseconds<0)throw new ArgumentOutOfRangeException();
   long delta=Math.Min(Math.Abs(target-speed),checked(5000*milliseconds/1000));
   long next=checked(speed+Math.Sign(target-speed)*delta);
   long travel=checked((speed+next)*milliseconds+remainder);
   return (checked(front+travel/2000),next,travel%2000);
  }
 }
}
