#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 public sealed class SceneMemoryLifecycleProbe:MonoBehaviour {
  [SerializeField] TextAsset source;[SerializeField] FourAgentV03Assets art;[SerializeField] TMP_FontAsset font;GameObject lobby,battle;
  public void Initialize(TextAsset original,FourAgentV03Assets catalog,TMP_FontAsset suppliedFont){source=original;art=catalog;font=suppliedFont;}
  public IEnumerator Run(SceneMemoryRecorder recorder){
   recorder.BeginWindow();lobby=new GameObject("IsolatedUnboundLobbyMemory",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));lobby.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=lobby.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.matchWidthOrHeight=1;
   var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(lobby.transform,false);var rect=safe.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   LobbyCommandDeck.Attach(lobby,font,"LOCAL MEMORY QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);
   yield return new WaitForSecondsRealtime(4);recorder.Checkpoint("01-isolated-lobby-cold");
   for(int cycle=0;cycle<6;cycle++){
    recorder.BeginWindow();lobby.SetActive(false);battle=new GameObject("MemoryEveLocal"+cycle);var scene=battle.AddComponent<RoofBattleLocalScene>();
    scene.Initialize(source,BDirectAttackInput.Digest(JObject.Parse(source.text)),"memory-local-"+cycle,null,font,recorder.Output+"/native-store-"+cycle);scene.ConfigureV03(art);scene.SetAutoAdvance(false);
    yield return new WaitForSecondsRealtime(3);if(!scene.Ready)throw new InvalidOperationException("Original Eve LOCAL not ready.");
    scene.AdvanceManual(10000,2);if((long)scene.Snapshot["tick"]!=1)throw new InvalidOperationException("Original Eve step missing.");
    recorder.Checkpoint("cycle-"+cycle+"-eve-local-loaded");recorder.BeginWindow();Destroy(battle);battle=null;lobby.SetActive(true);
    yield return new WaitForSecondsRealtime(4);recorder.Checkpoint("cycle-"+cycle+"-lobby-return");
   }
  }
  void OnDestroy(){if(battle!=null)Destroy(battle);if(lobby!=null)Destroy(lobby);}
 }
}
#endif
