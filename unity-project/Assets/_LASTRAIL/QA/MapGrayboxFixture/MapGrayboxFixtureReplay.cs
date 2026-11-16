using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Qa {
 public static class MapGrayboxFixtureReplay {
  public static void Equal(JToken actual,JToken expected,string context){
   bool Number(JToken t)=>t?.Type==JTokenType.Integer||t?.Type==JTokenType.Float;
   if(Number(actual)&&Number(expected)){if((decimal)actual!=(decimal)expected)throw new InvalidOperationException(context+": numeric mismatch "+actual+" != "+expected);return;}
   if(actual is JObject a&&expected is JObject b){if(a.Count!=b.Count)throw new InvalidOperationException(context+": fields differ");foreach(var p in b.Properties())Equal(a[p.Name],p.Value,context+"."+p.Name);return;}
   if(actual is JArray x&&expected is JArray y){if(x.Count!=y.Count)throw new InvalidOperationException(context+": array length");for(int i=0;i<x.Count;i++)Equal(x[i],y[i],context+"["+i+"]");return;}
   if(!JToken.DeepEquals(actual,expected))throw new InvalidOperationException(context+": value mismatch "+actual+" != "+expected);
  }
  public static JObject RunAll(string output=null){
   var root=MapGrayboxFixtureGeometry.Root;var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"Trace01/manifest.json")));
   string[] names={"model.cjs","mechanics.cjs","export-traces.cjs"};string[] hashes={"d50e760480626bc37f1ef9151ac232d7115038b6b2e39c03b2378483103b3970","05e9bf2d0d66ed83541d6facc88c20d77e63e49ed191e8f216578d1444349012","6e2a35af5be3271f8a0ff7fbc8d0832ba5f1771486afbd7c83c7a67713c62141"};
   for(int i=0;i<names.Length;i++)if(MapGrayboxFixtureGeometry.Hash(Path.Combine(root,names[i]))!=hashes[i]||(string)manifest["sources"][names[i]]!=hashes[i])throw new InvalidOperationException("Pinned lab source changed");
   if(output!=null){output=Path.GetFullPath(output);if(!output.StartsWith(Path.Combine(root,"Unity")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D Unity QA output required");Directory.CreateDirectory(output);}
   var geometry=new MapGrayboxFixtureGeometry();var cases=new JArray();int comparisons=0,totalSteps=0;
   foreach(var file in manifest["files"]){string name=(string)file["file"];if(Path.GetFileName(name)!=name)throw new FormatException();var path=Path.Combine(root,"Trace01",name);if(MapGrayboxFixtureGeometry.Hash(path)!=(string)file["sha256"])throw new InvalidOperationException("Trace bytes changed");var trace=JObject.Parse(File.ReadAllText(path));var config=(JObject)trace["config"];var model=Create(geometry,config);var commands=(JArray)trace["commands"];var receipts=new JArray();var changes=new JArray();
    for(int attempt=0;attempt<(int)trace["stepAttempts"];attempt++){
     foreach(var cmd in commands.Where(c=>(int)c[0]==attempt)){int tick=model.Tick;var result=model.Command((string)cmd[1],cmd.Count()>2?cmd[2]:null);var receipt=new JObject{["attempt"]=attempt,["command"]=(string)cmd[1],["value"]=cmd.Count()>2?cmd[2].DeepClone():JValue.CreateNull(),["result"]=result,["beforeTick"]=tick,["after"]=model.Capture()};Equal(receipt,trace["receipts"][receipts.Count],name+" command "+receipts.Count);receipts.Add(receipt);comparisons++;}
     model.Step();totalSteps++;foreach(var expected in trace["changes"].Where(c=>(int)c["attempt"]==attempt)){var actual=new JObject{["attempt"]=attempt,["state"]=model.Capture()};Equal(actual,expected,name+" change "+attempt);changes.Add(actual);comparisons++;}
    }
    Equal(model.Capture(),trace["final"],name+" final");comparisons++;var entry=new JObject{["id"]=(string)trace["id"],["attempts"]=(int)trace["stepAttempts"],["receipts"]=receipts,["changes"]=changes,["final"]=model.Capture()};cases.Add(new JObject{["id"]=(string)trace["id"],["result"]="MATCH",["final"]=model.Capture()});if(output!=null)File.WriteAllText(Path.Combine(output,name),entry.ToString());
   }
   var server=JObject.Parse(File.ReadAllText(Path.Combine(root,"Server/fixtures.json")));int serverFrames=0;var serverCases=new JArray();
   foreach(var fixture in server["cases"]){var model=Create(geometry,fixture["config"] as JObject);string id=(string)fixture["id"];var observed=JObject.Parse(File.ReadAllText(Path.Combine(root,"Server/traces",id+".json")));var commands=(JArray)fixture["commands"];
    for(int frame=1;frame<=(int)fixture["frames"];frame++){foreach(var command in commands.Where(c=>(int)c["frame"]==frame).OrderBy(c=>(int)c["sequence"]))model.Command((string)command["type"],command["value"]);model.Step();Equal(model.Capture()["state"],observed["frames"][frame-1]["state"],id+" frame "+frame);serverFrames++;}
    serverCases.Add(new JObject{["id"]=id,["frames"]=(int)fixture["frames"],["result"]="MATCH"});
   }
   var report=new JObject{["scope"]=MapGrayboxFixtureRun.Scope,["result"]="PASS",["geometryModules"]=geometry.Ids.Length,["planCases"]=cases,["planStepAttempts"]=totalSteps,["planSemanticComparisons"]=comparisons,["serverCases"]=serverCases,["serverFrameStatesCompared"]=serverFrames,["boundaryStatus"]="UNRESOLVED: onset441 prior-tail sweep yields1; endpoint-start oracle yields0",["productionReady"]=false};
   if(output!=null)File.WriteAllText(Path.Combine(output,"verification.json"),report.ToString());return report;
  }
  static MapGrayboxFixtureRun Create(MapGrayboxFixtureGeometry geometry,JObject config)=>new MapGrayboxFixtureRun(geometry,(int?)config?["fallDelay"]??320,(int?)config?["fallDuration"]??5,(int?)config?["trainHp"]??1000);
 }
}
