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
public static class DeparturePanelNavigationQa
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
            foreach(var path in new[]{"Art53","Art54"}.SelectMany(folder=>Directory.GetFiles("Assets/_LASTRAIL/Content/RemoteResources/NoncombatPreparation/"+folder,"*.png")))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            }
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);var cases=new JArray();
            foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual Deck departure/preparation/guide/support callbacks through first-hit GraphicRaycaster; frozen Preparation03 synthetic GET data and explicit memory-only CE03 support host. No HTTP, save, quote, START or grant.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
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
            if(prep.transform.Find("BackDeparture/BackArrow")==null)throw new InvalidOperationException("Preparation independent back arrow missing");
            var preparationGeometry=new JArray();
            foreach(var art in prep.GetComponentsInChildren<RawImage>(true).Where(x=>x.name.StartsWith("PreparationChassis")||x.name.StartsWith("PreparationSupport")||x.name=="PreparationCarriage0"||x.name=="PreparationTrainEnvironment"))
            {
                if(art.texture==null)throw new InvalidOperationException("Required final panel texture missing: "+art.name);
                var texture=(Texture2D)art.texture;var uv=art.uvRect;var rect=art.rectTransform;
                preparationGeometry.Add(new JObject{["name"]=art.name,["texture"]=texture.name,["textureWidth"]=texture.width,["textureHeight"]=texture.height,["mipmapCount"]=texture.mipmapCount,["filter"]=texture.filterMode.ToString(),["format"]=texture.format.ToString(),["uv"]=new JArray(uv.x,uv.y,uv.width,uv.height),["rect"]=new JArray(rect.anchoredPosition.x,rect.anchoredPosition.y,rect.rect.width,rect.rect.height),["raycast"]=art.raycastTarget});
            }
            foreach(var graphic in prep.GetComponentsInChildren<DeparturePanelGraphic>(true))if(graphic.canvasRenderer==null||graphic.raycastTarget)throw new InvalidOperationException("Invalid panel decoration: "+graphic.name);
            foreach(var name in new[]{"PreparationSupportFrame0","PreparationSupportFrame1","PreparationSupportFrame2","PreparationSupport0","PreparationSupport1","PreparationSupport2","BackDeparture/BackArrow","DepartureOperation/ChangeArrow","DepartureFormation/ChangeArrow","DepartureTrain/ChangeArrow","LocalCommandSelection/ChangeArrow"}){var r=(RectTransform)prep.transform.Find(name);preparationGeometry.Add(new JObject{["name"]=name,["x"]=r.anchoredPosition.x,["y"]=r.anchoredPosition.y,["width"]=r.rect.width,["height"]=r.rect.height});}
            void PrepHide(string name,GameObject[] items){foreach(var item in items)item.SetActive(false);Record("preparation-hide-"+name,prep);foreach(var item in items)item.SetActive(true);Record("preparation-restore-"+name,prep);}
            PrepHide("support-icons",Enumerable.Range(0,3).Select(i=>prep.transform.Find("PreparationSupport"+i).gameObject).ToArray());
            PrepHide("support-labels",Enumerable.Range(0,3).Select(i=>prep.transform.Find("PreparationSupportLabel"+i).gameObject).ToArray());
            foreach(var prefix in new[]{"PreparationSupportFrame","PreparationSupportPlate","PreparationChassis","PreparationCarriagePlate","PreparationCarriageOutline","PreparationCarriageNumber","PreparationCarriageRole","PreparationCarriageLock","PreparationCarriageLamp","PreparationCarriageCoupler","PreparationCarriageBadge"})
                PrepHide(prefix,prep.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith(prefix)).Select(x=>x.gameObject).ToArray());
            PrepHide("support-environment",new[]{prep.transform.Find("PreparationSupportEnvironment").gameObject});
            PrepHide("train-environment",new[]{prep.transform.Find("PreparationTrainEnvironmentViewport/PreparationTrainEnvironment").gameObject});
            PrepHide("locomotive",new[]{prep.transform.Find("PreparationCarriage0").gameObject});
            PrepHide("rail",new[]{prep.transform.Find("PreparationRail").gameObject});
            PrepHide("back-arrow",new[]{prep.transform.Find("BackDeparture/BackArrow").gameObject});
            PrepHide("change-arrows",new[]{"DepartureOperation","DepartureFormation","DepartureTrain","LocalCommandSelection"}.Select(n=>prep.transform.Find(n+"/ChangeArrow").gameObject).ToArray());
            var initialSupport=Enumerable.Range(0,3).Select(i=>prep.transform.Find("PreparationSupport"+i).GetComponent<RawImage>().texture).ToArray();
            var baselineIconOffsets=Enumerable.Range(0,3).Select(i=>((RectTransform)prep.transform.Find("PreparationSupport"+i)).anchoredPosition-((RectTransform)prep.transform.Find("PreparationSupportFrame"+i)).anchoredPosition).ToArray();
            var baselinePositions=Enumerable.Range(0,3).Select(i=>((RectTransform)prep.transform.Find("PreparationSupportFrame"+i)).anchoredPosition).ToArray();
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,height==1280?1600:1280);Layout();
            var delta=((RectTransform)prep.transform.Find("PreparationSupportFrame0")).anchoredPosition-baselinePositions[0];
            foreach(int i in Enumerable.Range(0,3)){var card=(RectTransform)prep.transform.Find("PreparationSupportFrame"+i);var icon=(RectTransform)prep.transform.Find("PreparationSupport"+i);if(card.anchoredPosition-baselinePositions[i]!=delta||Vector2.Distance(icon.anchoredPosition-card.anchoredPosition,baselineIconOffsets[i])>.01f)throw new InvalidOperationException("Support resize drift");}
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,height);Record("preparation-resize-restored",prep);
            Click(prep,"DepartureOperation");Click(deck,"SelectDeparture0");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();Record("preparation-operation-return",prep);
            foreach(var item in new[]{("DepartureFormation",LobbyPage.Formation),("DepartureTrain",LobbyPage.Train)})
            {
                Click(prep,item.Item1);if(!deck.Pages.IsOpen||deck.Pages.CurrentPage!=item.Item2)throw new InvalidOperationException("Preparation destination mismatch");
                Click(deck,"Back");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();if(prep==null)throw new InvalidOperationException("Preparation destination return missing");Record("preparation-"+item.Item1+"-return",prep);
            }
            Click(prep,"DepartureGuideEntry");await Task.Yield();var guide=root.GetComponentInChildren<DepartureGuideView>();if(guide==null)throw new InvalidOperationException("Guide missing");
            if(guide.transform.Find("GuideJunctionRing")==null)throw new InvalidOperationException("Guide independent junction missing");
            var junctionGeometry=new JArray();
            void Junction(){Layout();var ring=(RectTransform)guide.transform.Find("GuideJunctionRing");var dot=(RectTransform)guide.transform.Find("GuideJunctionDot");var line=(RectTransform)guide.transform.Find("GuideConnection1");var end=line.TransformPoint(new Vector3(line.rect.xMax,0));var center=ring.TransformPoint(ring.rect.center);if(Vector3.Distance(end,center)>.01f||Vector3.Distance(dot.TransformPoint(dot.rect.center),center)>.01f)throw new InvalidOperationException("Guide endpoint detached");junctionGeometry.Add(new JObject{["step"]=guide.Step,["ringWidth"]=ring.rect.width,["dotWidth"]=dot.rect.width,["centerX"]=center.x,["centerY"]=center.y,["distance"]=Vector3.Distance(end,center)});}
            for(int step=0;step<4;step++){if(guide.Step!=step)throw new InvalidOperationException("Guide step mismatch");Junction();Record("guide-step"+(step+1),guide);if(step==0){
                foreach(var name in new[]{"GuideJunctionRing","GuideJunctionDot","GuideCallout/Interior","GuideCallout/GuideCalloutFrame"}){var item=guide.transform.Find(name);item.gameObject.SetActive(false);Record("guide-hide-"+item.name,guide);item.gameObject.SetActive(true);Record("guide-restore-"+item.name,guide);}
                var callout=guide.transform.Find("GuideCallout");callout.gameObject.SetActive(false);Record("guide-callout-hidden",guide);callout.gameObject.SetActive(true);Record("guide-callout-restored",guide);
                var edges=guide.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("GuideFocusEdge")).ToArray();foreach(var edge in edges)edge.enabled=false;Record("guide-focus-hidden",guide);foreach(var edge in edges)edge.enabled=true;Record("guide-focus-restored",guide);
                var shades=guide.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("GuideShade")).ToArray();foreach(var shade in shades)shade.enabled=false;Record("guide-shade-hidden",guide);foreach(var shade in shades)shade.enabled=true;Record("guide-shade-restored",guide);
            }if(step==1){Click(guide,"GuidePrevious");if(guide.Step!=0)throw new InvalidOperationException("Guide previous failed");Click(guide,"GuideNext");}Click(guide,"GuideNext");}
            if(root.GetComponentInChildren<DepartureGuideView>()!=null)throw new InvalidOperationException("Guide final close failed");Record("guide-return",prep);
            Click(prep,"DepartureGuideEntry");await Task.Yield();guide=root.GetComponentInChildren<DepartureGuideView>();Click(guide,"GuideClose");if(root.GetComponentInChildren<DepartureGuideView>()!=null)throw new InvalidOperationException("Guide explicit close failed");
            Click(prep,"LocalCommandSelection");await Task.Yield();var panel=root.GetComponentInChildren<Ce03SupportSelectionPanel>();if(panel==null)throw new InvalidOperationException("Support missing");Record("support-selected",panel);
            var statuses=panel.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("ChoiceState")).ToArray();if(statuses.Length!=10||statuses.Count(t=>t.text=="준비 중")!=7)throw new InvalidOperationException("Support names and preparation states are not independently laid out");
            void HideGroup(string name,GameObject[] layers){foreach(var layer in layers)layer.SetActive(false);Record("support-hide-"+name,panel);foreach(var layer in layers)layer.SetActive(true);Record("support-restore-"+name,panel);}
            HideGroup("candidate-icons",panel.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("ChoiceIcon")).Select(g=>g.gameObject).ToArray());
            HideGroup("candidate-names",panel.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("Choice")&&t.name.EndsWith("Label")).Select(t=>t.gameObject).ToArray());
            HideGroup("preparation-states",statuses.Select(t=>t.gameObject).ToArray());
            HideGroup("selection-checks",panel.GetComponentsInChildren<RewardResourceSymbol>().Where(g=>g.name.StartsWith("ChoiceCheck")).Select(g=>g.gameObject).ToArray());
            HideGroup("order-numbers",panel.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("ChoiceBadge")).Select(t=>t.gameObject).ToArray());
            HideGroup("slot-icons",panel.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("SlotIcon")).Select(g=>g.gameObject).ToArray());
            foreach(var name in new[]{"SupportRoom","SupportHost"}){var art=panel.GetComponentsInChildren<RawImage>().Single(i=>i.name==name);art.enabled=false;Record(name+"-hidden",panel);art.enabled=true;Record(name+"-restored",panel);}
            string Slot(int index)=>panel.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="SlotName"+index).text;
            int SelectedCount()=>panel.GetComponentsInChildren<TMP_Text>().Count(t=>t.name.StartsWith("ChoiceBadge")&&!string.IsNullOrEmpty(t.text));
            var originalSlots=new[]{Slot(0),Slot(1),Slot(2)};Click(panel,"Choice3");if(SelectedCount()!=3||!originalSlots.SequenceEqual(new[]{Slot(0),Slot(1),Slot(2)}))throw new InvalidOperationException("Fourth support changed selection");Record("support-three-limit",panel);
            Click(panel,"MoveLeft2");if(!Slot(1).Contains("긴급 수리")||!Slot(2).Contains("방어막"))throw new InvalidOperationException("Reorder callback failed");Record("support-reordered",panel);Click(panel,"MoveLeft2");if(!originalSlots.SequenceEqual(new[]{Slot(0),Slot(1),Slot(2)}))throw new InvalidOperationException("Reorder restoration failed");Record("support-reorder-restored",panel);
            Click(panel,"RemoveSlot2");if(SelectedCount()!=2)throw new InvalidOperationException("Remove callback failed");Record("support-removed",panel);Click(panel,"Choice2");if(!originalSlots.SequenceEqual(new[]{Slot(0),Slot(1),Slot(2)}))throw new InvalidOperationException("Selection restoration failed");Record("support-remove-restored",panel);
            Click(panel,"Choice0");if(panel.GetComponentsInChildren<Button>().Single(b=>b.name=="Save").interactable)throw new InvalidOperationException("Incomplete draft allowed save");Record("support-incomplete-draft",panel);
            Click(panel,"Choice3");if(SelectedCount()!=3||!Slot(2).Contains("제압 포격"))throw new InvalidOperationException("Prepared support selection contract changed");Record("support-prepared-draft",panel);Click(panel,"Choice3");if(SelectedCount()!=2)throw new InvalidOperationException("Prepared support deselect failed");Record("support-prepared-removed",panel);
            Click(panel,"Back");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();if(prep==null||root.GetComponentInChildren<Ce03SupportSelectionPanel>()!=null)throw new InvalidOperationException("Support return failed");if(!initialSupport.SequenceEqual(Enumerable.Range(0,3).Select(i=>prep.transform.Find("PreparationSupport"+i).GetComponent<RawImage>().texture)))throw new InvalidOperationException("Support order lost on return");Record("support-return",prep);
            Click(prep,"LocalCommandSelection");await Task.Yield();panel=root.GetComponentInChildren<Ce03SupportSelectionPanel>();if(!panel.GetComponentsInChildren<Button>().Single(b=>b.name=="Save").interactable)throw new InvalidOperationException("Unsaved draft leaked into server selection");Record("support-reopened",panel);Click(panel,"Back");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();Click(prep,"BackDeparture");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Lobby return/write boundary");Record("lobby-return",deck);
            Click(deck,"Departure");Click(deck,"SelectDeparture0");await Task.Yield();prep=root.GetComponentInChildren<DeparturePreparationView>();Click(prep,"DepartureGuideEntry");guide=root.GetComponentInChildren<DepartureGuideView>();var marker=guide.transform.Find("GuideJunctionRing");session=null;state.SynchronizeSession();guide.ValidateOwner();if(root.GetComponentInChildren<DepartureGuideView>()!=null||marker!=null)throw new InvalidOperationException("Guide decoration survived owner closure");Record("guide-owner-invalid",prep);Click(prep,"BackDeparture");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["junctionGeometry"]=junctionGeometry,["guideOwnerInvalidClosed"]=true,["preparationGeometry"]=preparationGeometry,["preparationResizeRoundTrip"]=true,["supportOrderRetained"]=true,["nativeFirstHitClicks"]=clicks,["reads"]=wire.Reads,["paths"]=new JArray(wire.Paths),["writes"]=wire.Writes,["guideReturn"]=true,["supportReturn"]=true,["draftDiscarded"]=true,["lobbyReturn"]=true});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
