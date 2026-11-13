#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 public sealed class OrdinaryTravelPanelPlayerProbe:MonoBehaviour {
  [SerializeField] TMP_FontAsset font;
  [SerializeField] GameObject lobbyMusicScreen;
  public void Initialize(TMP_FontAsset value){
   font=value;
#if UNITY_EDITOR
   if(Environment.GetEnvironmentVariable("LASTRAIL_LOBBY_BGM_PLAYER_CHECK")=="1")
    lobbyMusicScreen=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab");
#endif
  }
  async void Start(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_TRAVEL_UI_QA");
   if(string.IsNullOrEmpty(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)){Application.Quit(2);return;}
   Directory.CreateDirectory(output);try{
   if(Environment.GetEnvironmentVariable("LASTRAIL_SYSTEM_HUD_CHECK")=="1"){await SystemDurabilityPlayerCheck.Run(output);Application.Quit(0);return;}
   if(Environment.GetEnvironmentVariable("LASTRAIL_NUMBERED_FORMATION_CHECK")=="1"){await NumberedFormationPlayerCheck.Run(output,font);Application.Quit(0);return;}
   if(Environment.GetEnvironmentVariable("LASTRAIL_LOBBY_BGM_PLAYER_CHECK")=="1"){await LobbyDepartureMusicPlayerCheck.Run(output,lobbyMusicScreen,font);Application.Quit(0);return;}
   if(Environment.GetEnvironmentVariable("LASTRAIL_AUDIO_NATIVE_PLAYER_CHECK")=="1"){await ReinaCinematicAudioPlayerCheck.Run(output);Application.Quit(0);return;}int selected=0,throttled=0,resumed=0,backs=0;bool pass=false;
   var panel=OrdinaryTravelPanel.Create(transform,font);
   panel.Bind(_=>selected++,v=>throttled++,()=>{},()=>resumed++,()=>{},()=>backs++,()=>{});
   panel.ShowChoices(new[]{("test-a","검수용 목적지 A"),("test-b","검수용 목적지 B"),("test-c","검수용 목적지 C")},true,"테스트 데이터 · 실제 서버 연결 없음");
   await LandscapeScreenSweepProbe.Capture(output,"destination");
   Button Find(string name)=>panel.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
   Find("Destination0").onClick.Invoke();Find("Back").onClick.Invoke();
   panel.ShowTravel("검수용 목적지 A","테스트 데이터 · 확정 이동 10 / 100 m",5000,1000,13000,false,false);
   var nozzle=panel.GetComponentInChildren<OrdinaryTravelNozzle>();nozzle.value=7000;
   bool dragQuiet=throttled==0;nozzle.OnPointerUp(new PointerEventData(EventSystem.current));
   await LandscapeScreenSweepProbe.Capture(output,"travel");
   panel.ShowTravel("검수용 목적지 A","테스트 데이터 · 일시정지",7000,1000,13000,true,false);
   bool canResume=Find("Resume").interactable;Find("Resume").onClick.Invoke();
   panel.ShowArrivalPaused("테스트 데이터 · 도착 확정 후 일시정지");bool arrivalResume=Find("Resume").interactable;
   panel.Unbind();Find("Resume").onClick.Invoke();Find("Back").onClick.Invoke();
   pass=selected==1&&backs==1&&throttled==1&&resumed==1&&dragQuiet&&canResume&&arrivalResume;
   File.WriteAllText(Path.Combine(output,"player-check.json"),new JObject{["passed"]=pass,["scope"]="ACTUAL_PLAYER_VIEW_INPUT_WITH_SYNTHETIC_DATA_NO_SERVER_NO_COMBAT",["destinationCallbacks"]=selected,["throttleCallbacks"]=throttled,["resumeCallbacks"]=resumed,["backCallbacks"]=backs,["dragSentNothing"]=dragQuiet,["pausedResumeEnabled"]=canResume,["arrivalResumeEnabled"]=arrivalResume,["width"]=Screen.width,["height"]=Screen.height}.ToString());
   panel.gameObject.SetActive(false);await CooldownDisplayPlayerCheck.Run(output,font);
   var enemyFixture=Environment.GetEnvironmentVariable("LASTRAIL_ENEMY_V2_FIXTURE_PATH");if(!string.IsNullOrEmpty(enemyFixture))OrdinaryEnemySpatialPlayerCheck.Run(output,enemyFixture);
   var ordinaryFixture=Environment.GetEnvironmentVariable("LASTRAIL_GUNNER_PRESENTATION_SOURCE");if(!string.IsNullOrEmpty(ordinaryFixture))await OrdinaryGunnerPlayerCheck.Run(output,ordinaryFixture);
   await Task.Delay(500);Application.Quit(pass?0:1);
   }catch(Exception error){File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());Debug.LogException(error);Application.Quit(1);}
  }
 }
}
#endif
