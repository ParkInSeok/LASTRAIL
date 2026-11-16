using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
 public sealed class NumberedFormationTests
 {
  static readonly string[] Ids={"op_kyle","op_reina","op_eve","op_sera","op_nyx"};
  sealed class Journal:IAccountActionJournal {public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>true;public bool Remove(PendingAccountAction a)=>true;}
  sealed class Wire:IRunApiHttpTransport
  {
   public int Reads;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Reads++;var json=JObject.Parse(FormationContractTests.S03Json());var original=(JObject)json["operators"][0];json["operators"]=new JArray(Ids.Select(id=>{var row=(JObject)original.DeepClone();row["operatorId"]=id;return row;}));json["trainingCoreSlots"]=new JArray();return Task.FromResult(RunApiHttpResponse.Success(r.EndpointPath=="/v1/account/state"?200:404,r.EndpointPath=="/v1/account/state"?json.ToString():"{}"));}
  }
  static Button Find(GameObject root,string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
  static string Label(GameObject root,string name)=>Find(root,name).GetComponentInChildren<TMP_Text>().text;
  [Test] public async Task ExistingDraftDrivesBothPreviewsAndOneClickSwapsWithoutExtraRead()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await state.RefreshAccountAsync();
   for(int i=0;i<5;i++)Assert.IsTrue(state.Formation.Select(i,Ids[i]));
   var root=new GameObject("NumberedFormationQA",typeof(RectTransform));
   try{
    var pages=LobbyDestinationPages.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>{});pages.BindAccount(state);pages.Open(LobbyPage.Formation);
    Assert.AreEqual(5,root.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("FormationSlot")));
    pages.RefreshAccountView(state);pages.RefreshAccountView(state);int reads=wire.Reads;
    Find(root,"AssignOwned1").onClick.Invoke();Find(root,"FormationSlot4").onClick.Invoke();
    Assert.AreEqual("op_kyle",state.Formation.Selection[4]);Assert.AreEqual("op_nyx",state.Formation.Selection[0]);Assert.AreEqual(5,state.Formation.Selection.Distinct().Count());
    Assert.AreSame(FormationPlanView.Portrait("op_kyle"),Find(root,"FormationSlot4").transform.Find("Portrait").GetComponent<RawImage>().texture);Assert.AreEqual(reads,wire.Reads);
    var stale=Find(root,"FormationSlot0").onClick;var before=state.Formation.Selection;pages.Close();stale.Invoke();Assert.AreEqual(before,state.Formation.Selection);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public void DepartureDisplaysConfirmedMembersInBothDirections()
  {
   var root=new GameObject("ConfirmedFormationQA",typeof(RectTransform));
   try{
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");Action noop=()=>{};
    var view=DeparturePreparationView.Create(root.transform,font,null,noop,noop,noop,noop,noop,noop,noop,noop,noop,noop);
    for(int i=0;i<5;i++)view.Member(i,Ids[i],null,false);
    Assert.IsTrue(root.GetComponentsInChildren<Button>().Any(b=>b.name=="LeftPosition0"),"Confirmed departure must display both previews.");
    StringAssert.Contains("카일",Label(root,"LeftPosition0"));StringAssert.Contains("카일",Label(root,"RightPosition0"));Assert.IsFalse(Find(root,"RightPosition0").interactable);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public async Task EmptyPlacementAndPreviewRebindingDoNotDuplicateCommands()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await state.RefreshAccountAsync();var root=new GameObject("BindingQA",typeof(RectTransform));
   try{var view=NumberedFormationView.Create(root.transform,null);int calls=0;Action<int> assign=slot=>{calls++;state.Formation.Select(slot,"op_kyle");};view.Bind(assign);view.Bind(assign);view.Render(state.Formation.Selection,-1,true);Find(root,"RightPosition4").onClick.Invoke();Assert.AreEqual(1,calls);Assert.AreEqual("op_kyle",state.Formation.Selection[4]);Assert.IsTrue(state.Formation.Selection.Take(4).All(x=>x==null));view.Unbind();Find(root,"LeftPosition0").onClick.Invoke();Assert.AreEqual(1,calls);view.Bind(assign);view.gameObject.SetActive(false);FindInactive(root,"RightPosition0").onClick.Invoke();Assert.AreEqual(1,calls);}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static Button FindInactive(GameObject root,string name)=>root.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
 }
}


