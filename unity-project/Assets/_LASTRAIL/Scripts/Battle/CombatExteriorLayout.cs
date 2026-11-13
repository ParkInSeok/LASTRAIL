using UnityEngine;
namespace LastRail.Battle {
// Local presentation coordinates only. This is not gameplay distance or routing.
public static class CombatExteriorLayout {
 public const float ViewTop=66,ViewHeight=273,TrainScale=.496f;
 public static readonly Vector2 TrainTopLeft=new Vector2(18,193.5f);
 public static float EntryBorn(bool restored,float now,float available)=>restored?available:Mathf.Max(now,available);
 static readonly Vector3[] Road0={new Vector3(716f,43f,0.0f),new Vector3(683f,51f,0.084038f),new Vector3(658f,61f,0.150678f),new Vector3(637f,73f,0.210538f),new Vector3(619f,88f,0.268527f),new Vector3(599f,100f,0.326252f),new Vector3(580f,111f,0.380588f),new Vector3(587f,121f,0.410798f),new Vector3(616f,132f,0.487561f),new Vector3(651f,143f,0.57836f),new Vector3(679f,155f,0.653754f),new Vector3(697f,169f,0.710191f),new Vector3(692f,184f,0.749323f),new Vector3(674f,198f,0.80576f),new Vector3(650f,212f,0.874526f),new Vector3(626f,222f,0.938874f),new Vector3(603f,231f,1.0f)};
 static readonly Vector3[] Road1={new Vector3(712f,40f,0.0f),new Vector3(680f,48f,0.202887f),new Vector3(655f,58f,0.368506f),new Vector3(634f,70f,0.517277f),new Vector3(616f,85f,0.661398f),new Vector3(596f,97f,0.804861f),new Vector3(578f,108f,0.934615f),new Vector3(585f,116f,1.0f)};
 public static Vector2 ApproachGround(int slot,float progress){
  var road=Mathf.Abs(slot)%2==0?Road0:Road1;
  // Extra ranks trail on the same road instead of shifting off its surface.
  float rankSpacing=Mathf.Abs(slot)%2==0?.16f:.50f;
  float p=Mathf.Clamp01(progress)*Mathf.Max(.2f,1-(Mathf.Abs(slot)/2)*rankSpacing);
  for(int i=1;i<road.Length;i++)if(p<=road[i].z||i==road.Length-1){var q=Vector3.Lerp(road[i-1],road[i],Mathf.InverseLerp(road[i-1].z,road[i].z,p));return new Vector2(q.x,q.y+ViewTop);}
  return new Vector2(road[0].x,road[0].y+ViewTop);
 }
 public static float ActorScale(int slot,float progress,string visual=null)=>Mathf.Lerp(.32f,.58f-(Mathf.Abs(slot)%2)*.08f,Mathf.Clamp01(progress))*(visual==null?1:visual.Contains("railbike")?.62f:.80f);
 public static float WheelBottom(string visual)=>113f*(visual.Contains("railbike")?1008f:982f)/1024f;
}
}
