#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics
{
 // Real Player UI and owners; in-memory response fixture, never production admission or movement QA.
 public static class NumberedFormationPlayerCheck
 {
  static readonly string[] ids={"op_kyle","op_reina","op_eve","op_sera","op_nyx"};
  sealed class Journal:IAccountActionJournal{PendingAccountAction pending;public PendingAccountAction Read(string a)=>pending;public bool Save(PendingAccountAction a){pending=a;return true;}public bool Remove(PendingAccountAction a){pending=null;return true;}}
  sealed class Wire:IRunApiHttpTransport
  {
   public readonly List<string> Calls=new List<string>();long sequence=0,revision=1,formationRevision=1;JArray saved=FormationWrite.Operators(ids),active=FormationWrite.Operators(ids);
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Calls.Add(r.Method+" "+r.EndpointPath);JObject result;
    if(r.EndpointPath=="/v1/account/state")result=Account();
    else if(r.Method=="POST"&&r.EndpointPath.StartsWith("/v1/formation/presets/")){
     var body=JObject.Parse(r.BodyJson);bool save=r.EndpointPath.EndsWith("/save");string preset=r.EndpointPath.Split('/')[4];long before=sequence++;
     if(save){saved=(JArray)body["operators"].DeepClone();revision=(long)body["expectedRevision"]+1;}else{active=(JArray)saved.DeepClone();formationRevision++;}
     var patch=new JObject{["kind"]="PATCH",["beforeMutationSequence"]=before,["mutationSequence"]=sequence,["coverage"]=new JArray(save?"FORMATION_PRESETS":"ACTIVE_FORMATION")};
     if(save)patch["preset"]=Preset(preset,(string)body["name"]);else patch["formation"]=new JObject{["activePresetId"]=preset,["activePresetRevision"]=revision,["revision"]=formationRevision,["operators"]=active.DeepClone()};
     result=new JObject{["accepted"]=true,["receiptId"]="qa_receipt_"+sequence,["kind"]=save?"SAVE":"ACTIVATE",["presetId"]=preset,["presetRevision"]=revision,["refreshRequired"]=false,["contractVersion"]=FormationWrite.CompleteVersion,["owners"]=new JObject{["account"]=patch},["invalidates"]=save?new JArray():new JArray("ULTIMATE_SELECTION","DEPARTURE_PREVIEW")};
     if(!save){result["formationRevision"]=formationRevision;result["formationChanged"]=true;result["operators"]=active.DeepClone();}
    }else return Task.FromResult(RunApiHttpResponse.Success(404,"{}"));
    return Task.FromResult(RunApiHttpResponse.Success(200,result.ToString()));
   }
   JObject Preset(string id="qa_formation",string name="번호 배치 검수")=>new JObject{["id"]=id,["name"]=name,["revision"]=revision,["operators"]=saved.DeepClone(),["definitionJson"]=new JObject{["schemaVersion"]=1,["operators"]=saved.DeepClone()}.ToString(),["updatedAt"]="2026-11-07T00:00:00.000Z"};
   JObject Account()=>new JObject{["accepted"]=true,["accountId"]="account_test",["serverTime"]="2026-11-07T00:00:00.000Z",["mutationSequence"]=sequence,["currencies"]=new JObject{["CREDITS"]=0,["MACHINE_PARTS"]=0,["TACTICAL_DATA"]=0,["SPECIAL_CORE"]=0,["OPERATOR_EXP"]=0},["items"]=new JArray(),["fuel"]=null,["train"]=null,["carriages"]=new JArray(),["turrets"]=new JArray(),["unlocks"]=new JArray(),["trainingCoreSlots"]=new JArray(),["operators"]=new JArray(ids.Select(id=>new JObject{["operatorId"]=id,["skillTraining"]=1,["promotion"]=1,["revision"]=1,["acquiredAt"]="2026-11-07T00:00:00.000Z"})),["formation"]=new JObject{["activationSupported"]=true,["activePresetId"]="qa_formation",["activePresetRevision"]=revision,["revision"]=formationRevision,["operators"]=active.DeepClone(),["updatedAt"]="2026-11-07T00:00:00.000Z",["presets"]=new JArray(Preset())}};
  }
  static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  public static async Task Run(string output,TMP_FontAsset font)
  {
   var wire=new Wire();var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddHours(2));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await state.EnsureAccountAsync();Need(state.AccountStatus==AccountStateReadStatus.Ready,"QA account read");state.Formation.Load(state.Account.Presets.Single());
   var root=new GameObject("NumberedFormationPlayer",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=1;
   var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
   var deck=LobbyCommandDeck.Attach(root,font,"LOCAL NUMBERED UI QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(state);
   var recording=root.AddComponent<NumberedFormationRecording>();recording.Begin(output);
   Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
   void Check(string first){foreach(var side in new[]{"Left","Right"}){var b=Find(side+"Position0");Need(b.GetComponentInChildren<TMP_Text>().text.Contains(NumberedFormationView.Label(first)),"Wrong slot zero");float x=b.transform.localPosition.x,last=Find(side+"Position4").transform.localPosition.x;Need(side=="Left"?x<last:x>last,"Wrong threat-side position");}}
   void Departure(){typeof(LobbyCommandDeck).GetMethod("OpenDepartureQuote",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(deck,new object[]{"opn_01_last_departure","로컬 UI 검수 · 실제 출격 없음",0});}
   try{
    Departure();Check(ids[0]);await LandscapeScreenSweepProbe.Capture(output,"00-confirmed-baseline");Find("DepartureFormation").onClick.Invoke();Check(ids[0]);await LandscapeScreenSweepProbe.Capture(output,"01-edit-baseline");
    deck.BindAccount(state);deck.BindAccount(state);Find("AssignOwned0").onClick.Invoke();Find("RightPosition4").onClick.Invoke();Check(ids[4]);Need(state.Formation.Selection[4]==ids[0],"Swap did not preserve operator");await LandscapeScreenSweepProbe.Capture(output,"02-draft-swap");
    var stale=Find("LeftPosition0").onClick;deck.Pages.Close();stale.Invoke();Check(ids[0]);await LandscapeScreenSweepProbe.Capture(output,"03-unsaved-return");
    Find("DepartureFormation").onClick.Invoke();Check(ids[4]);int accountReads=wire.Calls.Count(x=>x=="GET /v1/account/state");Find("SaveFormation").onClick.Invoke();Need(state.Formation.CanActivate,"Save receipt not applied");Need(state.Account.ActiveOperators.Single(r=>r.Number("slot")==0).Text("operatorId")==ids[0],"Save activated draft");Find("ActivateFormation").onClick.Invoke();Need(state.Pending==null&&state.Account.ActiveOperators.Single(r=>r.Number("slot")==0).Text("operatorId")==ids[4],"Activation not applied");Need(wire.Calls.Count(x=>x=="GET /v1/account/state")==accountReads,"Confirmation account GET");deck.Pages.Close();Check(ids[4]);await LandscapeScreenSweepProbe.Capture(output,"04-activated-return");
    Find("DepartureFormation").onClick.Invoke();Find("AssignOwned0").onClick.Invoke();Find("LeftPosition0").onClick.Invoke();Check(ids[0]);Find("SaveFormation").onClick.Invoke();Find("ActivateFormation").onClick.Invoke();deck.Pages.Close();Check(ids[0]);await LandscapeScreenSweepProbe.Capture(output,"05-restored-return");
    File.WriteAllText(Path.Combine(output,"numbered-player-check.json"),new JObject{["passed"]=true,["scope"]="ACTUAL_PLAYER_UI_WITH_MEMORY_RESPONSE_FIXTURE_NO_START_NO_GAIT",["input"]="native Button.onClick scripted invocation; not hardware pointer",["checks"]=new JArray("baseline both threat sides","1-5 swap","draft return keeps active","save does not activate","v2 activation without confirmation account GET","activated return","restored mapping","rebind and stale input"),["calls"]=new JArray(wire.Calls)}.ToString());
   }finally{recording.Finish();deck.BindAccount(null);UnityEngine.Object.Destroy(root);}
  }
 }
 sealed class NumberedFormationRecording:MonoBehaviour
 {
  string directory;readonly JArray frames=new JArray();readonly System.Diagnostics.Stopwatch clock=new System.Diagnostics.Stopwatch();
  public void Begin(string output){directory=Path.Combine(output,"frames");Directory.CreateDirectory(directory);clock.Start();StartCoroutine(Record());}
  System.Collections.IEnumerator Record(){while(true){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();try{string file=frames.Count.ToString("D5")+".jpg";File.WriteAllBytes(Path.Combine(directory,file),texture.EncodeToJPG(85));frames.Add(new JObject{["file"]=file,["seconds"]=clock.Elapsed.TotalSeconds});}finally{Destroy(texture);}yield return new WaitForSecondsRealtime(.08f);}}
  public void Finish(){StopAllCoroutines();File.WriteAllText(Path.Combine(directory,"frames.json"),frames.ToString());}
 }
}
#endif
