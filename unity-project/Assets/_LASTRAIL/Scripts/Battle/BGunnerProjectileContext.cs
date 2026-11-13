using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal interface IBGunnerProjectileContext : IBBoardingActors {
  string GunnerTarget(string source);
  bool EnemyBasicTargetEligible(string source,string target);
  bool TryGeometry(string source,string target,out JObject geometry);
  JObject CaptureGunnerAttack(string source);
  bool HasSpatial {get;}
 }
}
