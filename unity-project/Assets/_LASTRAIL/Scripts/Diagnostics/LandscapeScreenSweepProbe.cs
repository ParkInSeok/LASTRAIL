#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 public sealed class LandscapeScreenSweepProbe:MonoBehaviour {
  [SerializeField] GameObject[] screens;[SerializeField] TMP_FontAsset font;
  public void Initialize(GameObject[] values,TMP_FontAsset supplied){screens=values;font=supplied;}
  async void Start(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_LANDSCAPE_SWEEP");if(string.IsNullOrEmpty(output))return;
   SceneMemoryRecorder memory=null;GameObject current=null;
   try{
    output=Path.GetFullPath(output);if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);
    Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
    var events=new GameObject("SweepInput",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    memory=SceneMemoryRecorder.CreateExternal(output,true);var rows=new JArray();var sizes=new[]{new Vector2Int(1600,900),new Vector2Int(2100,900),new Vector2Int(1200,900)};
    for(int cycle=0;cycle<3;cycle++){
     Screen.SetResolution(sizes[cycle].x,sizes[cycle].y,false);await Task.Delay(500);memory.BeginWindow();memory.Checkpoint(cycle+"-before",continueWindow:true);
     current=new GameObject("SweepEntry",typeof(RectTransform));var entry=current.AddComponent<PlayerEntryPanel>();entry.Initialize(font);
     entry.ShowLogin(true,true);await Capture(output,cycle+"-login");
     var id=Array.Find(current.GetComponentsInChildren<Button>(true),b=>b.name=="ChooseIdLogin");id.onClick.Invoke();await Capture(output,cycle+"-credentials");
     entry.ShowNickname();await Capture(output,cycle+"-nickname");entry.ShowError("연결을 확인한 뒤 다시 시도해 주세요.",true);await Capture(output,cycle+"-login-error");
     Destroy(current);current=null;await Task.Delay(150);
     foreach(var prefab in screens){current=Instantiate(prefab);if(prefab.name=="MissionShellScreen")LastRail.Presentation.MissionShell.LobbyCommandDeck.Attach(current,font,"LOCAL DISPLAY QA",0,Array.Empty<LastRail.Presentation.MissionShell.MissionShellMissionRowState>(),null,null);await Capture(output,cycle+"-"+prefab.name);rows.Add(new JObject{["cycle"]=cycle,["screen"]=prefab.name,["width"]=Screen.width,["height"]=Screen.height});
      var deck=current.GetComponentInChildren<LastRail.Presentation.MissionShell.LobbyCommandDeck>(true);if(deck!=null)foreach(LastRail.Presentation.MissionShell.LobbyPage page in Enum.GetValues(typeof(LastRail.Presentation.MissionShell.LobbyPage))){deck.OpenPage(page);await Capture(output,cycle+"-page-"+page);deck.RequestBack();if(deck.Pages.IsOpen)throw new InvalidOperationException("Page back did not return to lobby");}
      if(deck!=null){deck.SetEditorVisualFixture();foreach(LastRail.Presentation.MissionShell.LobbyPage page in Enum.GetValues(typeof(LastRail.Presentation.MissionShell.LobbyPage))){deck.OpenPage(page);await Capture(output,cycle+"-synthetic-page-"+page);if(page==LastRail.Presentation.MissionShell.LobbyPage.Operators){var details=Array.Find(deck.Pages.GetComponentsInChildren<Button>(),b=>b.name=="OpenMemberDetails");if(details==null)throw new InvalidOperationException("Synthetic notice source missing");for(int repeat=0;repeat<3;repeat++)details.onClick.Invoke();deck.RequestBack();if(!deck.Pages.IsOpen)throw new InvalidOperationException("Notice back closed parent");}deck.RequestBack();if(deck.Pages.IsOpen)throw new InvalidOperationException("Synthetic page back did not return");}
       int baselineGroups=deck.GetComponentsInChildren<CanvasGroup>(true).Length;for(int n=0;n<3;n++){deck.OpenOperatorCodex();deck.RequestBack();var codex=deck.GetComponentInChildren<LastRail.Presentation.MissionShell.OperatorCodexView>();if(!deck.IsDialogOpen||codex==null||!codex.CurrentState.IsList)throw new InvalidOperationException("Codex detail back skipped its list");deck.RequestBack();if(deck.IsDialogOpen)throw new InvalidOperationException("Codex list back did not close");}await Task.Yield();if(deck.GetComponentsInChildren<CanvasGroup>(true).Length!=baselineGroups)throw new InvalidOperationException("Modal input groups survived close");}
      Destroy(current);current=null;await Task.Delay(150);}
     memory.Checkpoint(cycle+"-after");
    }
    File.WriteAllText(Path.Combine(output,"result.json"),new JObject{["status"]="Passed",["scope"]="REAL_PLAYER_SCREEN_COMPONENT_SWEEP_NOT_AUTHENTICATED_PRODUCT_FLOW",["captures"]=rows,["httpTransportComposed"]=false,["startSent"]=false,["captureMethod"]="OFFSCREEN_PLAYER_CAMERA_WITH_TEMPORARY_CANVAS_MODE",["backRouting"]="PAGE_AND_CODEX_CALLBACKS_THREE_SAME_FRAME_NOTICE_REPLACEMENTS_NOT_OS_INPUT",["touchTested"]=false,["forcedGC"]=false}.ToString());Destroy(events);Application.Quit(0);
   }catch(Exception e){if(Directory.Exists(output))File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
   finally{if(current!=null)Destroy(current);if(memory!=null)Destroy(memory.gameObject);}
  }
  internal static async Task Capture(string output,string name){
   await Task.Delay(350);
   var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
   var canvases=Array.FindAll(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None),c=>c.isRootCanvas);
   var modes=Array.ConvertAll(canvases,c=>c.renderMode);var cameras=Array.ConvertAll(canvases,c=>c.worldCamera);var distances=Array.ConvertAll(canvases,c=>c.planeDistance);
   var target=new RenderTexture(Screen.width,Screen.height,24);var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);
   var previous=RenderTexture.active;var priorTarget=camera.targetTexture;
   try{
    camera.targetTexture=target;
    foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
    pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
    var colors=pixels.GetPixels32();int varied=0;foreach(var color in colors)if(color.r!=colors[0].r||color.g!=colors[0].g||color.b!=colors[0].b)varied++;
    if(varied<colors.Length/100)throw new IOException("Empty offscreen Player capture: "+name);
    File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());
   }finally{
    for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
    camera.targetTexture=priorTarget;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(pixels);
   }
  }
 }
}
#endif
