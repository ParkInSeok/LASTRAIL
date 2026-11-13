using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class Ce03PlayerSourcePinTests {
 [Test] public void CompiledSourcePinRejectsChangedAndOmittedCodeDespiteRehashedBundle(){
  var type=typeof(Ce03CommandContract).Assembly.GetType("LastRail.Diagnostics.Ce03PlayerSourcePin");Assert.IsNotNull(type);
  var row=new JObject{["path"]="Assets/code.cs",["sha256"]=Ce03CommandContract.Sha("original"),["content"]="original"};
  var pin=new JObject{["version"]="ce03.compiled-sources.v1",["buildManifestPath"]="QA/test/player-build.json",["files"]=new JArray(new JObject{["path"]=row["path"].DeepClone(),["sha256"]=row["sha256"].DeepClone()})};
  var bundle=new JObject{["schemaVersion"]="ce03-source-bundle-v1",["files"]=new JArray(row,new JObject{["path"]="QA/test/player-build.json",["content"]="build",["sha256"]=Ce03CommandContract.Sha("build")})};
  var instance=Activator.CreateInstance(type,new object[]{pin});var verify=type.GetMethod("Verify");verify.Invoke(instance,new object[]{bundle});
  row["content"]="changed";row["sha256"]=Ce03CommandContract.Sha("changed");
  Assert.Throws<TargetInvocationException>(()=>verify.Invoke(instance,new object[]{bundle}));
  ((JArray)bundle["files"]).RemoveAt(0);Assert.Throws<TargetInvocationException>(()=>verify.Invoke(instance,new object[]{bundle}));
 }
}
