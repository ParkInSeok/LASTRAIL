using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
 public sealed class BOrdinaryLiveQaBootstrap:MonoBehaviour {
  public TextAsset fixture;public string fixtureHash;
  BOrdinaryLiveQaController controller;BOrdinaryLiveQaView view;
  public BOrdinaryLiveQaController Controller=>controller;
  public string Failure {get;private set;}
  void OnEnable(){try{Failure=null;controller=BOrdinaryLiveQaController.Read(JObject.Parse(fixture.text),fixtureHash);view=gameObject.AddComponent<BOrdinaryLiveQaView>();view.Bind(controller.Geometry,controller.Capture,()=>controller.Outcome,()=>controller.Paused,()=>controller.Hp,StartQa,()=>controller.SetPaused(!controller.Paused),RestartQa,controller.MoveKyle,()=>controller.Intercept());}catch(Exception e){Failure=e.Message;Debug.LogException(e,this);enabled=false;}}
  public void StartQa(){controller.Start();}
  public void RestartQa(){controller.Restart();}
  void Update(){if(controller==null||!controller.Started)return;try{controller.SubmitElapsed(TimeSpan.FromSeconds(Time.unscaledDeltaTime).Ticks,view.Demonstration);}catch(Exception e){Failure=e.Message;Debug.LogException(e,this);enabled=false;}}
  void OnDisable(){if(view!=null){view.Unbind();Destroy(view);view=null;}controller?.Dispose();controller=null;}
 }
}
