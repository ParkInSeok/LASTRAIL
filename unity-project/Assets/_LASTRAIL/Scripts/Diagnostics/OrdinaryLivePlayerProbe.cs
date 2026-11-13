#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Diagnostics {
 // Explicit local Player exercise: admission/source/accepted inputs come from the persistent loopback server.
 public sealed class OrdinaryLivePlayerProbe:MonoBehaviour {
  [SerializeField] CombatPlayerAssetBinding content;
  public void Initialize(CombatPlayerAssetBinding binding){content=binding??throw new ArgumentNullException(nameof(binding));}
  static JObject Json(string text){using var reader=new JsonTextReader(new StringReader(text)){DateParseHandling=DateParseHandling.None};return JObject.Load(reader);}
  static void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
  static string ProjectPath(string value){var path=Path.GetFullPath(value??throw new ArgumentException("Explicit D path required."));Need(path.StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase),"Project-local path required.");return path;}
  async void Start(){
   string output=null;bool gate=true,current=true;OrdinaryNodePreparationOwner owner=null;OrdinaryPlayerBattleController controller=null;OrdinaryLocalExecutionSession session=null;UnityWebRequestRunApiHttpAdapter adapter=null;
   var events=new JArray();
   try{
    Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
    output=ProjectPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_QA"));Need(!Directory.Exists(output),"Fresh QA output required.");Directory.CreateDirectory(output);
    var bootstrap=Json(File.ReadAllText(ProjectPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_BOOTSTRAP"))));
    Need((string)bootstrap["version"]=="ordinary.live-local-session.v1.20261106"&&(string)bootstrap["classification"]=="ISOLATED_LOCAL_AUTHORED_GAME_EXECUTION"&&(long)bootstrap["expiresAt"]>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Current isolated local session required.");
    string url=(string)bootstrap["baseUrl"];var origin=new Uri(url);Need(origin.Scheme=="http"&&origin.IsLoopback&&(string)bootstrap["sourcePath"]==OrdinaryLocalExecutionSession.EndpointPath,"Loopback source required.");
    var access=new GameApiAccessToken((string)bootstrap["accessToken"]);bootstrap.Remove("accessToken");
    adapter=new UnityWebRequestRunApiHttpAdapter(url,60);var transport=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));
    long sourceLastSequence=-1;int sourceRead=0;
    async Task<JObject> GetSource(){
     var response=await transport.SendAsync(new RunApiHttpRequest("GET",OrdinaryLocalExecutionSession.EndpointPath,null,new RunApiHttpHeader[0]),access);
     Need(response.Outcome==GameApiAuthenticatedHttpOutcome.Succeeded&&response.Response?.StatusCode==200,"Actual source HTTP request failed.");
     var result=Json(response.Response.BodyJson);Need((bool?)result["accepted"]==true&&result["source"] is JObject&&result["lastSequence"]?.Type==JTokenType.Integer,"Persisted local source unavailable.");
     File.WriteAllText(Path.Combine(output,"source-response-"+(sourceRead++)+".json"),response.Response.BodyJson);sourceLastSequence=(long)result["lastSequence"];Need(sourceLastSequence>=0,"Invalid accepted prefix.");
     // GET owns immutable source material, not a copy of the client's command prefix.
     var material=(JObject)result["source"].DeepClone();Need(material.Property("inputs")==null,"Unexpected source input owner.");material["inputs"]=new JArray();return material;
    }
    var input=await GetSource();var source=OrdinaryLocalExecutionReplay.ReadInput(input);
    Need(source.Attempt.AccountId==(string)bootstrap["accountId"]&&source.Attempt.RunId==(string)bootstrap["runId"],"Bootstrap/source scope differs.");
    if(sourceLastSequence!=0){
     var restart=new OrdinaryNodeRestart(source.Node,Guid.NewGuid().ToString("N"),source.Attempt.Revision,source.Attempt.Generation);
     File.WriteAllText(Path.Combine(output,"restart-request.json"),new JObject{["key"]=restart.Key,["path"]=restart.EndpointPath,["body"]=Json(restart.BodyJson)}.ToString());
     var request=new RunApiHttpRequest("POST",restart.EndpointPath,restart.BodyJson,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("Idempotency-Key",restart.Key)});
     var response=await transport.SendAsync(request,access);
     if(response.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded||response.Response?.StatusCode!=200)response=await transport.SendAsync(request,access);
     Need(response.Outcome==GameApiAuthenticatedHttpOutcome.Succeeded&&response.Response?.StatusCode==200,"Restart unresolved; original request retained.");
     var accepted=restart.ReadResponse(response.Response.BodyJson);Need(!accepted.ResultAvailable,"Previous result must be recovered first.");
     File.WriteAllText(Path.Combine(output,"restart-response.json"),response.Response.BodyJson);
     input=await GetSource();source=OrdinaryLocalExecutionReplay.ReadInput(input);Need(sourceLastSequence==0&&source.Attempt.Generation==accepted.Generation,"Restart source differs.");
    }
    File.WriteAllText(Path.Combine(output,"source.json"),input.ToString());File.WriteAllText(Path.Combine(output,"session-reference.json"),bootstrap.ToString());
    owner=new OrdinaryNodePreparationOwner(source,()=>current);var driver=await owner.PrepareAsync();Need(driver.IsExecutionReady,"Actual execution sources are not ready.");
    Directory.CreateDirectory(Path.Combine(output,"requests"));
    session=new OrdinaryLocalExecutionSession(driver,source.Attempt,transport,url,()=>access,()=>current,b=>File.WriteAllText(Path.Combine(output,"requests",b.Key+".json"),new JObject{["key"]=b.Key,["body"]=Json(b.BodyJson)}.ToString()));
    async Task Flush(){
     try{
     var receipt=await session.FlushAsync();if(receipt==null)receipt=await session.FlushAsync();
     Need(receipt!=null,"Execution receipt unresolved; pending request retained in process.");
     events.Add(new JObject{["requestId"]=receipt["requestId"].DeepClone(),["sequence"]=receipt["result"]["lastSequence"].DeepClone(),["tick"]=receipt["result"]["tick"].DeepClone(),["checkpointHash"]=Ce03CommandContract.Sha(receipt["result"]["checkpoint"].ToString(Formatting.None))});
     File.WriteAllText(Path.Combine(output,"latest-receipt.json"),receipt.ToString());
     }catch{gate=true;throw;}
    }
    await Flush();gate=false;
    var binding=content!=null?content:LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");Need(binding!=null,"Original presentation binding missing.");
    File.WriteAllText(Path.Combine(output,"presentation-binding.json"),binding.ManifestJson);
    var host=new GameObject("OrdinaryLiveCombatPlayer").AddComponent<CombatPlayerScene>();Exception failed=null;
    controller=new OrdinaryPlayerBattleController(source.Original,driver,binding,()=>current,()=>!gate&&session.CanAcceptMoreInputs);
    controller.Present(host,EventSystem.current,()=>{},r=>{},e=>failed=e);
    while(!host.IsRunning&&failed==null)await Awaitable.NextFrameAsync();if(failed!=null)throw failed;
    int throttle=int.Parse(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_THROTTLE")??"7000");Need(throttle==7000||throttle==13000,"Slow or fast case required.");
    // Exercise the existing View callback; all execution and input capture still belong to the product driver.
    var slider=host.View.GetComponentsInChildren<Slider>(true).Single(s=>s.name=="TrainThrottle");slider.value=(throttle-7000)/6000f;
    Need((long)driver.HudState["ordinaryRail"]["targetSpeedMmPerSecond"]==throttle*2,"Original throttle callback did not apply.");
    var controls=new JArray();var pause=host.View.GetComponent<CombatPauseController>();pause.Open();long pausedTick=driver.Tick;
    for(int i=0;i<10;i++)await Awaitable.NextFrameAsync();Need(driver.Tick==pausedTick,"Pause advanced the original logical clock.");
    controls.Add(new JObject{["kind"]="PAUSE",["tick"]=pausedTick,["endTick"]=driver.Tick});await Flush();
    // The product host must render its resumed state before it accepts View commands.
    do{await Awaitable.NextFrameAsync();if(failed!=null)throw failed;}while(!host.IsRunning);
    pause.RequestBack();
    var speedButton=host.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Speed");
    foreach(int speed in new[]{3,4,2}){speedButton.onClick.Invoke();for(int i=0;i<3;i++)await Awaitable.NextFrameAsync();Need((int)driver.HudState["speedHalfUnits"]==speed,"Existing speed callback failed.");controls.Add(new JObject{["kind"]="SPEED",["halfUnits"]=speed,["tick"]=driver.Tick});}
    async Task Capture(string name){await Awaitable.EndOfFrameAsync();var texture=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());}finally{Destroy(texture);}File.WriteAllText(Path.Combine(output,name+"-hud.json"),driver.HudState.ToString());}
    bool recordFrames=true;long sequenceWave=-1;int imageIndex=0;string sequencePath=Path.Combine(output,"sequence");Directory.CreateDirectory(sequencePath);
    async Task RecordFrames(){
     try{while(current&&recordFrames){
      await Awaitable.EndOfFrameAsync();if(!current||!recordFrames)return;
      var hud=driver.HudState;long tick=driver.Tick;
      if((string)hud["ordinaryObstacle"]?["phase"]=="COMBAT"&&sequenceWave<0)sequenceWave=tick;
      if(tick<(throttle==7000?340:280)||sequenceWave>=0&&tick>sequenceWave+70||Time.frameCount%3!=0)continue;
      string file=(imageIndex++).ToString("D5")+".jpg";var texture=ScreenCapture.CaptureScreenshotAsTexture();
      try{File.WriteAllBytes(Path.Combine(sequencePath,file),texture.EncodeToJPG(85));}finally{Destroy(texture);}
      var sample=new JObject{["file"]=file,["frame"]=Time.frameCount,["realtimeSeconds"]=Time.realtimeSinceStartupAsDouble,["tick"]=tick,["transportGate"]=gate,["inputCapacity"]=session.CanAcceptMoreInputs,["inFlight"]=session.InFlight,
       ["rail"]=hud["ordinaryRail"].DeepClone(),["obstacle"]=hud["ordinaryObstacleExecution"].DeepClone(),["phase"]=hud["ordinaryObstacle"]["phase"].DeepClone(),
       ["actors"]=new JArray(((JArray)hud["actors"]).Select(a=>new JObject{["id"]=a["id"].DeepClone(),["hp"]=a["hp"].DeepClone(),["foot"]=a["footRelativeToFrontMm"]?.DeepClone()})),["shots"]=hud["ordinaryGunnerProjectiles"]?.DeepClone()};
      File.AppendAllText(Path.Combine(sequencePath,"frames.jsonl"),sample.ToString(Formatting.None)+Environment.NewLine);
     }}catch(Exception e){failed=e;}
    }
    var sequenceTask=RecordFrames();
    await Capture("entry");long lastFlush=driver.Tick;long waveAt=-1;Task flushing=null;bool damaged=false,destroyed=false,falling=false,gunnerShot=false,gunnerHit=false;float deadline=Time.realtimeSinceStartup+240;
    while(waveAt<0||driver.Tick<waveAt+240){
     await Awaitable.NextFrameAsync();if(failed!=null)throw failed;Need(Time.realtimeSinceStartup<deadline,"Actual Player deadline exceeded.");
     var state=driver.HudState;Need(driver.Outcome==BasicCombatOutcome.Running,"Train was defeated during live case.");
     if(!damaged&&(int?)state["ordinaryObstacleExecution"]?["hp"]<100){damaged=true;await Capture("obstacle-hit");}
     if(!falling&&(string)state["ordinaryObstacleExecution"]?["phase"]=="FALLING"){falling=true;await Capture("obstacle-falling");}
     if(!destroyed&&(int?)state["ordinaryObstacleExecution"]?["hp"]==0){destroyed=true;await Capture("obstacle-destroyed");}
     if(waveAt<0&&(string)state["ordinaryObstacle"]?["phase"]=="COMBAT"){waveAt=driver.Tick;await Capture("next-wave");}
     if(state["ordinaryGunnerProjectiles"] is JArray gunnerShots){
      if(!gunnerShot&&gunnerShots.Any(s=>(string)s["status"]=="FLYING")){gunnerShot=true;await Capture("first-gunner-shot");}
      if(!gunnerHit&&gunnerShots.Any(s=>(string)s["status"]=="HIT")){gunnerHit=true;await Capture("first-gunner-hit");}
     }
     if(flushing!=null&&flushing.IsCompleted){await flushing;flushing=null;}
     if(flushing==null&&(driver.Tick-lastFlush>=20||!session.CanAcceptMoreInputs)){lastFlush=driver.Tick;flushing=Flush();}
    }
    gate=true;if(flushing!=null)await flushing;await Flush();recordFrames=false;await sequenceTask;if(failed!=null)throw failed;await Capture("wave-progress");
    var final=Json(driver.StateJson);string expected=throttle==7000?"DESTROYED":"PASSED";
    Need((string)final["obstacleSequence"]["resolution"]["outcome"]==expected,"Actual obstacle result differs.");
    Need(final["gunnerExecution"]!=null&&final["gunnerMotion"]!=null,"Original wave owners missing.");
    File.WriteAllText(Path.Combine(output,"checkpoint.json"),driver.StateJson);
    File.WriteAllText(Path.Combine(output,"result.json"),new JObject{["status"]="LIVE_OBSTACLE_AND_NEXT_WAVE_VERIFIED",["scope"]="ACTUAL_ISOLATED_SERVER_PRODUCT_PREPARATION_MODEL_CONTROLLER_PLAYER",["inputMethod"]="EXISTING_VIEW_CALLBACKS_PROGRAMMATICALLY_INVOKED",["controls"]=controls,["throttle"]=throttle,["width"]=Screen.width,["height"]=Screen.height,["waveAtTick"]=waveAt,["endTick"]=driver.Tick,["attemptGeneration"]=source.Attempt.Generation,["outcome"]=expected,["receipts"]=events,["nodeCompletionVerified"]=false}.ToString());
    if(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_HOLD")=="1"){
     gate=false;pause.Open();lastFlush=driver.Tick;
     File.WriteAllText(Path.Combine(output,"manual-ready.json"),new JObject{["status"]="PAUSED_PRODUCT_PLAYER_FOR_POINTER_REVIEW",["tick"]=driver.Tick,["bootstrapPath"]=Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_BOOTSTRAP"),["scope"]="EXISTING_FIVE_WITH_KYLE_ART_NOT_NYX_START"}.ToString());
     while(current){await Awaitable.NextFrameAsync();if(failed!=null)throw failed;if(driver.Tick-lastFlush>=20||!session.CanAcceptMoreInputs){lastFlush=driver.Tick;await Flush();}}
    }
    Application.Quit(0);
   }catch(Exception error){if(output!=null&&Directory.Exists(output)){
    File.WriteAllText(Path.Combine(output,"failure.json"),new JObject{["type"]=error.GetType().Name,["message"]=error.Message,["stack"]=error.StackTrace,["receipts"]=events}.ToString());
    if(session?.Pending!=null)File.WriteAllText(Path.Combine(output,"pending-checkpoint.json"),session.Pending.Checkpoint.ToString());
    if(session?.LastTransportResult?.Response!=null)File.WriteAllText(Path.Combine(output,"last-response.json"),session.LastTransportResult.Response.BodyJson);
   }Application.Quit(2);}
   finally{current=false;session?.Dispose();controller?.Dispose();owner?.Dispose();adapter?.Dispose();}
  }
 }
}
#endif
