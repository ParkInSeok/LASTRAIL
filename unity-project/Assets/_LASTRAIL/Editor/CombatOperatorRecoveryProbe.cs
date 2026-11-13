#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class CombatOperatorRecoveryProbe {
 static JObject draft;
 static BasicCombatSimulation Fresh(){var m=BasicCombatSimulation.FromPreparedProfileForLocalTest(draft,(JObject)draft["ownedSelection"],"presentation-local-qa",1);m.ConfigureLocalLeaderCharge(4);return m;}
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-probeOutput");if(!Application.isBatchMode||at<0||File.Exists(args[at+1]))throw new Exception("Fresh explicit output required");
  draft=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));draft["ownedSelection"]["accountId"]="presentation-local-qa";
  var raw=JObject.Parse(Fresh().CaptureLocalState());foreach(var actor in (JArray)raw["actors"]){string id=(string)actor["id"];if(id.StartsWith("op_"))actor["vitals"]["hp"]=1;if(id.StartsWith("op_")||id=="train_a"||id.StartsWith("device"))actor["ready"]=20000;}
  var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var result=new JArray();var previous=model.ReadHudState();
  for(int i=0;i<1200&&model.Outcome==BasicCombatOutcome.Running;i++){
   model.Advance(500000,1);var current=model.ReadHudState();foreach(var actor in ((JArray)current["actors"]).Where(a=>((string)a["id"]).StartsWith("op_"))){var before=((JArray)previous["actors"]).Single(a=>(string)a["id"]==(string)actor["id"]);if(((int)before["hp"]==0)!=((int)actor["hp"]==0))result.Add(new JObject{["id"]=actor["id"],["tick"]=current["tick"],["hp"]=actor["hp"],["recoverAt"]=actor["recoverAt"]});}previous=current;
  }
  File.WriteAllText(args[at+1],new JObject{["scope"]="offline initial hp1 / allied ready20000; actual unchanged impacts/recovery thereafter",["transitions"]=result,["finalTick"]=model.Tick}.ToString());EditorApplication.Exit(0);
 }
}
#endif
