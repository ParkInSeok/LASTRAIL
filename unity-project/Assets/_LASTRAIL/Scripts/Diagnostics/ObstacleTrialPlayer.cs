#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LastRail.Diagnostics {
 public static class ObstacleTrialPlayer {

  static async Task Capture(string path){
   await Awaitable.EndOfFrameAsync();var image=ScreenCapture.CaptureScreenshotAsTexture();
   try{File.WriteAllBytes(path,image.EncodeToPNG());}finally{UnityEngine.Object.Destroy(image);}
  }
  static void Need(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
  static async Task Click(ObstacleTrialView view,string name,Mouse mouse,JArray log,ObstacleTrialController controller){
   var rect=view.Control(name);var corners=new Vector3[4];rect.GetWorldCorners(corners);var point=(corners[0]+corners[2])/2;
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
   Need(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(rect),"Button not reached: "+name);
   void Queue(bool down)=>InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left,down));
   Queue(false);await Awaitable.NextFrameAsync();await Awaitable.NextFrameAsync();
   Queue(true);await Awaitable.NextFrameAsync();await Awaitable.NextFrameAsync();
   Queue(false);await Awaitable.NextFrameAsync();await Awaitable.NextFrameAsync();
   log.Add(new JObject{["button"]=name,["point"]=new JArray(point.x,point.y),["target"]=controller.Model.TargetSpeed,["paused"]=controller.Paused,["rate"]=controller.Rate,["timeMs"]=controller.Model.Milliseconds});
  }
  public static async Task Run(string output){
   Need(!Directory.Exists(output),"Fresh output required");Directory.CreateDirectory(output);
   var source=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/HpOwner839/node-source.json"));
   var consist=source["originalStart"]["ordinaryRailSource"]["packet"]["physicalConsist"];
   Need((int)consist["totalLengthMm"]==38000&&(int)consist["initialFrontArcMm"]==58000,"Original consist");
   var ids=((JArray)consist["instanceIds"]).Values<string>().ToArray();
   int defense=(int)source["originalStart"]["ordinaryTrainSource"]["packet"]["permanentStats"]["defense"];
   int health=(int)source["originalStart"]["ordinaryTrainSource"]["packet"]["permanentStats"]["maxHp"];
   var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation");
   var roof=LastRail.Resources.PreparedContent.Load<OrdinaryRoofPresentationAssets>("CombatPresentation/OrdinaryRoof");
   Need(art!=null&&roof!=null,"Original presentation resources");
   File.WriteAllText(Path.Combine(output,"source.json"),new JObject{["scope"]="APPROVED_LOCAL_OBSTACLE_TRIAL_NOT_PRODUCT_ADMISSION",["originalConsist"]=consist.DeepClone(),["defense"]=defense,["trainHp"]=health,["trackLengthMm"]=600000,["socketBehindFrontMm"]=6000,["socketHeightMm"]=4000,["obstacleSizeMm"]=new JArray(6000,6000,6000),["vehicleHeightMm"]=3200,["vehicleWidthMm"]=3000,["fallStartMs"]=15400,["hazardMs"]=16000,["landingMs"]=16320,["initialCenterYmm"]=12200,["fallSpeedMmPerSecond"]=10000,["viewPixelsPerMeter"]=10.5,["background"]=art.MapId,["geometryRole"]="RUNTIME_AUTHORED_TRIAL_VOLUMES_NOT_PRODUCTION_ART",["sourceHash"]=Ce03CommandContract.Sha(source.ToString())}.ToString());
   var mouse=InputSystem.AddDevice<Mouse>("ObstacleTrialMouse");
   var summary=new JArray();
   try{
    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1260,540),new Vector2Int(960,720)}){
     Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
     for(int i=0;i<120&&(Screen.width!=size.x||Screen.height!=size.y);i++)await Awaitable.NextFrameAsync();
     Need(Screen.width==size.x&&Screen.height==size.y,"Resolution");
     foreach(string scenario in new[]{"slow","fast","rear-shield","horizontal-partial","defeat"}){
      int shots=scenario=="horizontal-partial"?2:scenario=="defeat"?0:int.MaxValue;
      bool grounded=scenario=="horizontal-partial";
      string key=size.x+"x"+size.y+"-"+scenario;
      Func<ObstacleTrialModel> factory=()=>new ObstacleTrialModel(key,ids,defense,scenario=="defeat"?100:health,scenario=="rear-shield"?300:0,shots,grounded);
      using var controller=new ObstacleTrialController(factory);
      var go=new GameObject("ObstacleTrial",typeof(RectTransform));
      var view=go.AddComponent<ObstacleTrialView>();view.Bind(controller,art,roof);
      var inputs=new JArray();var samples=new JArray();var frames=new JArray();
      try{
       await Awaitable.NextFrameAsync();await Awaitable.NextFrameAsync();
       await Click(view,"Rate",mouse,inputs,controller);Need(controller.Rate==1.5f,"1.5 rate");
       await Click(view,"Rate",mouse,inputs,controller);Need(controller.Rate==2,"2 rate");
       if(size.x==1280){await Click(view,"Rate",mouse,inputs,controller);Need(controller.Rate==1,"1 rate");}
       await Click(view,"Pause",mouse,inputs,controller);
       controller.Advance(.25f);Need(controller.Model.Milliseconds==0,"Paused simulation moved");
       await Click(view,"Pause",mouse,inputs,controller);
       controller.Advance(.5f/controller.Rate);view.Refresh();
       var old=controller.Model;
       await Click(view,"Reenter",mouse,inputs,controller);Need(controller.Model.Milliseconds==0&&controller.Model.Hp==100,"Reentry");
       bool rejected=false;try{old.Advance(1);}catch(ObjectDisposedException){rejected=true;}Need(rejected,"Old attempt remained active");
       controller.Advance(1/controller.Rate);
       await Click(view,scenario=="slow"?"Low":scenario=="fast"?"High":"Cruise",mouse,inputs,controller);
       view.Refresh();await Capture(Path.Combine(output,key+"-start.png"));
       double nextFrame=14000;int frameNumber=0,lastEvents=0;bool flightRestarted=false,stallInjected=false;var priorAttempts=new JArray();double lastWall=Time.realtimeSinceStartupAsDouble;double maxRealDelta=0;
       // Advance at recorded real frame deltas; long frames are subdivided by the model's event grid.
       while(!controller.Model.Finished&&controller.Model.Milliseconds<28000){
        await Awaitable.NextFrameAsync();
        if(!stallInjected&&controller.Model.Milliseconds>=(grounded?13800:15900)){stallInjected=true;await Task.Delay(250);}
        double now=Time.realtimeSinceStartupAsDouble;float elapsed=(float)(now-lastWall);lastWall=now;maxRealDelta=Math.Max(maxRealDelta,elapsed);
        controller.Advance(elapsed);view.Refresh();
        var m=controller.Model;var state=m.Capture();
        if(m.Milliseconds/50>samples.Count)samples.Add(new JObject{["atMs"]=m.Milliseconds,["frontMm"]=m.Front,["speed"]=m.Speed,["hp"]=m.Hp,["trainHp"]=m.TrainHp});
        if(scenario=="slow"&&!flightRestarted&&m.Shots.Any(s=>s.Flying)){
         flightRestarted=true;await Click(view,"Pause",mouse,inputs,controller);
         var frozen=m.Capture();controller.Advance(.25f);Need(JToken.DeepEquals(frozen,m.Capture()),"Flight moved while paused");
         await Capture(Path.Combine(output,key+"-paused-flight.png"));
         await Click(view,"Pause",mouse,inputs,controller);
         priorAttempts.Add(m.Capture());
         await Click(view,"Reenter",mouse,inputs,controller);
         bool staleRejected=false;try{m.Advance(1);}catch(ObjectDisposedException){staleRejected=true;}
         Need(staleRejected&&controller.Model.Shots.Count==0&&controller.Model.Hp==100&&controller.Model.TrainHp==health,"Flying attempt leaked into reentry");
         controller.Advance(1/controller.Rate);await Click(view,"Low",mouse,inputs,controller);view.Refresh();
         lastEvents=0;nextFrame=14000;lastWall=Time.realtimeSinceStartupAsDouble;continue;
        }
        int count=((JArray)state["events"]).Count;
        bool capture=count!=lastEvents||m.Milliseconds>=nextFrame&&m.Milliseconds<=18500;
        if(capture){string file=key+"-"+(frameNumber++).ToString("D3")+".png";await Capture(Path.Combine(output,file));frames.Add(new JObject{["file"]=file,["atMs"]=m.Milliseconds,["attempt"]=priorAttempts.Count});nextFrame=m.Milliseconds+125;lastEvents=count;}
       }
       Need(controller.Model.Finished,"Unresolved obstacle");
       var result=controller.Model.Capture();result["injectedRenderGapMs"]=250;result["maximumObservedRealFrameDeltaSeconds"]=maxRealDelta;result["trialMaximumShots"]=shots;result["trialInitiallyLanded"]=grounded;result["trialInitialTrainHp"]=scenario=="defeat"?100:health;result["trialEligibleShield"]=scenario=="rear-shield"?300:0;result["inputs"]=inputs;result["priorAttempts"]=priorAttempts;result["frames"]=frames;result["samples"]=samples;
       File.WriteAllText(Path.Combine(output,key+".json"),result.ToString());
       await Capture(Path.Combine(output,key+"-end.png"));
       summary.Add(new JObject{["case"]=key,["finished"]=true,["trainHp"]=controller.Model.TrainHp,["obstacleHp"]=controller.Model.Hp,["timeMs"]=controller.Model.Milliseconds});
      }finally{UnityEngine.Object.Destroy(go);await Awaitable.NextFrameAsync();}
     }
    }
    File.WriteAllText(Path.Combine(output,"summary.json"),summary.ToString());
   }finally{InputSystem.RemoveDevice(mouse);}
  }
 }
}
#endif
