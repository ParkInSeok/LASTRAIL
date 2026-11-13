#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Presentation.AugmentSelection {
 // Synthetic gesture QA on the real View. Never attached to a server grant,
 // acquisition store, reroll balance or combat owner.
 internal static class AugmentChoiceLocalProbe {
  public static async Task RunAsync(Transform parent,TMP_FontAsset font,string directory,Func<string,CancellationToken,Task> capture,CancellationToken cancellation){
   var view=AugmentSelectionView.Create(parent,font);var rect=(RectTransform)view.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(-450,-100);
   try{
    int selected=0;void Bind()=>view.Bind(_=>selected++,(_,__)=>{},()=>{});Bind();
    var value=JObject.Parse("{runId:'LOCAL_INPUT_ONLY',eventId:'LOCAL_INPUT_ONLY',stageId:'LOCAL_INPUT_ONLY',catalogVersion:'LOCAL_INPUT_ONLY',grantRevision:0,state:'SELECTING',slots:[{slotIndex:0,candidateId:'입력 시험 A',freeRemaining:3,extraRemaining:1},{slotIndex:1,candidateId:'입력 시험 B',freeRemaining:3,extraRemaining:1},{slotIndex:2,candidateId:'입력 시험 C',freeRemaining:3,extraRemaining:1}],selectedCandidateId:null}");
    void Render(bool busy=false)=>view.Render(AugmentSelectionSnapshot.Parse(value.ToString()),busy,false,"합성 입력 시험 · 서버 선택/비용 처리 없음",(_,id)=>new AugmentCardPresentation(id,"입력 시험",id,null,string.Join("\n",Enumerable.Repeat(id+" · 상세 스크롤 시험",35))));
    Button Find(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
    var pointer=new PointerEventData(EventSystem.current??throw new InvalidOperationException("Actual EventSystem required")){button=PointerEventData.InputButton.Left,pointerId=42};
    void Down(Button b)=>ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerDownHandler);
    void Up(Button b){ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
    void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException("Synthetic input probe: "+reason);}
    Render();Find("Details0").onClick.Invoke();var scroll=view.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.3f;
    var confirm=Find("DetailSelect");Down(confirm);value["grantRevision"]=1;value["slots"][0]["candidateId"]="교체된 입력 시험 A";Render();Up(confirm);Need(selected==0,"old press selected new candidate");Need(scroll.verticalNormalizedPosition>.99f,"changed detail did not reset");
    await capture("input-candidate-changed.png",cancellation);
    scroll.verticalNormalizedPosition=.3f;value["grantRevision"]=2;value["slots"][1]["candidateId"]="교체된 입력 시험 B";Render();Need(Math.Abs(scroll.verticalNormalizedPosition-.3f)<.01f,"other slot reset detail");
    Render(true);confirm.onClick.Invoke();Need(selected==0&&scroll.gameObject.activeInHierarchy,"busy detail mutation or dismissal");Render();
    Down(confirm);Up(confirm);Need(selected==1,"fresh gesture not accepted exactly once");
    var button=Find("Select0");Down(button);view.Unbind();Bind();Render();Up(button);Need(selected==1,"rebind kept old press");
    view.Unbind();button.onClick.Invoke();Need(selected==1,"unbound callback invoked");
    File.WriteAllText(Path.Combine(directory,"input-probe.json"),new JObject{["classification"]="ACTUAL_PLAYER_VIEW_SYNTHETIC_EVENTS_ONLY",["physicalPointerInput"]=false,["serverRerollExecuted"]=false,["heldPointerReplacementSelections"]=0,["freshGestureSelections"]=1,["changedDetailReset"]=true,["otherDetailScrollRetained"]=true,["busyDetailRetained"]=true,["rebindAndUnbindExtraSelections"]=0,["keyboardGamepadVerified"]=false}.ToString());
   }finally{view.Unbind();view.gameObject.SetActive(false);UnityEngine.Object.Destroy(view.gameObject);}
  }
 }
}
#endif
