using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class EquipmentInventoryTests {
 static TMP_FontAsset Font=>UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
 static JObject Item(string id,bool equipped=false)=>new JObject{["instance_id"]=id,["source_item_id"]="source-"+id,["definition_id"]="definition",["definition_version"]="v1",["category_id"]="category",["set_tag"]="SET01",["star"]=3,["locked"]=1,["location_kind"]=equipped?"EQUIPPED":"STORED",["wearer_kind"]=equipped?(JToken)"operator":JValue.CreateNull(),["wearer_id"]=equipped?(JToken)"op_mira":JValue.CreateNull(),["slot_index"]=equipped?(JToken)0:JValue.CreateNull(),["revision"]=1};
 static JObject Source(int revision=1)=>new JObject{["revision"]=revision,["capacitySlots"]=30,["usedSlots"]=1,["items"]=new JArray(Item("a"),Item("b",true))};
 static EquipmentInventoryModel Model(JObject source)=>new EquipmentInventoryModel("account","run",source);
 [Test] public void SourceIsIsolatedAndOnlyStoredItemsConsumeCapacity(){var source=Source();var m=Model(source);source["items"][0]["star"]=1;Assert.AreEqual(3,m.Items[0].Star);Assert.AreEqual(1,m.Used);Assert.AreEqual(2,m.Items.Count);}
 [TestCase("star")][TestCase("locked")][TestCase("duplicate")][TestCase("capacity")][TestCase("placement")][TestCase("missing")][TestCase("wrong-count")][TestCase("duplicate-slot")]
 public void InvalidSourcesAreRejected(string fault){var s=Source();switch(fault){case "star":s["items"][0]["star"]=6;break;case "locked":s["items"][0]["locked"]=2;break;case "duplicate":s["items"][1]["instance_id"]="a";break;case "capacity":s["capacitySlots"]=0;break;case "placement":s["items"][0]["slot_index"]=0;break;case "missing":((JObject)s["items"][0]).Remove("wearer_id");break;case "wrong-count":s["usedSlots"]=2;break;case "duplicate-slot":var clone=Item("c",true);((JArray)s["items"]).Add(clone);break;}Assert.Throws<ArgumentException>(()=>Model(s));}
 [Test] public void SelectionUpdatesAndRevisionRulesPreserveCurrentState(){var c=new EquipmentInventoryController("account","run");int changes=0;c.Changed+=()=>changes++;c.Apply(Model(Source(2)));c.Select("a");c.Select("a");Assert.AreEqual(2,changes);c.Apply(Model(Source(1)));Assert.AreEqual(2,c.Snapshot.Revision);var conflict=Source(2);conflict["items"][0]["locked"]=0;Assert.Throws<ArgumentException>(()=>c.Apply(Model(conflict)));Assert.AreEqual("a",c.SelectedId);Assert.Throws<ArgumentException>(()=>c.Apply(new EquipmentInventoryModel("foreign","run",Source(3))));var next=Source(3);((JArray)next["items"]).RemoveAt(0);next["usedSlots"]=0;c.Apply(Model(next));Assert.IsNull(c.Selected);Assert.IsNull(c.SelectedId);c.Select("b");c.Back();Assert.IsNull(c.SelectedId);}
 [Test] public void EquivalentReorderedRowsDoNotPublishAgain(){var c=new EquipmentInventoryController("account","run");c.Apply(Model(Source()));int changes=0;c.Changed+=()=>changes++;var s=Source();s["items"]=new JArray(((JArray)s["items"]).Reverse());c.Apply(Model(s));Assert.AreEqual(0,changes);}
 [Test] public void ViewSelectionCloseAndRebindOwnOnlyTheirSubscriptions(){
  var root=new GameObject("InventoryTest",typeof(RectTransform),typeof(Canvas));var c=new EquipmentInventoryController("account","run");c.Apply(Model(Source()));int external=0,closed=0;c.Changed+=()=>external++;
  try{var v=EquipmentInventoryView.Open(root.transform,Font,c,()=>closed++);v.Bind(c);v.transform.Find("List/Rows/Row0/Select").GetComponent<Button>().onClick.Invoke();Assert.AreEqual("a",c.SelectedId);Assert.AreEqual(1,external);StringAssert.Contains("★★★",v.transform.Find("Detail/SelectedItem").GetComponent<TMP_Text>().text);v.RequestBack();Assert.IsNull(c.SelectedId);v.gameObject.SetActive(false);c.Select("b");v.gameObject.SetActive(true);Assert.NotNull(v.transform.Find("Detail/SelectedItem"));v.Unbind();v.gameObject.SetActive(false);v.gameObject.SetActive(true);var before=external;v.transform.Find("List/Rows/Row0/Select").GetComponent<Button>().onClick.Invoke();Assert.AreEqual(before,external);v.Bind(c);v.RequestBack();v.RequestBack();Assert.AreEqual(1,closed);c.Select("a");Assert.AreEqual("a",c.SelectedId);}
  finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void UnavailableIsDifferentFromConfirmedEmptyInventory(){var root=new GameObject("InventoryEmptyTest",typeof(RectTransform),typeof(Canvas));try{var c=new EquipmentInventoryController("account","run");var v=EquipmentInventoryView.Open(root.transform,Font,c,null);Assert.NotNull(v.transform.Find("Detail/Unavailable"));var s=Source();s["items"]=new JArray();s["usedSlots"]=0;c.Apply(Model(s));StringAssert.Contains("보유 장비가 없습니다",v.transform.Find("Detail/EmptyOrSelect").GetComponent<TMP_Text>().text);}finally{UnityEngine.Object.DestroyImmediate(root);}}
 [Test] public void RenderLandscapeFixture(){
  const string dir="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/equipment-inventory";System.IO.Directory.CreateDirectory(dir);
  var root=new GameObject("InventoryRender",typeof(RectTransform),typeof(Canvas));var cameraObject=new GameObject("InventoryCamera",typeof(Camera));var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
  try{var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var c=new EquipmentInventoryController("account","run");c.Apply(Model(Source()));EquipmentInventoryView.Open(root.transform,Font,c,null);c.Select("a");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();System.IO.File.WriteAllBytes(dir+"/list-detail.png",pixels.EncodeToPNG());
  }finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);}
 }
}
