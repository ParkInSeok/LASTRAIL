#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class ResultHistoryQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
    sealed class Wire:IRunApiHttpTransport
    {
        public string Mode="known";public bool Victory;public int Reads,Writes;public TaskCompletionSource<bool> Gate;
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No QA writes");}Reads++;if(Gate!=null)await Gate.Task;
            if(request.EndpointPath.EndsWith("/lifecycle"))
            {
                var data=JObject.Parse(File.ReadAllText(Source+"result-lost-finish-restored.json"));
                if(Victory){data["runStatus"]="COMPLETED";data["finish"]["outcome"]="VICTORY";}
                if(Mode!="legacy")
                {
                    var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};if(Mode=="unknown-id")ids[4]="operator_not_in_visual_catalog";
                    data["startResponse"]["admission"]=new JObject{["version"]=Mode=="unsupported"?"departure-future":"departure-v2",["formation"]=new JObject{["schemaVersion"]=1,["operators"]=new JArray(ids.Select((id,slot)=>new JObject{["slot"]=slot,["operatorId"]=Mode=="malformed"?"op_kyle":id}).Reverse())}};
                }
                return RunApiHttpResponse.Success(200,data.ToString());
            }
            if(request.EndpointPath.Contains("/reward-preview/"))return RunApiHttpResponse.Success(200,File.ReadAllText(Source+"result-preview.json"));
            return RunApiHttpResponse.Success(404,"{}");
        }
    }
    public static async void Run()
    {
        try
        {
            string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");Directory.CreateDirectory(output);
            var cases=new JArray();foreach(int h in new[]{1280,1600})foreach(bool victory in new[]{false,true})await Capture(output,h,victory,cases);
            File.WriteAllText(output+"/verification.json",new JObject{["scope"]="48 actual AccountScreenRuntime/lifecycle/presentations with in-memory GET-only transport. Added admission and victory are explicit local derived fixtures; original server evidence untouched. No true historical participation/wave/time inferred.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,bool victory,JArray cases)
    {
        var root=new GameObject("ResultHistoryQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
        var wire=new Wire{Victory=victory};using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_03_water_stop","급수 정거장",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(owner);
            int captures=0;var clicks=new JArray();var states=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();camera.Render();}
            void Save(string name){Draw();foreach(var label in root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="DepartureOperatorLabel"||t.name=="ResultParticipantsText"))if(label.isTextOverflowing)throw new InvalidOperationException("History text overflow "+name);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/history-"+(victory?"victory":"failed")+"-"+height+"-"+name+".png",pixels.EncodeToPNG());captures++;}
            void Click(string name){Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);if(!b.interactable)throw new InvalidOperationException("Disabled "+name);var rect=(RectTransform)b.transform;var p=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(p,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("First hit "+name);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);clicks.Add(name);}
            void Verify(string phase,bool populated)
            {
                Draw();var history=new RunResultHistoryDisplayState(owner,"run-smoke-1");if(history.HasFormation!=populated)throw new InvalidOperationException("Roster state "+phase);
                var labels=root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name==(victory?"DepartureOperatorLabel":"ResultParticipantsText")).ToArray();string text=string.Join("\n",labels.Select(t=>t.text));
                if(populated&&!text.Contains("카일")||!populated&&text.Contains("카일"))throw new InvalidOperationException("Stale/unconnected history "+phase);
                if(victory){int count=root.GetComponentsInChildren<RawImage>().Count(i=>i.name=="DeparturePortrait"&&i.enabled&&i.texture!=null);if(count!=(populated?(wire.Mode=="unknown-id"?4:5):0))throw new InvalidOperationException("Stale portrait "+phase);}
                var wave=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==(victory?"VictoryWave":"ResultStatsText"));var time=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==(victory?"VictoryTime":"ResultTimeText"));if(!wave.text.Contains("기록 확인 필요")||!time.text.Contains("기록 확인 필요"))throw new InvalidOperationException("Invented wave/time");
                states.Add(new JObject{["phase"]=phase,["formationStatus"]=owner.Lifecycle?.StartFormation.Status.ToString()??"NoLifecycle",["operatorIds"]=new JArray(history.OperatorIds),["displayText"]=text,["wave"]=wave.text,["time"]=time.text});
            }
            deck.OpenRunLifecycle("run-smoke-1");Verify("known",true);Save("known");
            var labelsToHide=root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name==(victory?"DepartureOperatorLabel":"ResultParticipantsText")).ToArray();foreach(var t in labelsToHide)t.enabled=false;Save("hide-labels");foreach(var t in labelsToHide)t.enabled=true;Save("restore-labels");
            if(victory){var portraits=root.GetComponentsInChildren<RawImage>().Where(i=>i.name=="DeparturePortrait").ToArray();foreach(var p in portraits)p.enabled=false;Save("hide-portraits");foreach(var p in portraits)p.enabled=true;Save("restore-portraits");Click("VictoryDetails");Save("details");}
            int before=wire.Reads;Click(victory?"VictoryRefresh":"RefreshRunLifecycle");if(wire.Reads-before!=2)throw new InvalidOperationException("Refresh duplicated reads");if(victory)Click("VictoryDetails");
            foreach(string mode in new[]{"legacy","malformed","unsupported","unknown-id","known"})
            {
                wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();var read=owner.ReadRunResultsAsync("run-smoke-1");Verify(mode+"-loading",false);Save(mode+"-loading");wire.Gate.SetResult(true);await read;wire.Gate=null;Verify(mode,mode=="known"||mode=="unknown-id");Save(mode=="known"?"restored":mode);
            }
            session=null;owner.SynchronizeSession();Verify("owner-cleared",false);Save("owner-cleared");Click(victory?"VictoryReturn":"CloseLobbyDialog");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Exit/write boundary");Save("returned");
            cases.Add(new JObject{["height"]=height,["victory"]=victory,["captures"]=captures,["reads"]=wire.Reads,["writes"]=wire.Writes,["nativeFirstHitClicks"]=clicks,["states"]=states,["runOrigin"]="Original missing-history fixture plus explicitly derived admission/outcome; not realplayed result"});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
#endif
