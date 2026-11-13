using UnityEngine;
namespace LastRail.Battle {
// Presentation world only. Actor IDs, availability, hits and target choice remain model-owned.
public static class CombatPursuitLayout {
 public const float Period=960, RoadSpeed=64, RidgeSpeed=24;
 public static float Phase(float seconds,float speed)=>Mathf.Repeat(seconds*speed,Period);
 public static float RoadY(float screenX,float seconds,int lane){
  float world=screenX+Phase(seconds,RoadSpeed);
  return (lane==0?186:154)+screenX*.045f+2*Mathf.Sin(world*Mathf.PI*2/Period);
 }
 public static Vector2 Contact(int slot,float progress,float seconds){
  int lane=Mathf.Abs(slot)%2,rank=Mathf.Abs(slot)/2;
  float end=lane==0?555:535;end+=rank*105;
  float x=Mathf.Lerp(790+rank*105,end,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress)));
  return new Vector2(x,CombatExteriorLayout.ViewTop+RoadY(x,seconds,lane));
 }
 public static float Depth(float localY)=>Mathf.InverseLerp(120,240,localY);
}
}
