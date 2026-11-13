using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 readonly List<CombatCanyonLayer> canyonLayers=new List<CombatCanyonLayer>();
 void AddCanyonLayers(bool foreground){
  if(foreground||assets.PursuitRoad!=null&&assets.PursuitLayout!=null)return;
  if(assets.DistantTerrain==null||assets.DistantTerrainLayout==null)return;
  var rows=JObject.Parse(assets.DistantTerrainLayout.text)["layers"] as JArray;if(rows==null||rows.Count!=3)throw new System.ArgumentException("Expected three supplied canyon layers");
  foreach(JObject row in rows){string id=(string)row["id"];if((id=="foreground_left")!=foreground)continue;var layer=Rect("Canyon_"+id,exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatCanyonLayer>();layer.Configure(assets.DistantTerrain,row);canyonLayers.Add(layer);}
 }
 void RefreshCanyonVisibility(bool selected){foreach(var layer in canyonLayers)layer.gameObject.SetActive(selected);}
}
}
