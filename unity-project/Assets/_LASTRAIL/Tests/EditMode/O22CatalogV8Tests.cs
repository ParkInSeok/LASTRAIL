using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class O22CatalogV8Tests {
 const string V8="augment.utility-definitions.v8.20261020";
 static JObject Catalog()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/utility-definitions-v8-source.json"));
 static string Hash(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
 static void Pin(JObject p){p.Remove("packetHash");p["packetHash"]=Hash(p);}
 // Actual exported v8 definitions, synthetic acquisition envelope/authentication.
 public static JObject Packet(){var p=CombatO22OriginalTests.Packet();var catalog=Catalog();var d=(JObject)p["definition"];var s=(JObject)d["selectionSource"];s["catalogVersion"]=V8;s["selection"]["catalogVersion"]=V8;
  d["definitionCatalog"]=new JObject{["version"]=V8,["definitionHash"]=catalog["definitionHash"].DeepClone()};d["selectedDefinition"]=catalog["definitions"].Single(x=>(string)x["id"]=="O22").DeepClone();foreach(var key in new[]{"composition","lifecycle","resistance","eligibility"})d[key]=catalog[key].DeepClone();
  Repin(p);return p;
 }
 static void Repin(JObject p){var d=(JObject)p["definition"];var s=(JObject)d["selectionSource"];Pin(s);Pin(d);p["applicationId"]="o22:"+Hash(new JObject{["accountId"]=p["accountId"].DeepClone(),["runId"]=p["runId"].DeepClone(),["eventId"]=p["grantEventId"].DeepClone(),["environment"]=p["environment"].DeepClone(),["expectedStoreRevision"]=s["storeRevision"].DeepClone(),["expectedCatalogVersion"]=s["catalogVersion"].DeepClone(),["expectedStartSnapshotHash"]=s["startSnapshotHash"].DeepClone()});}
 static CombatO22ApplicationOriginal Read(JObject p)=>CombatO22ApplicationOriginal.Read(p.ToString(),"account","run","grant","TEST",new string('a',64));
 [Test]public void ExactExportedV8AndHistoricalV7BothReadWithoutRewriting(){var p=Packet();var a=Read(p);Assert.AreEqual((string)p["applicationId"],a.ApplicationId);Assert.IsFalse(a.Applied);Assert.IsNotNull(Read(CombatO22OriginalTests.Packet()));}
 [Test]public void V8AllowsOneCandidateAndShowsActualPinnedCopy(){var p=Packet();var selection=(JObject)p["definition"]["selectionSource"]["selection"];selection["slots"]=new JArray(selection["slots"][0].DeepClone());Repin(p);Assert.AreEqual(1,AugmentSelectionSnapshot.Parse(selection.ToString()).Slots.Count);var catalog=Catalog();foreach(string id in new[]{"O22","O23"})Assert.NotNull(HealingAugmentPresentation.FromPinned(V8,(JObject)catalog["definitions"].Single(x=>(string)x["id"]==id),(JObject)catalog["composition"]));}
 [TestCase("damageToHealBps")][TestCase("recoveryKind")][TestCase("blockedTriggers")]
 public void RepinnedChangedV8SemanticsAreRejected(string key){var p=Packet();p["definition"]["selectedDefinition"]["rules"][key]=key=="damageToHealBps"?new JValue(800):new JValue("changed");Repin(p);Assert.Throws<FormatException>(()=>Read(p));}
 [Test]public void UnknownAndMixedCatalogsAreRejected(){var p=Packet();p["definition"]["selectionSource"]["selection"]["catalogVersion"]="unknown";Repin(p);Assert.Throws<FormatException>(()=>Read(p));p=Packet();p["definition"]["definitionCatalog"]["version"]="unknown";Repin(p);Assert.Throws<FormatException>(()=>Read(p));}
 [Test]public void V8SelectCannotCompleteWithoutApplicationOriginal(){var p=Packet();var after=(JObject)p["definition"]["selectionSource"]["selection"];var before=(JObject)after.DeepClone();before["grantRevision"]=0;before["state"]="SELECTING";before["selectedCandidateId"]=null;var scope=new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");var command=AugmentSelectionCommand.Create(scope,"SELECT",0,null,AugmentSelectionSnapshot.Parse(before.ToString()));var result=new JObject{["outcome"]="SELECTED",["snapshot"]=after,["approval"]=null};var method=typeof(AugmentSelectionCommand).GetMethod("ValidateResult",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>method.Invoke(command,new object[]{result.ToString(),scope}));Assert.IsInstanceOf<FormatException>(error.InnerException);}
}
