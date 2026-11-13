using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
public class CombatOperatorRigTests {
 [TestCase("sera","forearm_weapon",1170,650,"controlGrip")]
 [TestCase("sera","far_forearm",403,742,"supportGrip")]
 [TestCase("mira","far_forearm",354,719,"supportGrip")]
 public void InterpolatedAuthoredPosesKeepHandOnWeaponAndFeetFixed(string id,string part,int x,int y,string grip){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-rig-exact-v03.json"));data.Remove("skillAngles");data.Remove("skillPoseSamples");data["sockets"]["diagnosticHand"]=new JObject{["part"]=part,["sourcePoint"]=new JArray(x,y)};
  var texture=new Texture2D(2,2);var go=new GameObject("grip-contract",typeof(RectTransform));go.transform.localScale=Vector3.one*(155/(float)data["rigHeight"]*.86f);
  try{var rig=go.AddComponent<CombatMultipartRig>();rig.Configure(texture,data);Assert.IsFalse(rig.HasSkillPose,"Missing skill source must not fall back to Kyle's guard");var lower=go.transform.Find("lower");var original=lower.position;
   for(int i=0;i<=100;i++){rig.SetExtension(i/100f);Assert.IsTrue(rig.TrySocket("diagnosticHand",out var hand,out _));Assert.IsTrue(rig.TrySocket(grip,out var target,out _));Assert.Less(Vector3.Distance(hand,target),1f,"Actual-size grip error at "+i);Assert.AreEqual(original,lower.position);}
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
}
