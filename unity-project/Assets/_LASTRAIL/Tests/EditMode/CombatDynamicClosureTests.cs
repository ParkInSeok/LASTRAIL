using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatDynamicClosureTests {
 [Test] public void LiveThirtyThreeResourcesMatchClosedTypedManifest(){Ce03DynamicResourceClosure.Validate();}
 [TestCase("missing")][TestCase("hash")][TestCase("path")][TestCase("type")]
 public void IncompleteOrMutatedDynamicClosureIsRejected(string mutation){
  string path=Ce03DynamicResourceClosure.ManifestPath,original=File.ReadAllText(path);try{
   var root=JObject.Parse(original);var rows=(JArray)root["rows"];
   if(mutation=="missing")rows.RemoveAt(0);else if(mutation=="hash")rows[0]["rawSha256"]=new string('0',64);else if(mutation=="path")rows[0]["path"]="../outside.wav";else rows[0]["type"]="Texture2D";
   File.WriteAllText(path,root.ToString());Assert.Throws<FormatException>(()=>Ce03DynamicResourceClosure.Validate());
  }finally{File.WriteAllText(path,original);}
 }
}
