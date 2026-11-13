using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using LastRail.Presentation.Battle;

namespace LastRail.Battle {
 public sealed partial class ApprovedCombatView {
  ReinaBarrageVideoView reinaBarrageView;
  JObject reinaDisplayState;
  void BindReinaBarrage(){
   reinaDisplayState=driver.ReadDisplayState();
   reinaBarrageView=gameObject.AddComponent<ReinaBarrageVideoView>();
   reinaBarrageView.Bind(driver,root,font,assets.SupportFire,ProjectReinaImpact,()=>presentation.TryReserveFullCinematic(),()=>reinaDisplayState);
  }
  Vector2? ProjectReinaImpact(JObject impact){
   if(ordinaryRoof==null||ordinaryRoofSurface==null)return null;
   var frame=impact["frame"];var local=impact["centerMm"] as JArray;
   if(!(frame?["originMm"] is JArray origin)||!(frame["basis"] is JArray basis)||local==null||local.Count!=3||origin.Count!=3||basis.Count!=3)
    throw new ArgumentException("Original impact frame and local anchor required.");
   var world=new double[3];for(int i=0;i<3;i++){world[i]=(double)origin[i];for(int j=0;j<3;j++)world[i]+=(double)basis[j][i]*(double)local[j];}
   var rail=reinaDisplayState?["ordinaryRail"];if(rail?["frontArcMm"]==null)return null;
   // The visual buffer follows the same moving train frame; local committed targeting stays fixed.
   if(impact["frontArcMm"]!=null)world[0]+=(double)rail["frontArcMm"]-(double)impact["frontArcMm"];
   var surface=ordinaryRoof.ProjectRoofRelativeToFront(world[0]-(double)rail["frontArcMm"],world[2]);if(!surface.HasValue)return null;var uv=surface.Value;
   var rect=ordinaryRoofSurface.rectTransform;var point=(Vector2)root.InverseTransformPoint(rect.TransformPoint(new Vector2(uv.x*rect.rect.width,-(1-uv.y)*rect.rect.height)));
   return point-root.rect.min;
  }
 }
}
