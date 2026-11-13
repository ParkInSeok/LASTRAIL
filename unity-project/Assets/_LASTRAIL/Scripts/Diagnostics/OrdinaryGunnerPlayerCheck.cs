#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 public static class OrdinaryGunnerPlayerCheck {
  static void Need(bool valid,string reason){if(!valid)throw new InvalidOperationException(reason);}
  public static async Task Run(string output,string sourcePath){
   Need(Path.GetFullPath(sourcePath).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase),"D source required");
   var raw=File.ReadAllText(sourcePath);var source=JObject.Parse(raw);bool nodeSource=(string)source["scope"]=="ACTUAL_NODE_FACTORY_SYNTHETIC_ARRIVAL_NOT_LIVE_ENTRY";Need(nodeSource||(string)source["scope"]=="ACTUAL_ORDINARY_FACTORY_AND_ASSET_BINDING_SYNTHETIC_ARRIVAL_NOT_PRODUCT_ADMISSION","Explicit ordinary diagnostic source required");
   var packet=(JObject)source["binding"];var route=BRouteBinding.Read(packet.ToString(),(string)packet["packetHash"],(string)packet["accountId"],(string)packet["runId"]);
   var request=BRouteCommand.FromOriginal(route,(JObject)source["command"]);var arrival=BRouteCommandResult.Read(200,source["response"].ToString(),request);
   var original=OrdinaryFiveStartInput.Read((JObject)source["originalStart"],route.AccountId,route.RunId);
   var factory=nodeSource?OrdinaryFiveCombatFactory.FromNodeStart(original,arrival.NodeStart,arrival.Scaling,arrival.NodeStart.ReadActions(original).StepTimeSpanTicks):OrdinaryFiveCombatFactory.FromInitialization(original,source["initializationResponse"].ToString(),500000,arrival.Scaling);
   int writes=0;var driver=nodeSource?CombatSceneDriver.ForOrdinaryNode(factory,()=>true):new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>{writes++;return revision+1;},()=>true);driver.Open();
   var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");Need(binding!=null&&binding.ManifestHash==(string)source["manifestHash"],"Original ordinary asset binding differs");
   var host=new GameObject("ActualOrdinaryGunnerPlayerHost");var scene=host.AddComponent<CombatPlayerScene>();
   using var controller=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>true);Exception failure=null;
   try{
    string before=driver.StateJson;int prior=writes;controller.Present(scene,EventSystem.current,()=>{},r=>failure=new InvalidOperationException("Unexpected terminal"),e=>failure=e);
    await Task.Delay(150);CombatScenePlayerTrial.Capture(scene.SceneCamera,Path.Combine(output,"ordinary-gunner-preparation.png"));
    Need(failure==null&&scene.IsOpen&&!scene.IsRunning,"Ordinary preparation was not kept separate from execution");
    Need(before==driver.StateJson&&writes==prior&&!(bool)driver.HudState["fullCombatReady"],"Preparation unexpectedly executed combat");
    var rows=new JArray();foreach(var id in arrival.Scaling.IssuedGunnerIds){var actor=((JArray)driver.HudState["actors"]).Single(a=>(string)a["id"]==id);var art=scene.View.GetComponentsInChildren<RawImage>(true).Single(a=>a.name=="Unit"&&a.transform.parent.name==id);var fallback=art.transform.parent.Find("MissingFigure");Need(art.texture==null&&!art.enabled&&fallback!=null&&fallback.gameObject.activeSelf,"Unapproved ordinary gunner art selected");rows.Add(new JObject{["id"]=id,["hp"]=actor["hp"],["physicalInstanceId"]=actor["physicalInstanceId"],["textureName"]=JValue.CreateNull(),["artStatus"]="APPROVED_SOURCE_UNAVAILABLE",["fallbackActive"]=true,["rectWidth"]=art.rectTransform.rect.width,["rectHeight"]=art.rectTransform.rect.height,["pivot"]=new JArray(art.rectTransform.pivot.x,art.rectTransform.pivot.y)});}
    Need(rows.Count==2,"Original two gunners required");
    var displayed=driver.HudState;controller.ClosePresentation();Need(!scene.IsOpen,"Scene did not close");
    File.WriteAllText(Path.Combine(output,"ordinary-gunner-player.json"),new JObject{["passed"]=true,["scope"]="ACTUAL_ORDINARY_FACTORY_SCENE_AND_ASSETS_SYNTHETIC_ARRIVAL_NOT_LIVE_ENTRY",["factorySource"]=nodeSource?"NODE_START":"INITIALIZATION",["trainHp"]=displayed["trainHp"],["operatorHp"]=new JArray(displayed["actors"].Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]).Select(a=>new JObject{["id"]=a["id"],["hp"]=a["hp"]})),["sourceSha256"]=Ce03CommandContract.Sha(raw),["manifestHash"]=binding.ManifestHash,["actors"]=rows,["stateUnchanged"]=true,["displayWrites"]=writes-prior,["closed"]=true,["fullCombatReady"]=false,["physicalHitVerified"]=false,["liveApiVerified"]=false}.ToString());
   }finally{controller.Dispose();UnityEngine.Object.Destroy(host);}
   if(nodeSource)await CheckNodeLifetime(output,original,factory,binding);
  }
  static async Task CheckNodeLifetime(string output,OrdinaryFiveStartInput original,OrdinaryFiveCombatFactory factory,CombatPlayerAssetBinding binding){
   var host=new GameObject("NodeLifetimePlayerHost");var scene=host.AddComponent<CombatPlayerScene>();var samples=new JArray();int callbacks=0;
   Action<CombatSceneResult> lateComplete=null;Action<Exception> lateFail=null;
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   try{
    for(int i=0;i<3;i++){
     var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
     using(var owner=new OrdinaryPlayerBattleController(original,driver,binding,()=>true)){
      owner.Present(scene,EventSystem.current,()=>callbacks++,r=>callbacks++,e=>callbacks++);await Task.Delay(150);
      driver.SetPaused(true);string paused=driver.StateJson;await Task.Delay(50);Need(paused==driver.StateJson&&owner.IsCurrent,"Pause retired or advanced node");
      lateComplete?.Invoke(null);lateFail?.Invoke(new Exception("Injected previous presentation callback"));Need(callbacks==0&&paused==driver.StateJson,"Late callback changed current node");
      lateComplete=(Action<CombatSceneResult>)typeof(CombatPlayerScene).GetField("completed",flags).GetValue(scene);
      lateFail=(Action<Exception>)typeof(CombatPlayerScene).GetField("failed",flags).GetValue(scene);
      scene.Close();Need(driver.IsRetired&&driver.StateJson==null&&!owner.IsCurrent,"Scene close retained node execution");
      bool rejected=false;try{driver.SetSpeed(3);}catch(InvalidOperationException){rejected=true;}Need(rejected,"Late node input accepted");
     }
     await Task.Delay(50);Need(host.transform.childCount==0&&!scene.IsOpen,"Owned presentation remained");
     samples.Add(new JObject{["iteration"]=i+1,["ownedChildren"]=host.transform.childCount,["managedBytesAfterCollection"]=GC.GetTotalMemory(true),["unityAllocatedBytes"]=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()});
    }
    lateComplete(null);lateFail(new Exception("Injected final presentation callback"));Need(callbacks==0,"Retired callback published");
    File.WriteAllText(Path.Combine(output,"node-lifetime-player.json"),new JObject{["passed"]=true,["scope"]="ACTUAL_PLAYER_PRESENTATION_LIFETIME_SYNTHETIC_NODE_NOT_COMBAT_OR_SERVER_REENTRY",["iterations"]=samples,["lateCallbacksPublished"]=callbacks,["nativeProjectileLifetimeVerified"]=false,["liveApiVerified"]=false}.ToString());
   }finally{UnityEngine.Object.Destroy(host);}
  }
 }
}
#endif
