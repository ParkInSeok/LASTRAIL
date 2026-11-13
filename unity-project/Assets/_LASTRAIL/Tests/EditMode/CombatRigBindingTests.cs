using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatRigBindingTests {
 [Test] public void GenericRigFollowsActualActorStateKeepsSoleAndPlacesHpAboveParts(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var reference=CombatPresentationConnectionTests.Fresh();var go=new GameObject("rig-binding",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var actor=(RectTransform)go.transform.Find("ApprovedBattle/BattleActors/op_kyle");var support=actor.TransformPoint(new Vector3(75,-160));
   var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/rig-connected-union-v01.json"));var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/parts-source-v01.png");view.SetCaptureOperatorRig("op_kyle",texture,data);view.EnableCaptureRigMotion();view.Refresh();
   var rig=actor.GetComponentInChildren<CombatMultipartRig>();Assert.Less(Vector3.Distance(support,rig.transform.TransformPoint(new Vector3(220,-475))),.001f);Assert.IsFalse(actor.Find("Unit").gameObject.activeSelf);
   float maximum=0;for(int i=0;i<90;i++){driver.Advance(500000,1);reference.Advance(500000,1);view.Refresh();maximum=Mathf.Max(maximum,rig.Extension);Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);Assert.Less(Vector3.Distance(support,rig.transform.TransformPoint(new Vector3(220,-475))),.001f);}
   Assert.Greater(maximum,.5f);driver.SetPaused(true);view.Refresh();float before=rig.Extension;driver.Advance(10000000,20);view.Refresh();Assert.AreEqual(before,rig.Extension);
   var health=actor.Find("Health") as RectTransform;float bottom=health.anchoredPosition.y-health.rect.height;var corners=new Vector3[4];foreach(var part in rig.GetComponentsInChildren<CombatClippedPart>()){part.rectTransform.GetWorldCorners(corners);foreach(var point in corners)Assert.Greater(bottom,actor.InverseTransformPoint(point).y);}
  }finally{Object.DestroyImmediate(go);}
 }
}
