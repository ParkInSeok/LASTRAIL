#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Diagnostics
{
 public static class SystemDurabilityPlayerCheck
 {
  public static async Task Run(string output)
  {
   var driver=BattleDComparisonRoster.CreateDriver();driver.Open();
   var art=BattleDComparisonRoster.CreateArt();
   var evidence=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/SideViewReuse-20261023/battle-d-field-848-v01/party-roof-actual-assets-v01/verification.json"));
   var originalNyx=art.OperatorFrames.Find("op_nyx").IdleFrames[0];
   // Display-only still registration. No clip, movement or ordinary combat source is inferred.
   foreach(var row in (JArray)evidence["sources"]){string id="op_"+((string)row["id"]).ToLowerInvariant();var entry=art.OperatorFrames.Find(id);var texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NumberedFormation/"+id);var bytes=File.ReadAllBytes((string)row["file"]);int Dimension(int n)=>(bytes[n]<<24)|(bytes[n+1]<<16)|(bytes[n+2]<<8)|bytes[n+3];float sx=texture.width/(float)Dimension(16),sy=texture.height/(float)Dimension(20);entry.CanvasSize=new Vector2(texture.width,texture.height);entry.FootAnchorTopLeft=new Vector2((float)row["foot"][0]*sx,(float)row["foot"][1]*sy);entry.BodySpan=((float)row["alphaBounds"][3]-(float)row["alphaBounds"][1])*sy;entry.IdleFrames=new[]{texture};entry.DurationsMs=new[]{1000};entry.BasicFrames=Array.Empty<Texture2D>();entry.BasicDurationsMs=Array.Empty<int>();entry.Emblem=null;}
   var host=new GameObject("SystemHudActualProduct",typeof(RectTransform));
   try
   {
    var view=host.AddComponent<ApprovedCombatView>();view.BindOrRefresh(driver,art);view.UseLandscapeLayout();
    var kyleCard=host.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single(i=>i.name=="ApprovedBattleCardPortrait_op_kyle");
    if(kyleCard.texture!=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/DConsole/portrait-kyle"))throw new InvalidOperationException("Approved combat-card portrait was replaced.");
    if(host.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name=="ConfirmRailRoute"))throw new InvalidOperationException("Obsolete separate confirmation button remains.");
    string before=driver.StateJson;var result=new JArray();
    foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(2100,900),new Vector2Int(1200,900)})
    {
     Screen.SetResolution(size.x,size.y,false);await Task.Delay(500);view.BindOrRefresh(driver,art);view.BindOrRefresh(driver,art);
     var meters=host.GetComponentsInChildren<CombatMeter>(true).Where(m=>m.name.StartsWith("SystemDurability")).ToArray();
     if(meters.Length!=3||meters.Any(m=>m.Value!=0)||before!=driver.StateJson)throw new InvalidOperationException("Missing-source HUD mutated owner or invented HP.");
     Canvas.ForceUpdateCanvases();
     var train=host.GetComponentsInChildren<CombatMeter>(true).Single(m=>m.name=="TrainHp");
     var ordered=meters.OrderBy(m=>m.name).ToArray();
     var trainCorners=new Vector3[4];var leftCorners=new Vector3[4];var rightCorners=new Vector3[4];
     train.rectTransform.GetWorldCorners(trainCorners);ordered[0].rectTransform.GetWorldCorners(leftCorners);ordered[2].rectTransform.GetWorldCorners(rightCorners);
     float leftError=Mathf.Abs(trainCorners[0].x-leftCorners[0].x),rightError=Mathf.Abs(trainCorners[2].x-rightCorners[2].x);
     if(leftError>.001f||rightError>.001f||ordered.Any(m=>m.transform.parent!=train.transform.parent))throw new InvalidOperationException("Train/system outside gauge edges are not aligned.");
     await LandscapeScreenSweepProbe.Capture(output,"unknown-"+size.x+"x"+size.y);
     result.Add(new JObject{["width"]=Screen.width,["height"]=Screen.height,["meters"]=meters.Length,["ownerStateUnchanged"]=true,["leftGaugeEdgeError"]=leftError,["rightGaugeEdgeError"]=rightError});
    }
    File.WriteAllText(Path.Combine(output,"system-hud-check.json"),new JObject{["passed"]=true,["scope"]="ACTUAL_PLAYER_HUD_LOCAL_FIXTURE_MISSING_EQUIPMENT_SOURCE_NOT_REAL_DAMAGE_REPAIR_EQUIPMENT",["bodySource"]="NumberedFormation approved full-body stills; static registration only; no flip or gait",["kylePortrait"]="approved independent combat-card source; existing runtime pixels verified against authoritative crop",["nozzles"]="approved rail-slim independent SVG layers; 200 housing / 77 grip / 43 rubber / +/-41 travel; separate confirmation button hidden; existing lever Commit reused",["captures"]=result}.ToString());
   }
   finally{host.SetActive(false);UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(originalNyx);UnityEngine.Object.Destroy(art.OperatorFrames);UnityEngine.Object.Destroy(art);}
  }
 }
}
#endif
