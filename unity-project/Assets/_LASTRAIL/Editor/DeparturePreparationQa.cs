#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

public static class DeparturePreparationQa
{
    static object Fixture(string type,string method,params object[] args)
    {
        var definition=AppDomain.CurrentDomain.GetAssemblies().Select(x=>x.GetType("LastRail.Tests.EditMode."+type)).FirstOrDefault(x=>x!=null);
        if(definition==null)throw new InvalidOperationException("Existing QA fixture assembly unavailable: "+type);
        return definition.GetMethod(method,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);
    }
    sealed class Wire:IRunApiHttpTransport
    {
        public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public string Mode="populated";public int Reads,Writes;
        readonly JObject account,train,ultimate;
        public Wire()
        {
            account=JObject.Parse((string)Fixture("FormationContractTests","S03Json"));
            train=(JObject)Fixture("TrainLoadoutTests","Snapshot",1L);
            var ids=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/RemoteResources/OperatorIdentity/catalog.json"))["operators"].Take(5).Select(x=>(string)x["operatorId"]).ToArray();
            account["operators"]=new JArray(ids.Select(id=>new JObject{["operatorId"]=id,["skillTraining"]=1,["promotion"]=0,["revision"]=0,["acquiredAt"]="2026-09-10T00:00:00Z"}));
            account["trainingCoreSlots"]=new JArray(ids.Select((id,i)=>new JObject{["slot"]=i,["operatorId"]=id,["trainedLevel"]=3+i,["investedOperatorExp"]=120,["revision"]=0}));
            var formation=(JObject)account["formation"];formation["activePresetId"]="preset_qa";formation["activePresetRevision"]=1;formation["revision"]=1;formation["updatedAt"]="2026-09-10T00:00:00Z";
            formation["operators"]=new JArray(ids.Select((id,i)=>new JObject{["slot"]=i,["operatorId"]=id}));
            ultimate=(JObject)Fixture("UltimateSelectionTests","Read",ids[0],1L);
        }
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Preparation QA forbids mutations");}
            Reads++;var mode=Mode;await Gate.Task;
            if(mode=="error")return RunApiHttpResponse.Success(503,"{}");
            if(request.EndpointPath=="/v1/account/state")return RunApiHttpResponse.Success(200,mode=="empty"?(string)Fixture("FormationContractTests","S03Json"):account.ToString());
            if(request.EndpointPath=="/v1/trains/loadout")return RunApiHttpResponse.Success(200,mode=="empty"?((JObject)Fixture("TrainLoadoutTests","Snapshot",0L)).ToString():train.ToString());
            if(request.EndpointPath=="/v1/formation/ultimate")return RunApiHttpResponse.Success(200,ultimate.ToString());
            return RunApiHttpResponse.Success(404,"{}");
        }
        public JObject Inputs=>new JObject{["account"]=account,["train"]=train,["ultimate"]=ultimate};
    }
    sealed class Journal:IAccountActionJournal
    {
        public PendingAccountAction Read(string id)=>null;
        public bool Save(PendingAccountAction action)=>throw new InvalidOperationException("No QA journal write");
        public bool Remove(PendingAccountAction action)=>throw new InvalidOperationException("No QA journal removal");
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatPreparation/cockpit-v01.png");
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var cases=new JArray();foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual lobby operation entry and AccountScreenRuntime with delayed synthetic GET fixtures derived from existing test contracts; no live network, quote POST, START, or grants. Driver alpha and final resource likeness remain open.",["cases"]=cases}.ToString());
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("PreparationQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        try
        {
            File.WriteAllText(Path.Combine(output,"synthetic-read-inputs-"+height+".json"),wire.Inputs.ToString());
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_01_last_departure","황무지 철로 · LOCAL QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(state);
            void Click(string name){var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);ExecuteEvents.Execute(button.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
            void Enter(){deck.OpenOperations();Click("SelectDeparture0");}
            int count=0;var overflow=new JArray();
            void Record(string name)
            {
                Canvas.ForceUpdateCanvases();deck.EvaluateLayout();
                foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&label.GetComponentInParent<DeparturePreparationView>()!=null)overflow.Add(name+":"+label.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"preparation-"+height+"-"+name+".png"),pixels.EncodeToPNG());count++;
            }
            async Task Release(){wire.Gate.TrySetResult(true);for(int i=0;state.AccountStatus==AccountStateReadStatus.Loading||state.TrainLoadoutLoading||state.UltimateLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            Enter();Record("loading");await Release();if(state.Account?.ActiveOperators.Count!=5||!state.TrainLoadoutReady)throw new InvalidOperationException("Populated fixture not read");Record("populated");
            Click("DepartureFormation");if(deck.Pages?.CurrentPage!=LobbyPage.Formation||!deck.Pages.IsOpen)throw new InvalidOperationException("Formation entry failed");deck.Pages.Close();await Release();Record("formation-return");
            var view=root.GetComponentInChildren<DeparturePreparationView>();var environment=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="PreparationEnvironment");var texture=environment.texture;
            environment.enabled=false;Record("hide-environment");environment.enabled=true;environment.texture=Texture2D.whiteTexture;Record("swap-environment");environment.texture=texture;Record("restored");
            wire.Mode="empty";wire.Gate=new TaskCompletionSource<bool>();Enter();Record("empty-loading");await Release();Record("empty");
            wire.Mode="error";wire.Gate=new TaskCompletionSource<bool>();Enter();Record("error-loading");await Release();Record("error");
            session=null;state.SynchronizeSession();Record("owner-cleared");Click("BackDeparture");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Return or mutation boundary failed");
            cases.Add(new JObject{["height"]=height,["captures"]=count,["reads"]=wire.Reads,["writes"]=wire.Writes,["formationReturn"]=true,["backClosed"]=true,["overflow"]=overflow});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
