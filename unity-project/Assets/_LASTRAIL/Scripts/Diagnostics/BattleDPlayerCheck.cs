#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LastRail.Diagnostics
{
    internal static class BattleDPlayerCheck
    {
        const string NodePath = "D:/Project/LASTRAIL/QA/CommanderS1-20260922/HpOwner839/node-source.json";
        static readonly string[] Controls = {"Speed","Pause","BranchLever","TrainThrottle","ConfirmRailRoute","Command0","Command1","Command2","DSupportRack","UnavailableSupport0","UnavailableSupport1","UnavailableSupport2","UltimateAuto"};
        static async Task Frame() { await Awaitable.NextFrameAsync(); }
        static async Task SetSize(int width, int height)
        {
            Screen.SetResolution(width,height,FullScreenMode.Windowed);
            for(int i=0;i<120 && (Screen.width!=width || Screen.height!=height);i++) await Frame();
            if(Screen.width!=width || Screen.height!=height) throw new InvalidOperationException($"Requested {width}x{height}; Player reported {Screen.width}x{Screen.height}.");
            await Frame();await Frame();
        }
        static async Task Screenshot(string path)
        {
            await Awaitable.EndOfFrameAsync();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(path,image.EncodeToPNG());} finally{UnityEngine.Object.Destroy(image);}
            await Frame();
        }
        internal static RectTransform Find(Transform view,string name) =>
            view.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name==name);
        internal static Rect Bounds(RectTransform rect,Camera camera)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
        }
        static JObject Brief(CombatSceneDriver driver,ApprovedCombatView view)
        {
            var hud=driver.HudState;
            return new JObject{["speed"]=hud["speedHalfUnits"]?.DeepClone(),["paused"]=hud["paused"]?.DeepClone(),["pauseOpen"]=view.GetComponent<CombatPauseController>()?.IsOpen,
                ["selected"]=view.InformationFrame?.Target?.ActorId,["revision"]=driver.CaptureCommittedState().Revision,
                ["ultimateAuto"]=hud["ultimateAuto"]?.DeepClone(),["rail"]=hud["railJourney"] is JObject r?new JObject{["phase"]=r["phase"],["throttle"]=r["throttleBps"],["preview"]=r["previewIndex"],["offerId"]=(r["offer"] as JObject)?["id"]}:null,
                ["manuals"]=hud["manuals"]?.DeepClone(),["modelHash"]=Ce03CommandContract.Sha(driver.StateJson)};
        }
        internal static async Task Click(JArray rows, Mouse mouse, EventSystem system, ApprovedCombatView view, Camera camera, CombatSceneDriver driver,string name,float fractionY=.5f,float fractionX=.5f)
        {
            var rect=Find(view.transform,name); var row=new JObject{["name"]=name,["before"]=Brief(driver,view)};rows.Add(row);
            if(rect==null || !rect.gameObject.activeInHierarchy){row["active"]=false;return;}
            var b=Bounds(rect,camera);var p=new Vector2(b.xMin+b.width*fractionX,b.yMin+b.height*fractionY);
            var hits=new List<RaycastResult>();system.RaycastAll(new PointerEventData(system){position=p},hits);
            row["screenRect"]=new JArray(b.xMin,b.yMin,b.width,b.height);row["inside"]=b.xMin>=-.5f&&b.yMin>=-.5f&&b.xMax<=Screen.width+.5f&&b.yMax<=Screen.height+.5f;
            var selected=rect.GetComponent<Selectable>();row["interactable"]= selected==null?null:(JToken)selected.IsInteractable();
            row["topHit"]=hits.FirstOrDefault().gameObject?.name;row["targetReached"]=hits.Count>0&&hits[0].gameObject.transform.IsChildOf(rect);
            void Queue(bool down) => InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left,down));
            Queue(false);await Frame();await Frame();Queue(true);await Frame();await Frame();Queue(false);await Frame();await Frame();
            row["after"]=Brief(driver,view);
        }

        static async Task<string> FindSupportTarget(string folder,Func<BasicCombatSimulation> factory,JObject evidence)
        {
            var search=new JArray();evidence["eligibilityPaths"]=search;
            foreach(string name in new[]{"initial-factory-model.json","route-confirmed.state.json","artillery-live.state.json","route-traversed.state.json","artillery-focused.state.json","ultimate-ready.state.json"}){
                var model=BasicCombatSimulation.RestoreLocalState(File.ReadAllText(folder+name),factory);
                var path=new JObject{["checkpoint"]=name,["startTick"]=model.Tick};search.Add(path);
                for(int step=0;step<=1200;step++){
                    var hud=model.ReadHudState();var command=((JArray)hud["manuals"]).Single(c=>(string)c["ability"]=="support_fire");
                    path["lastTick"]=model.Tick;path["lastStatus"]=command["status"];
                    if((bool)command["enabled"]){
                        evidence["checkpoint"]=name;evidence["eligibleTick"]=model.Tick;evidence["eligibleStatus"]=command["status"];
                        return model.CaptureLocalState();
                    }
                    if(model.Outcome!=BasicCombatOutcome.Running || (string)hud["railJourney"]["phase"]=="Selecting")break;
                    model.Advance(1000000L,100);
                    if(step%60==0)await Frame();
                }
            }
            throw new InvalidOperationException("No retained source or observed forward step supplied an eligible support-fire target.");
        }
        static async Task ActiveRail(string output,EventSystem ev,Mouse mouse)
        {
            const string folder="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
            var identity=JObject.Parse(File.ReadAllText(folder+"session/session.json"));
            var wrapper=JObject.Parse(File.ReadAllText(folder+"session/preparation.json"));
            var preparation=new Ce03RailPreparation(wrapper,(string)identity["owner"],(long)identity["supportRevision"]);
            var results=new JArray();
            try{
                foreach(string stateFile in new[]{"initial-factory-model.json","selection-pending.state.json","restored-selection.state.json","ultimate-ready.state.json","support-target-search"}){
                    var row=new JObject{["checkpoint"]=stateFile};
                    results.Add(row);
                    Func<BasicCombatSimulation> factory=()=>preparation.CreateModel((uint)identity["seed"]);
                    string restoredState;
                    try{restoredState=stateFile=="support-target-search"?await FindSupportTarget(folder,factory,row):File.ReadAllText(folder+stateFile);}
                    catch(Exception error){row["error"]=error.ToString();continue;}
                    var driver=new CombatSceneDriver(factory,()=>new RestoredCombatCheckpoint(BasicCombatSimulation.RestoreLocalState(restoredState,factory),1),(m,r)=>r+1,()=>true);
                    var host=new GameObject("ExistingCE03Controls",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
                    var cam=new GameObject("CE03ControlsCamera",typeof(Camera)).GetComponent<Camera>();
                    try{
                        driver.Open();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
                        var view=host.AddComponent<ApprovedCombatView>();
                        view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.UseLandscapeLayout();
                        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
                        await Frame();await Frame();Canvas.ForceUpdateCanvases();
                        string stage=stateFile.Split('.')[0];
                        var hud=driver.HudState;
                        File.WriteAllText(Path.Combine(output,"CE03-"+stage+"-hud.json"),hud.ToString());
                        await Screenshot(Path.Combine(output,"CE03-"+stage+".png"));
                        var inputs=new JArray();row["pointerInputs"]=inputs;
                        if(stage=="selection-pending"){
                            await Click(inputs,mouse,ev,view,cam,driver,"BranchLever",.5f,.92f);
                        }else if(stage=="restored-selection"){
                            await Click(inputs,mouse,ev,view,cam,driver,"ConfirmRailRoute");
                        }else if(stage=="support-target-search"){
                            await Click(inputs,mouse,ev,view,cam,driver,"Command1");
                            driver.Advance(10000000L,100);view.Refresh();
                            row["afterOneSecond"]=Brief(driver,view);
                        }else if(stage=="initial-factory-model"){
                            await Click(inputs,mouse,ev,view,cam,driver,"TrainThrottle",.87f);
                            foreach(int index in new[]{0,2}){
                                var command="Command"+index;
                                for(int second=0;second<180 && !(bool)driver.HudState["manuals"][index]["enabled"] && driver.Outcome==BasicCombatOutcome.Running;second++){
                                    if((string)driver.HudState["railJourney"]["phase"]=="Selecting")break;
                                    driver.Advance(10000000L,100);
                                    if(second%8==0)await Frame();
                                }
                                view.Refresh();await Frame();
                                await Click(inputs,mouse,ev,view,cam,driver,command);
                                driver.Advance(10000000L,100);view.Refresh();
                            }
                        }else{
                            var ready=((JArray)hud["ultimates"]).FirstOrDefault(u=>(bool?)u["enabled"]==true&&(int?)u["charge"]==100);
                            if(ready!=null){row["readyActor"]=ready["actor"];await Click(inputs,mouse,ev,view,cam,driver,"Ultimate_"+(string)ready["actor"]);}
                            var samples=new JArray();row["chargeLight"]=samples;
                            for(int i=0;i<450 && driver.Outcome==BasicCombatOutcome.Running;i++){
                                var previous=((JArray)driver.HudState["ultimates"]).Where(u=>(int?)u["charge"]==100&&(bool?)u["enabled"]==true).Select(u=>(string)u["actor"]).ToArray();
                                driver.Advance(1000000,100);view.Refresh();
                                var fresh=((JArray)driver.HudState["ultimates"]).FirstOrDefault(u=>(int?)u["charge"]==100&&(bool?)u["enabled"]==true&&!previous.Contains((string)u["actor"]));
                                if(fresh==null){if(i%12==0)await Frame();continue;}
                                string actor=(string)fresh["actor"];var card=Find(view.transform,"OperatorCard_"+actor);
                                var wash=Find(card,"ready-wash").GetComponent<RawImage>();var flash=Find(card,"ready-flash").GetComponent<RawImage>();
                                double start=Time.unscaledTimeAsDouble;
                                JObject Light(string stage,double age,int charge)=>new JObject{["stage"]=stage,["actor"]=actor,["secondsFromEdge"]=age,["charge"]=charge,["wash"]=wash.color.a,["flash"]=flash.color.a};
                                var real=new JArray();row["observedFrames"]=real;
                                real.Add(Light("firstObserved",0,100));
                                while(Time.unscaledTimeAsDouble-start<.45){
                                    await Frame();await Awaitable.EndOfFrameAsync();
                                    real.Add(Light("actualPlayerLateUpdate",Time.unscaledTimeAsDouble-start,100));
                                }
                                double controlled=start;view.SetCapturePresentationClock(()=>controlled);
                                foreach(double offset in new[]{0,.10,.20,.27,.35,.40}){
                                    controlled=start+offset;view.Refresh();samples.Add(Light("controlledPresentationClock",offset,100));
                                }
                                view.SetCapturePresentationClock(()=>Time.unscaledTimeAsDouble);
                                await Click(inputs,mouse,ev,view,cam,driver,"Ultimate_"+actor);
                                driver.Advance(10000000L,100);view.Refresh();
                                int spent=(int)((JArray)driver.HudState["ultimates"]).Single(u=>(string)u["actor"]==actor)["charge"];
                                samples.Add(Light("after-manual-cast",Time.unscaledTimeAsDouble-start,spent));break;
                            }
                        }
                    }catch(Exception e){row["error"]=e.ToString();}
                    finally{host.SetActive(false);cam.enabled=false;UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(cam.gameObject);driver.Retire();await Frame();}
                }
            }finally{
                File.WriteAllText(Path.Combine(output,"CE03-input.json"),new JObject{["sourceFolder"]=folder,["preparationHash"]=preparation.PreparationSha256,["scope"]="SEPARATE_RETAINED_CE03_LOCAL_CHECKPOINTS_REAL_VIEW_AND_INPUT_NO_LIVE_API",["states"]=results}.ToString());
            }
        }
        internal static async Task Run(CombatPlayerAssetBinding binding,string output)
        {
            if(Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_COMPARISON")=="1"){
                await RunComparison(output);
                return;
            }
            var fixture=JObject.Parse(File.ReadAllText(NodePath));var routePacket=(JObject)fixture["binding"];
            var route=BRouteBinding.Read(routePacket.ToString(),(string)routePacket["packetHash"],(string)routePacket["accountId"],(string)routePacket["runId"]);
            var req=BRouteCommand.FromOriginal(route,(JObject)fixture["command"]);var arrival=BRouteCommandResult.Read(200,fixture["response"].ToString(),req);
            var original=OrdinaryFiveStartInput.Read((JObject)fixture["originalStart"],route.AccountId,route.RunId);
            var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,arrival.NodeStart,arrival.Scaling,arrival.NodeStart.ReadActions(original).StepTimeSpanTicks);
            var ev=UnityEngine.Object.FindFirstObjectByType<EventSystem>();var mouse=InputSystem.AddDevice<Mouse>("BattleDQAMouse");
            try{
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1260,540),new Vector2Int(960,720)}){
                    await SetSize(size.x,size.y);string prefix=Path.Combine(output,size.x+"x"+size.y);
                    var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
                    var host=new GameObject("BattleDOrdinaryProductPresentation");var scene=host.AddComponent<CombatPlayerScene>();
                    using var controller=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>true);
                    try{
                        controller.Present(scene,ev,()=>{},r=>{},e=>Debug.LogException(e));
                        for(int i=0;i<6;i++)await Frame();
                        var hud=driver.HudState;
                        File.WriteAllText(prefix+"-state.json",hud.ToString());
                        await Screenshot(prefix+"-battle.png");

                        var geometry=new JArray();
                        foreach(var rect in scene.View.GetComponentsInChildren<RectTransform>(false).Where(t=>Controls.Contains(t.name)||t.name.StartsWith("OperatorCard_")||t.name.StartsWith("General_")||t.name=="Wave"||t.name=="Header"||t.name=="TrainLayer")){
                            var b=Bounds(rect,scene.SceneCamera);geometry.Add(new JObject{["name"]=rect.name,["x"]=b.xMin,["y"]=b.yMin,["width"]=b.width,["height"]=b.height});
                        }
                        var rows=new JArray();
                        foreach(var name in new[]{"UltimateAuto","Speed","Pause","PauseContinue","TrainThrottle","BranchLever","ConfirmRailRoute","Command0","Command1","Command2","UnavailableSupport0","UnavailableSupport1","UnavailableSupport2"})
                            await Click(rows,mouse,ev,scene.View,scene.SceneCamera,driver,name,name=="TrainThrottle"?.89f:.5f);
                        foreach(var a in original.Actors.OrderBy(a=>a.Slot)){
                            await Click(rows,mouse,ev,scene.View,scene.SceneCamera,driver,"SelectOperator_"+a.OperatorId);
                            var marker=Find(Find(scene.View.transform,"OperatorCard_"+a.OperatorId),"selected-corners");
                            ((JObject)rows.Last)["selectedCorners"]=marker!=null&&marker.GetComponent<RawImage>().enabled;
                            if(a.OperatorId=="op_reina"){
                                await Screenshot(prefix+"-selected-reina.png");
                                await Click(rows,mouse,ev,scene.View,scene.SceneCamera,driver,"CloseOperatorInformation");
                            }
                        }
                        var result=new JObject{["source"]=NodePath,["snapshotHash"]=arrival.NodeStart.SnapshotHash,["manifest"]=binding.ManifestHash,["width"]=Screen.width,["height"]=Screen.height,["presentationRunning"]=scene.IsRunning,
                            ["fullReady"]=driver.IsExecutionReady,["module"]=ev.currentInputModule?.GetType().Name,["controls"]=geometry,["pointerInputs"]=rows};
                        File.WriteAllText(prefix+"-input.json",result.ToString());
                    }finally{controller.Dispose();UnityEngine.Object.Destroy(host);}
                    await Frame();
                }
                await SetSize(1280,720);
                await ActiveRail(output,ev,mouse);
            }finally{InputSystem.RemoveDevice(mouse);}
        }
        sealed class ComparisonField : IDisposable
        {
            public readonly Texture2D Refinery, Viaduct, EnemyEngine;
            public ComparisonField()
            {
                Refinery=Read("refinery");
                try{Viaduct=Read("viaduct");EnemyEngine=Read("enemy-engine",1774,887);}catch{Dispose();throw;}
            }
            static Texture2D Read(string name,int width=1672,int height=941)
            {
                var bytes=File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"BattleDField",name+".png"));
                var image=new Texture2D(2,2,TextureFormat.RGBA32,false){name=name,wrapMode=TextureWrapMode.Clamp};
                if(image.LoadImage(bytes,true) && image.width==width && image.height==height)return image;
                UnityEngine.Object.Destroy(image);
                throw new InvalidDataException("Invalid 848 comparison field texture: "+name);
            }
            public void Dispose(){UnityEngine.Object.Destroy(Refinery);UnityEngine.Object.Destroy(Viaduct);UnityEngine.Object.Destroy(EnemyEngine);}
        }
        static ApprovedCombatView BindComparison(GameObject host,Camera camera,CombatSceneDriver driver,CombatPresentationAssets art,ComparisonField field)
        {
            driver.Open();
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.black;
            var view=host.AddComponent<ApprovedCombatView>();
            view.Bind(driver,art);
            view.UseLandscapeLayout();
            view.UseComparisonField(field.Refinery,field.Viaduct,field.EnemyEngine);
            var canvas=host.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;
            canvas.planeDistance=1;
            return view;
        }
        internal static async Task InspectComparison()
        {
            using var field=new ComparisonField();
            var art=BattleDComparisonRoster.CreateArt();
            var driver=BattleDComparisonRoster.CreateDriver();
            var host=new GameObject("LocalComparisonInspector",typeof(RectTransform),typeof(Canvas));
            var camera=new GameObject("ComparisonCamera",typeof(Camera)).GetComponent<Camera>();
            try{
                BindComparison(host,camera,driver,art,field);
                while(!Application.exitCancellationToken.IsCancellationRequested)await Frame();
            }finally{
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(camera.gameObject);
                driver.Retire();
                BattleDComparisonRoster.DestroyArt(art);
            }
        }
        static async Task InspectShield(string prefix, CombatPresentationAssets art, ComparisonField field, bool expire)
        {
            string name=expire?"expiry":"zero";
            var driver=BattleDComparisonRoster.CreateDriver(expire?2800:0,expire?1L:long.MaxValue);
            var host=new GameObject("ShieldInspection",typeof(RectTransform),typeof(Canvas));
            var camera=new GameObject("ShieldInspectionCamera",typeof(Camera)).GetComponent<Camera>();
            var frames=new JArray();
            try{
                var view=BindComparison(host,camera,driver,art,field);
                var graphic=view.GetComponentsInChildren<CombatElectromagneticField>(true).Single();
                for(int i=0; i<(expire?2:1); i++){
                    if(i==1)driver.Advance((long)driver.HudState["stepTimeSpanTicks"],2);
                    // Let the production LateUpdate and Canvas rebuild submit their mesh.
                    await Frame();await Frame();await Awaitable.EndOfFrameAsync();
                    var hud=driver.HudState;
                    var train=hud["actors"].Single(a=>(bool)a["train"]);
                    var mesh=graphic.canvasRenderer.GetMesh();
                    int vertices=mesh?.vertexCount??0;
                    string image=Path.GetFileName(prefix)+"-shield-"+name+"-"+i+".png";
                    var row=new JObject{["tick"]=hud["tick"],["playerFrame"]=Time.frameCount,["train"]=train.DeepClone(),
                        ["roster"]=hud["ultimates"].DeepClone(),["hudHits"]=hud["hits"].DeepClone(),["field"]=graphic.Frame.DeepClone(),
                        ["gameObject"]=graphic.name,["graphicEnabled"]=graphic.isActiveAndEnabled,["meshPresent"]=mesh!=null,
                        ["canvasVertices"]=vertices,["image"]=image};
                    frames.Add(row);
                    var captured=ScreenCapture.CaptureScreenshotAsTexture();
                    try{File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(prefix),image),captured.EncodeToPNG());}
                    finally{UnityEngine.Object.Destroy(captured);}
                    int expected=expire && i==0?2800:0;
                    bool on=expected>0;
                    row["passed"]=(long)hud["tick"]==i && (int)train["shield"]==expected && (int)train["hp"]==4800 &&
                        graphic.isActiveAndEnabled && (int)graphic.Frame["shield"]==expected && (bool)graphic.Frame["active"]==on &&
                        (on?vertices>0:vertices==0) && !hud["hits"].Any(h=>(string)h["Target"]==(string)train["id"]);
                    if(!(bool)row["passed"])throw new InvalidOperationException("Comparison shield frame mismatch: "+image);
                }
            }finally{
                host.SetActive(false);camera.enabled=false;
                UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(camera.gameObject);driver.Retire();
                File.WriteAllText(prefix+"-shield-"+name+".json",new JObject{
                    ["fixture"]=expire?"Original local shield layer expires at tick 1":"Same tick-0 roster without a granted shield layer",
                    ["readModel"]="CombatSceneDriver.HudState / BasicCombatSimulation.ReadHudState",["frames"]=frames}.ToString());
                await Frame();
            }
        }
        static async Task RunComparison(string output)
        {
            using var field=new ComparisonField();
            var ev=UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            var mouse=InputSystem.AddDevice<Mouse>("BattleDComparisonMouse");
            bool inspectShield=Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_HONEYCOMB_QA")=="1";
            var art=BattleDComparisonRoster.CreateArt();
            try{
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1260,540),new Vector2Int(960,720)}){
                    await SetSize(size.x,size.y);
                    string prefix=Path.Combine(output,size.x+"x"+size.y);
                    var driver=BattleDComparisonRoster.CreateDriver();
                    var host=new GameObject("ComparisonView",typeof(RectTransform),typeof(Canvas));
                    var camera=new GameObject("ComparisonCamera",typeof(Camera)).GetComponent<Camera>();
                    try{
                        var view=BindComparison(host,camera,driver,art,field);
                        // Let the real ready-card entrance finish while the comparison model stays at its saved tick.
                        double settle=Time.unscaledTimeAsDouble+.5;
                        do{await Frame();}while(Time.unscaledTimeAsDouble<settle);
                        File.WriteAllText(prefix+"-state.json",driver.HudState.ToString());
                        await Screenshot(prefix+"-battle.png");
                        if(inspectShield){
                            var shell=view.GetComponentInChildren<CombatElectromagneticField>();
                            shell.SetComparisonCellsVisible(false);
                            try{await Frame();await Screenshot(prefix+"-outline-control.png");}
                            finally{shell.SetComparisonCellsVisible(true);}
                        }
                        var rows=new JArray();
                        foreach(var id in new[]{"reina","kyle","eve","sera","nyx"}){
                            await Click(rows,mouse,ev,view,camera,driver,"SelectOperator_op_"+id);
                            if(id=="reina")await Screenshot(prefix+"-reina-selected.png");
                            await Click(rows,mouse,ev,view,camera,driver,"CloseOperatorInformation");
                        }
                        foreach(var name in new[]{"Speed","Pause","PauseContinue","UltimateAuto","TrainThrottle","BranchLever","ConfirmRailRoute","Command0","Command1","Command2"})
                            await Click(rows,mouse,ev,view,camera,driver,name,name=="TrainThrottle"?.87f:.5f);
                        File.WriteAllText(prefix+"-input.json",rows.ToString());
                    }finally{
                        host.SetActive(false);
                        camera.enabled=false;
                        UnityEngine.Object.Destroy(host);
                        UnityEngine.Object.Destroy(camera.gameObject);
                        driver.Retire();
                    }
                    await Frame();
                    if(inspectShield){
                        await InspectShield(prefix,art,field,false);
                        await InspectShield(prefix,art,field,true);
                    }
                }
                if(inspectShield)return;
                await SetSize(1280,720);
                if(Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_EVENT_CAPTURE")=="1"){
                    await BattleDArtilleryEvidence.Run(output,ev,mouse);
                    await BattleDFocusEvidence.Run(output,ev,mouse);
                }else await BattleDReinaFrames.Run(output,ev,mouse);
            }finally{
                BattleDComparisonRoster.DestroyArt(art);
                InputSystem.RemoveDevice(mouse);
            }
        }
    }
}
#endif
