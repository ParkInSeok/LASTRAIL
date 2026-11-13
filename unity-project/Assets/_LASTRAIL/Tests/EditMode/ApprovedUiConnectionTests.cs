using System;
using System.Linq;
using System.Reflection;
using LastRail.Presentation;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class ApprovedUiConnectionTests {
 [Test] public void ServerNodeKindAndIconSurviveDisplayProjection(){
  var sample=typeof(LastRail.Tests.EditMode.OperationMapStatusTests).GetMethod("Sample",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  var map=OperationMapStatus.Read(sample.ToString(),"run");Assert.AreEqual("COMBAT",map.Choices[0].EventKind);Assert.AreEqual("combat",map.Choices[0].IconId);Assert.AreEqual("e1",map.Choices[0].EdgeId);
 }
 [Test] public void DestinationArtworkPreservesSingleCommandAndBlocksRemovedAndHiddenRows(){
  var root=new GameObject("map-test");
  try{var panel=OrdinaryTravelPanel.Create(root.transform,TMP_Settings.defaultFontAsset);int calls=0;string edge=null;panel.Bind(e=>{calls++;edge=e;},null,null,null,null,null,null);
   panel.ShowChoices(new[]{("edge","Elite","ELITE")},true,"Choose");var first=panel.GetComponentsInChildren<Button>().Single(b=>b.name=="Destination0");
   var firstClick=first.onClick;var art=Resources.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");Assert.NotNull(art);Assert.AreSame(art.Elite,first.GetComponentInChildren<RawImage>().texture);first.onClick.Invoke();Assert.AreEqual(1,calls);Assert.AreEqual("edge",edge);
   panel.ShowChoices(new[]{("other","Unknown","MERCHANT")},true,"Choose");firstClick.Invoke();Assert.AreEqual(1,calls);
   var second=panel.GetComponentsInChildren<Button>().Single(b=>b.name=="Destination0");Assert.IsNull(second.GetComponentInChildren<RawImage>());panel.gameObject.SetActive(false);second.onClick.Invoke();Assert.AreEqual(1,calls);
   panel.gameObject.SetActive(true);second.onClick.Invoke();Assert.AreEqual(2,calls);Assert.AreEqual("other",edge);panel.Unbind();second.onClick.Invoke();Assert.AreEqual(2,calls);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void CurrentLocationUsesConfirmedNodeAndClearsOutsideChoices(){
  var sample=(Newtonsoft.Json.Linq.JObject)typeof(LastRail.Tests.EditMode.OperationMapStatusTests).GetMethod("Sample",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  var entry=OperationMapStatus.Read(sample.ToString(),"run");Assert.AreEqual("entry",entry.CurrentNodeId);Assert.IsNull(entry.CurrentNodeName);
  sample["view"]["currentNodeId"]="n1";sample["view"]["nodes"][0]["access"]="CURRENT";sample["view"]["nodes"][1]["access"]="DIRECT";sample["view"]["traversedEdgeIds"]=new Newtonsoft.Json.Linq.JArray("e1");sample["view"]["edges"][0]["traversed"]=true;
  var map=OperationMapStatus.Read(sample.ToString(),"run");Assert.AreEqual("First",map.CurrentNodeName);Assert.AreEqual("Future",map.Choices.Single().Name);
  sample["view"]["nodes"][0]["publicInfo"]["name"]="mutated";Assert.AreEqual("First",map.CurrentNodeName);
  var root=new GameObject("current-location-test");try{
   var panel=OrdinaryTravelPanel.Create(root.transform,TMP_Settings.defaultFontAsset);var choices=map.Choices.Select(c=>(c.EdgeId,c.Name,c.EventKind));
   panel.ShowChoices(choices,true,"Choose",map.CurrentNodeName);var location=panel.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="CurrentLocation");Assert.IsTrue(location.gameObject.activeSelf);StringAssert.EndsWith("First",location.text);Assert.AreSame(Resources.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110").CurrentLocation,location.GetComponentInChildren<RawImage>().texture);
   panel.ShowChoices(choices,false,"Pending",map.CurrentNodeName);StringAssert.EndsWith("First",location.text);
   panel.ShowTravel("Travel","Moving",10000,7000,13000,false,false);Assert.IsFalse(location.gameObject.activeSelf);
   panel.ShowChoices(choices,true,"Choose",map.CurrentNodeName);Assert.IsTrue(location.gameObject.activeSelf);panel.ShowUnavailable("Unavailable");Assert.IsFalse(location.gameObject.activeSelf);
   panel.ShowChoices(choices,true,"Entry",entry.CurrentNodeName);Assert.IsFalse(location.gameObject.activeSelf);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void NoticeUsesIndependentArtworkAndDismissesOnceAfterReopen(){
  var root=new GameObject("notice-test",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
  try{var page=LobbyDestinationPages.Create(root.transform,TMP_Settings.defaultFontAsset,()=>{});page.Open(LobbyPage.Formation,true);var show=typeof(LobbyDestinationPages).GetMethod("Notice",BindingFlags.NonPublic|BindingFlags.Instance);
   for(int i=0;i<2;i++){show.Invoke(page,new object[]{"Notice","Keep the existing callback"});typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(page,null);var close=page.GetComponentsInChildren<Button>().Single(b=>b.name=="Dismiss");
   var box=(RectTransform)close.transform.parent.Find("Dialog");var buttonRect=(RectTransform)close.transform;Assert.That(buttonRect.anchoredPosition.x,Is.GreaterThan(box.anchoredPosition.x));Assert.That(buttonRect.anchoredPosition.x+buttonRect.sizeDelta.x,Is.LessThan(box.anchoredPosition.x+box.sizeDelta.x));Assert.That(-buttonRect.anchoredPosition.y+buttonRect.sizeDelta.y,Is.LessThan(-box.anchoredPosition.y+box.sizeDelta.y));var frame=page.GetComponentsInChildren<Image>().Single(x=>x.name=="ApprovedDialogFrame");Assert.IsFalse(frame.fillCenter);Assert.IsFalse(frame.raycastTarget);Assert.NotNull(frame.sprite);Assert.IsTrue(page.GetComponentsInChildren<RawImage>().Any(x=>x.name=="ApprovedInfoMedal"));var dismiss=close.onClick;dismiss.Invoke();Assert.DoesNotThrow(()=>dismiss.Invoke());Assert.IsFalse(page.GetComponentsInChildren<Button>().Any(b=>b.name=="Dismiss"));}
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
