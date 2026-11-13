#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace LastRail.Battle {
 // Disposable component QA only: never a product bootstrap or an authenticated START source.
 public sealed class OrdinaryFiveSeraPlayerProbe:MonoBehaviour {
  [SerializeField] TextAsset source;
  public void Initialize(TextAsset value)=>source=value;
  void Start(){
   string root=null;
   try{
    root=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_SERA_QA")??throw new ArgumentException("Explicit QA output required"));
    if(!root.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D QA path required");
    string mode=Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_SERA_MODE");if(mode!="WRITE"&&mode!="READ")throw new ArgumentException("Explicit mode required");
    if(mode=="WRITE"&&Directory.Exists(root))throw new InvalidOperationException("Fresh writer directory required");Directory.CreateDirectory(root);
    var fixture=JObject.Parse(source.text);bool supplied=(string)fixture["classification"]=="SYNTHETIC_ACCOUNT_INTERNAL_START_PRODUCTION_INITIALIZATION_HTTP_NOT_PRODUCT_ADMISSION";
    if(!supplied&&(string)fixture["scope"]!="SYNTHETIC_START_NATIVE_SERA_COMPONENT_ONLY")throw new ArgumentException("Wrong QA fixture scope");
    var start=(JObject)fixture[supplied?"originalStart":"start"];var input=OrdinaryFiveStartInput.Read(start,(string)fixture[supplied?"accountId":"account"],(string)fixture[supplied?"runId":"run"]);
    OrdinaryFiveSeraFactory factory;
    if(supplied)factory=OrdinaryFiveSeraFactory.FromInitialization(input,fixture["initializationResponse"].ToString(Newtonsoft.Json.Formatting.None),500000);
    else {var stats=fixture["stats"];var snapshot=OrdinaryFiveCoreStatSnapshot.Read((JObject)stats["packet"],(JObject)stats["state"],(string)stats["packet"]["packetHash"],input.AccountId,input.RunId,input.OriginHash);factory=new OrdinaryFiveSeraFactory(input,snapshot,500000);}
    var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=input.RunId,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
    var original=new PendingAccountAction(input.AccountId,"departure-start",(string)start["operationPreviewId"],0,"native-sera-qa-original",new JObject{["operationPreviewId"]=start["operationPreviewId"].DeepClone()}.ToString(),response.ToString(Newtonsoft.Json.Formatting.None));
    int pid=System.Diagnostics.Process.GetCurrentProcess().Id;
    if(mode=="READ"&&(int)JObject.Parse(File.ReadAllText(root+"/writer.json"))["pid"]==pid)throw new InvalidOperationException("New process required");
    var effects=fixture["nativeResolvedEffects"] as JArray;
    if(effects!=null&&(string)fixture["nativeEffectClassification"]!="SYNTHETIC_TRUSTED_EFFECTS_NOT_ACTUAL_ENEMY_OR_RAIL_IMPACT")throw new ArgumentException("Explicit native effect QA classification required");
    var cycles=new JArray();var before=Memory();for(int cycle=0;cycle<3;cycle++){var result=Cycle(root,mode,cycle,original,factory,effects);result["memoryAfterComponentCycle"]=Memory();cycles.Add(result);}
    File.WriteAllText(root+(mode=="WRITE"?"/writer.json":"/reader.json"),new JObject{["scope"]="NATIVE_SERA_COMPONENT_ONLY_SYNTHETIC_START_NO_ADMISSION",["result"]="PASSED",["pid"]=pid,["mode"]=mode,["apiConfigured"]=false,["encryptedStoreTested"]=false,["fullCombatTested"]=false,["forcedGc"]=false,["memoryBefore"]=before,["cycles"]=cycles}.ToString());
    Application.Quit(0);
   }catch(Exception error){if(root!=null&&Directory.Exists(root))File.WriteAllText(root+"/failure.json",new JObject{["result"]="FAILED",["error"]=error.ToString()}.ToString());Debug.LogException(error);Application.Quit(1);}
  }
  static JObject Memory(){using var process=System.Diagnostics.Process.GetCurrentProcess();process.Refresh();return new JObject{["unityAllocated"]=Profiler.GetTotalAllocatedMemoryLong(),["managedUsed"]=Profiler.GetMonoUsedSizeLong(),["graphicsEstimate"]=Profiler.GetAllocatedMemoryForGraphicsDriver(),["privateBytes"]=process.PrivateMemorySize64,["workingSet"]=process.WorkingSet64,["unityObjects"]=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length};}
  static JObject Cycle(string root,string mode,int cycle,PendingAccountAction original,OrdinaryFiveSeraFactory factory,JArray effects){
   string prefix=root+"/cycle-"+cycle;var checkpoint=new OrdinaryFiveSeraCheckpoint(CombatCheckpointStore.CreateExplicitLocalQaFile(prefix+"-checkpoint.json"),original,factory,new AppBuildSnapshot("ordinary-sera-qa-20261026",1));
   if(mode=="WRITE")checkpoint.Save(factory.Create(),0);
   var driver=CombatSceneDriver.ForOrdinarySeraStart(checkpoint,()=>true);driver.Open();
   if(mode=="WRITE"){
    if(effects!=null)foreach(JObject effect in effects)driver.ScheduleResolvedOrdinaryEffect(effect);
    driver.ScheduleResolvedSeraCommand(new JObject{["command"]=new JObject{["kind"]="CAST",["eventId"]="cast-one",["actionId"]="heal-one",["atMs"]=0,["sourceId"]="op_sera",["abilityId"]="op_sera.active"},["sequence"]=1,["effectSequence"]=2});driver.Advance(2000000,20);
    var baseline=BasicCombatSimulation.RestoreLocalState(driver.StateJson,factory.Create);baseline.Advance(2000000,20);File.WriteAllText(prefix+"-expected.json",baseline.CaptureLocalState());
   }else{
    if(driver.Tick!=4)throw new InvalidOperationException("Pending heal was not restored");driver.Advance(2000000,20);
    if(driver.StateJson!=File.ReadAllText(prefix+"-expected.json"))throw new InvalidOperationException("Restored execution differs from uninterrupted execution");
   }
   var state=JObject.Parse(driver.StateJson);var receipts=(JArray)state["commands"]["receipts"];
   if(mode=="READ"&&factory.Original.Train?.SourceVersion!=OrdinaryTrainSource.InitialHealthVersion&&(receipts.Count!=2||(string)receipts.Last["receipt"]["status"]!="APPLIED"))throw new InvalidOperationException("Expected exactly one completed heal");
   if(factory.Original.Train?.SourceVersion==OrdinaryTrainSource.InitialHealthVersion&&((JArray)state["actors"]).Count!=6)throw new InvalidOperationException("Train state missing from the shared checkpoint");
   if(effects!=null){var train=((JArray)state["actors"]).Single(a=>(string)a["id"]==factory.Original.Train.TrainId);var kyle=((JArray)state["actors"]).Single(a=>(string)a["id"]=="op_kyle");if((int)train["vitals"]["hp"]!=(mode=="WRITE"?5000:4988)||(int)kyle["vitals"]["hp"]!=1350)throw new InvalidOperationException("Six-target resolved damage differs");}
   return new JObject{["cycle"]=cycle,["tick"]=driver.Tick,["stateHash"]=BDirectAttackInput.Digest(state),["receiptCount"]=receipts.Count,["nativeEffects"]=effects!=null?"SYNTHETIC_TRUSTED_EFFECTS_NOT_ACTUAL_ENEMY_OR_RAIL_IMPACT":"NONE"};
  }
 }
}
#endif
