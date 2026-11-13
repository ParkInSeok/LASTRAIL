#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03PresentationLoaderTests {
 static JObject Manifest()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/self-check-1Ys9pJ/presentation-manifest.json"));
 [Test] public void ActualR6AssetDatabaseTypesFilesRolesAndDependenciesLoad(){var config=Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(Manifest()));Assert.NotNull(config);Assert.AreEqual(160,config.Frames.Length);}
 [TestCase("asset")][TestCase("meta")][TestCase("source")][TestCase("dependency")]
 public void ActualLoaderRejectsShapeValidTampering(string mode){var m=Manifest();switch(mode){case "asset":m["bindings"][0]["assetRawSha256"]=new string('0',64);break;case "meta":m["bindings"][0]["metaRawSha256"]=new string('0',64);break;case "source":m["sourceFiles"][0]["rawSha256"]=new string('0',64);break;case "dependency":foreach(var b in (JArray)m["bindings"])if(((JArray)b["dependencies"]).Count>0){b["dependencies"]=new JArray();break;}break;}var parser=new Ce03PresentationManifest(m);Assert.Throws<FormatException>(()=>Ce03PresentationManifestLoader.Validate(parser));}
}
#endif
