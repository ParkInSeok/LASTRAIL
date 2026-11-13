using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using LastRail.Battle;
public class CombatRigDownTests {
 [TestCase("kyle")][TestCase("reina")][TestCase("eve")][TestCase("sera")][TestCase("mira")]
 public void DownBeginsAtCurrentCastPoseKeepsLowerAndRecoversToCurrentAttack(string id){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-rig-exact-v04.json"));var go=new GameObject("down-test",typeof(RectTransform));var texture=new Texture2D(2,2);
  try{var rig=go.AddComponent<CombatMultipartRig>();rig.Configure(texture,data);rig.SetExtension(.7f);rig.SetGuard(.6f);var torso=go.transform.Find("lower/torso")??go.transform.Find("torso");Assert.IsNotNull(torso);var before=torso.localRotation;var lower=go.transform.Find("lower");var support=lower.position;rig.BeginDown();rig.SetExtension(0);rig.SetGuard(0);rig.SetDown(0,false);Assert.Less(Quaternion.Angle(before,torso.localRotation),.001f,"Down first frame cannot snap to idle");
   for(int i=0;i<=100;i++){rig.SetDown(i/100f,false);Assert.AreEqual(support,lower.position);}Assert.AreEqual(1,rig.Down);
   rig.SetExtension(.4f);rig.SetGuard(0);var attack=torso.localRotation;rig.SetDown(0,true);Assert.Less(Quaternion.Angle(attack,torso.localRotation),.001f);Assert.AreEqual(0,rig.Down);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
}
