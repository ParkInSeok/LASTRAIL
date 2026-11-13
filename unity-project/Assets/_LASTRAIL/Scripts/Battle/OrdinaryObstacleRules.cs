using System;

namespace LastRail.Battle {
 // Approved numeric rules only; the node owns time, motion, HP and damage eligibility.
 public static class OrdinaryObstacleRules {
  public static CombatExactValue RawDamage(bool falling,int hp,int maximumHp,long speedMmPerSecond){
   Ratio(hp,maximumHp);
   if(speedMmPerSecond<0||speedMmPerSecond>26000)throw new ArgumentOutOfRangeException(nameof(speedMmPerSecond),"Approved obstacle profile supports 0..26000 mm/s.");
   return falling?CombatExactValue.Create(900L*hp,maximumHp):
    CombatExactValue.Create(checked(600L*hp*speedMmPerSecond*speedMmPerSecond),checked((long)maximumHp*20000*20000));
  }
  public static long CollisionSpeed(double currentSpeed,int hp,int maximumHp){
   Ratio(hp,maximumHp);
   if(double.IsNaN(currentSpeed)||double.IsInfinity(currentSpeed)||currentSpeed<0||currentSpeed>26000)throw new ArgumentOutOfRangeException(nameof(currentSpeed));
   return (long)Math.Round(Math.Max(Math.Min(14000,currentSpeed),currentSpeed*(1-.5*hp/maximumHp)));
  }
  public static double FallingHeight(double milliseconds,double initialHeight,double landedHeight,long fallStartMs,long downwardSpeedMmPerSecond){
   if(double.IsNaN(milliseconds)||double.IsInfinity(milliseconds)||milliseconds<0||initialHeight<landedHeight||landedHeight<0||fallStartMs<0||downwardSpeedMmPerSecond<=0)throw new ArgumentException("Original falling geometry required.");
   return Math.Max(landedHeight,initialHeight-Math.Max(0,milliseconds-fallStartMs)*downwardSpeedMmPerSecond/1000d);
  }
  public static (double X,double Y,double FlightSeconds) Aim(double dx,double muzzleY,Func<double,double> targetHeight,long shotSpeed,double lifetimeSeconds){
   if(targetHeight==null||double.IsNaN(dx)||double.IsInfinity(dx)||dx<0||double.IsNaN(muzzleY)||double.IsInfinity(muzzleY)||shotSpeed<=0||lifetimeSeconds<=0||double.IsInfinity(lifetimeSeconds)||double.IsNaN(lifetimeSeconds))throw new ArgumentException("Original shot trajectory required.");
   double lo=0,hi=lifetimeSeconds;
   for(int i=0;i<40;i++){
    double t=(lo+hi)/2,dy=targetHeight(t)-muzzleY;
    if(double.IsNaN(dy)||double.IsInfinity(dy))throw new ArgumentException("Invalid target trajectory.");
    if(Math.Sqrt(dx*dx+dy*dy)>shotSpeed*t)lo=t;else hi=t;
   }
   double y=targetHeight(hi)-muzzleY,length=Math.Sqrt(dx*dx+y*y);
   if(length<=0||double.IsNaN(length)||double.IsInfinity(length))throw new ArgumentException("Invalid launch direction.");
   return (dx/length*shotSpeed,y/length*shotSpeed,hi);
  }
  static void Ratio(int hp,int maximumHp){if(maximumHp<=0||hp<0||hp>maximumHp)throw new ArgumentOutOfRangeException(nameof(hp));}
 }
}
