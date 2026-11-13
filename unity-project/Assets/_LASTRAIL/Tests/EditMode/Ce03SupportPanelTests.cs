using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Ce03SupportPanelTests {
 sealed class Wire:IRunApiHttpTransport {
  public int Saves,Reads;public JObject Body;public TaskCompletionSource<bool> SaveGate;long currentRevision;JArray savedSlots=new JArray();
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){
   string[] ids={"support_fire","barrier","emergency_repair","ce03.role.suppressive_barrage","ce03.role.medical_support","ce03.role.smoke","ce03.role.weakness_scan","ce03.role.firepower_boost","ce03.role.electronic_disruption","ce03.role.emergency_protection"};
   var selection=new JObject{["rulesVersion"]="ce03-support-selection-v1",["catalogVersion"]="ce03-local-support-roles-v1",["revision"]=currentRevision,["slots"]=savedSlots.DeepClone()};
   if(r.Method=="GET"){Reads++;return Task.FromResult(RunApiHttpResponse.Success(200,new JObject{["selection"]=selection,["catalog"]=new JObject{["scope"]="LOCAL_SELECTION_ONLY",["entries"]=new JArray(ids.Select((id,i)=>new JObject{["skillId"]=id,["executionAvailable"]=i<3}))}}.ToString()));}
   Saves++;Body=JObject.Parse(r.BodyJson);return Complete();
   async Task<RunApiHttpResponse> Complete(){if(SaveGate!=null)await SaveGate.Task;currentRevision++;savedSlots=(JArray)Body["slots"].DeepClone();selection["revision"]=currentRevision;selection["slots"]=savedSlots.DeepClone();return RunApiHttpResponse.Success(200,selection.ToString());}
  }
 }
 [Test] public void ActualPanelRequiresExplicitThreeThenPersistsTheirOrder(){
  var w=new Wire();string persisted=null;var c=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),s=>persisted=s);var root=new GameObject("PanelTest",typeof(RectTransform),typeof(Canvas));
  try{var panel=Ce03SupportSelectionPanel.Open(root.transform,c,()=>{});var buttons=panel.GetComponentsInChildren<Button>();var save=buttons.Single(b=>b.name=="Save");Assert.IsFalse(save.interactable);Assert.AreEqual(7,panel.GetComponentsInChildren<TMP_Text>().Count(t=>t.text.Contains("準備中")||t.text.Contains("준비 중")));
   foreach(int i in new[]{2,0,1})buttons.Single(b=>b.name=="Choice"+i).onClick.Invoke();Assert.IsTrue(save.interactable);save.onClick.Invoke();Assert.AreEqual(1,w.Saves);CollectionAssert.AreEqual(new[]{"emergency_repair","support_fire","barrier"},((JArray)w.Body["slots"]).Select(s=>(string)s["skillId"]));Assert.IsFalse(c.HasPending);Assert.IsNotNull(persisted);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void NativeSlotsReorderRemoveAndDecorationsNeverInterceptInput(){
  var w=new Wire();var c=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),s=>{});var root=new GameObject("SlotTest",typeof(RectTransform),typeof(Canvas));
  try{var panel=Ce03SupportSelectionPanel.Open(root.transform,c,()=>{});Button B(string name)=>panel.GetComponentsInChildren<Button>().Single(b=>b.name==name);
   foreach(int i in new[]{0,1,2})B("Choice"+i).onClick.Invoke();B("MoveLeft2").onClick.Invoke();B("RemoveSlot0").onClick.Invoke();Assert.IsFalse(B("Save").interactable);B("Choice0").onClick.Invoke();B("Save").onClick.Invoke();
   CollectionAssert.AreEqual(new[]{"emergency_repair","barrier","support_fire"},((JArray)w.Body["slots"]).Select(s=>(string)s["skillId"]));Assert.AreEqual(1,w.Saves);
   foreach(string name in new[]{"SupportRoom","SupportHost"}){var image=panel.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);Assert.IsNotNull(image.texture);Assert.IsFalse(image.raycastTarget);}
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator ReenableWaitsForInflightSaveBeforeRefreshingRevision(){yield return new UnityEngine.TestTools.EnterPlayMode();var task=ReenableSaveCore();while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();yield return new UnityEngine.TestTools.ExitPlayMode();}
 async Task ReenableSaveCore(){
  var w=new Wire{SaveGate=new TaskCompletionSource<bool>()};var c=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),s=>{});var root=new GameObject("PendingSlotTest",typeof(RectTransform),typeof(Canvas));
  try{var panel=Ce03SupportSelectionPanel.Open(root.transform,c,()=>{});Button B(string name)=>panel.GetComponentsInChildren<Button>().Single(b=>b.name==name);foreach(int i in new[]{0,1,2})B("Choice"+i).onClick.Invoke();B("Save").onClick.Invoke();panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);Assert.AreEqual(1,w.Reads);Assert.IsFalse(B("Save").interactable);
   w.SaveGate.SetResult(true);for(int i=0;i<100&&(!B("Save").interactable||w.Reads<2);i++)await Task.Delay(1);Assert.AreEqual(2,w.Reads);Assert.IsTrue(B("Save").interactable);B("MoveLeft2").onClick.Invoke();B("Save").onClick.Invoke();Assert.AreEqual(1,(long)w.Body["expectedRevision"]);Assert.AreEqual(2,w.Saves);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
