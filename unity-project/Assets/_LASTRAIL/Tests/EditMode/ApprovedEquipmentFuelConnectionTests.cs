using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Presentation;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ApprovedEquipmentFuelConnectionTests {
 [Test] public void FuelArtworkPreservesBalancesRefreshAndLandscapePosition(){
  var root=new GameObject("Fuel test",typeof(RectTransform));int refreshes=0;
  try{
   var view=DeparturePreparationView.Create(root.transform,TMP_Settings.defaultFontAsset,null,null,null,null,null,null,null,null,null,null,()=>refreshes++);
   var art=Resources.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");Assert.NotNull(art.Fuel);
   var icons=view.GetComponentsInChildren<RawImage>().Where(i=>i.name.StartsWith("CurrencyIcon")).OrderBy(i=>i.name).ToArray();Assert.AreEqual(3,icons.Length);
   Assert.AreSame(art.Fuel,icons[0].texture);Assert.AreEqual(new Rect(0,0,1,1),icons[0].uvRect);Assert.IsFalse(icons[0].raycastTarget);
   Assert.AreNotSame(art.Fuel,icons[1].texture);Assert.AreNotSame(art.Fuel,icons[2].texture);
   view.Render("Operation","I","",new[]{"80","2400","150"},"10",true,true,false,true,true,"Start");
   Assert.AreEqual("80",view.transform.Find("Balance0").GetComponent<TMP_Text>().text);Assert.AreEqual("10",view.transform.Find("FuelValue").GetComponent<TMP_Text>().text);
   view.transform.Find("DepartureBalance0").GetComponent<Button>().onClick.Invoke();Assert.AreEqual(1,refreshes);
   view.SetViewportHeight(900);var point=icons[0].rectTransform.anchoredPosition;view.SetViewportHeight(1080);view.SetViewportHeight(900);
   Assert.AreEqual(point,icons[0].rectTransform.anchoredPosition);Assert.AreEqual(new Vector2(1028,-33),point);
  }finally{Object.DestroyImmediate(root);}
 }
 [Test] public void EquipmentFramesFollowExistingRowsWithoutChangingTextOrCloseCallback(){
  var root=new GameObject("Equipment test",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
  try{
   var entry=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/PresentationSource/server-packets.json"))["packets"]["grown"]["presentations"];
   JObject Display(JObject p)=>EquipmentMultishotModel.Read(p,p,BDirectAttackInput.Digest(p),(string)p["accountId"],(string)p["runId"]).Render();
   var frame=new JObject{["version"]=EquipmentMultishotController.FrameVersion,["current"]=Display((JObject)entry["highest"]["current"]),["departure"]=Display((JObject)entry["one"]["departure"]),["stored"]=Display((JObject)entry["stored"])};
   var page=LobbyDestinationPages.Create(root.transform,TMP_Settings.defaultFontAsset,()=>{});page.Open(LobbyPage.Operators,true);page.PresentEquipmentMultishot(frame);
   var art=Resources.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");Assert.NotNull(art.ItemFrame);
   var borders=page.GetComponentsInChildren<Image>().Where(i=>i.name=="Border"&&i.sprite==art.ItemFrame).ToArray();Assert.AreEqual(7,borders.Length);
   foreach(var border in borders){Assert.IsFalse(border.fillCenter);Assert.IsFalse(border.raycastTarget);Assert.AreEqual(Image.Type.Sliced,border.type);}
   var texts=page.GetComponentsInChildren<TMP_Text>();Assert.AreEqual("추가 피해15%",texts.Single(t=>t.name=="departureCoefficient0").text);
   var fit=typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Instance);fit.Invoke(page,null);
   var row=(RectTransform)borders.Single(i=>i.transform.parent.name=="currentItemFrame0").transform.parent;var position=row.anchoredPosition;fit.Invoke(page,null);Assert.AreEqual(position,row.anchoredPosition);
   var slot=texts.Single(t=>t.name=="currentSlot0").rectTransform;Assert.Less(row.anchoredPosition.x,slot.anchoredPosition.x);Assert.Greater(row.anchoredPosition.y,slot.anchoredPosition.y);
   int closes=0;page.BindEquipmentMultishotClose(()=>closes++);var close=page.GetComponentsInChildren<Button>().Single(b=>b.name=="CloseEquipmentMultishot").onClick;close.Invoke();close.Invoke();Assert.AreEqual(1,closes);
   Assert.IsFalse(page.GetComponentsInChildren<Image>().Any(i=>i.name=="Border"&&i.sprite==art.ItemFrame));
  }finally{Object.DestroyImmediate(root);}
 }
}
