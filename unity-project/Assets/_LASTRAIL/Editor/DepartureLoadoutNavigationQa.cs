#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Editor-only capture of existing navigation; all transport and journal boundaries are local fixtures.
public static class DepartureLoadoutNavigationQa
{
    sealed class Wire : IRunApiHttpTransport
    {
        public readonly JObject Inputs=JObject.Parse(File.ReadAllText("QA/NoncombatFinal-20260915/Preparation03/synthetic-read-inputs-1280.json"));
        public int Reads,Writes;
        public readonly List<string> Paths=new List<string>();
        static readonly string[] Ids={"support_fire","barrier","emergency_repair","ce03.role.suppressive_barrage","ce03.role.medical_support","ce03.role.smoke","ce03.role.weakness_scan","ce03.role.firepower_boost","ce03.role.electronic_disruption","ce03.role.emergency_protection"};
        public JObject Support=>new JObject{
            ["selection"]=new JObject{["rulesVersion"]="ce03-support-selection-v1",["catalogVersion"]="ce03-local-support-roles-v1",["revision"]=1,["slots"]=new JArray(Ids.Take(3).Select((id,i)=>new JObject{["slot"]=i,["skillId"]=id}))},
            ["catalog"]=new JObject{["scope"]="LOCAL_SELECTION_ONLY",["entries"]=new JArray(Ids.Select((id,i)=>new JObject{["skillId"]=id,["executionAvailable"]=i<3}))}};
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Navigation QA forbids all mutations: "+request.EndpointPath);}
            Reads++;Paths.Add(request.EndpointPath);
            JToken data=request.EndpointPath=="/v1/account/state"?Inputs["account"]:request.EndpointPath=="/v1/trains/loadout"?Inputs["train"]:request.EndpointPath=="/v1/formation/ultimate"?Inputs["ultimate"]:request.EndpointPath=="/__local/ce03/support-loadout"?Support:null;
            return Task.FromResult(RunApiHttpResponse.Success(data==null?404:200,data?.ToString()??"{}"));
        }
    }
    sealed class Journal:IAccountActionJournal
    {
        public PendingAccountAction Read(string id)=>null;
        public bool Save(PendingAccountAction action)=>throw new InvalidOperationException("No journal mutation");
        public bool Remove(PendingAccountAction action)=>throw new InvalidOperationException("No journal removal");
    }
    public static async void Run()
    {
        try{
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);var cases=new JArray();
            foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual Deck preparation/formation/train/reentry/back callbacks through first-hit GraphicRaycaster; frozen Preparation03 synthetic GET data and explicit memory-only CE03 support host. No HTTP, save, quote, START or grant.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("NavigationQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.offsetMin=safeRect.offsetMax=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        try{
            File.WriteAllText(Path.Combine(output,"synthetic-inputs-"+height+".json"),new JObject{["preparation"]=wire.Inputs,["support"]=wire.Support}.ToString());
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_01_last_departure","황무지 철로 · LOCAL QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(state);
            deck.BindLocalCommandClient(new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"qa",wire,new GameApiAccessToken("lst1.fixture"),_=>throw new InvalidOperationException("No support save")));
            int captures=0;var clicks=new JArray();
            void Layout(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Click(Component owner,string name){
                Layout();var button=owner.GetComponentsInChildren<Button>().Single(b=>b.name==name);if(!button.interactable)throw new InvalidOperationException("Disabled "+name);
                var rect=(RectTransform)button.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("First hit mismatch "+name+": "+(hits.Count==0?"none":hits[0].gameObject.name));
                clicks.Add(name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            }
            void Record(string name,Component owner){Layout();foreach(var label in owner.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new InvalidOperationException("Overflow "+name+":"+label.name);}camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"navigation-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;}
            Click(deck,"Departure");Click(deck,"SelectDeparture0");await Task.Yield();var prep=root.GetComponentInChildren<DeparturePreparationView>();if(prep==null)throw new InvalidOperationException("Preparation missing");Record("preparation",prep);
            foreach(var route in new[]{LobbyPage.Formation,LobbyPage.Train}){
                string buttonName=route==LobbyPage.Formation?"DepartureFormation":"DepartureTrain";
                for(int visit=0;visit<2;visit++){
                    Click(prep,buttonName);await Task.Yield();var page=deck.Pages;
                    if(page==null||!page.IsOpen||page.CurrentPage!=route||root.GetComponentInChildren<DeparturePreparationView>()!=null)throw new InvalidOperationException("Route entry failed "+route);
                    Record(route.ToString().ToLower()+"-visit"+(visit+1),page);
                    if(route==LobbyPage.Train&&visit==0){Click(page,"Category1");if(page.Category!=1)throw new InvalidOperationException("Train loadout tab failed");Record("train-loadout",page);}
                    Click(page,"Back");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();
                    if(page.IsOpen||prep==null||!deck.IsDialogOpen)throw new InvalidOperationException("Preparation callback return failed "+route);
                    if(state.Account?.ActiveOperators.Count!=5||!state.TrainLoadoutReady||state.Pending!=null||state.TrainDraftDirty)throw new InvalidOperationException("Read-only navigation changed authoritative selection");
                    Record(route.ToString().ToLower()+"-return"+(visit+1),prep);
                }
            }
            Click(prep,"BackDeparture");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Lobby return/write boundary");Record("lobby-return",deck);
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["nativeFirstHitClicks"]=clicks,["reads"]=wire.Reads,["paths"]=new JArray(wire.Paths),["writes"]=wire.Writes,["formationReturnTwice"]=true,["trainReturnTwice"]=true,["trainLoadoutTab"]=true,["draftUnchanged"]=true,["lobbyReturn"]=true});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

