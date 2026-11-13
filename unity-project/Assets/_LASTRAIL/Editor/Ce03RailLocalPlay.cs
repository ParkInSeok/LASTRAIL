#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class Ce03RailLocalPlay:EditorWindow {
 string connectionPath="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/connection.private.json";
 string message="실행 중인 로컬 서버 연결 파일을 선택해 주세요.";bool busy;
 [MenuItem("LAST RAIL/Local QA/Rail Combat")]
 public static void Open()=>GetWindow<Ce03RailLocalPlay>("Rail Combat");
 void OnGUI(){
  EditorGUILayout.LabelField("로컬 분기 전투",EditorStyles.boldLabel);
  connectionPath=EditorGUILayout.TextField("연결 파일",connectionPath);
  if(GUILayout.Button("연결 파일 선택")){var chosen=EditorUtility.OpenFilePanel("Local connection","D:/Project/LASTRAIL","json");if(!string.IsNullOrEmpty(chosen))connectionPath=chosen;}
  if(!Application.isPlaying){EditorGUILayout.HelpBox("빈 장면에서 Play를 누른 후 전투를 여세요.",MessageType.Info);}
  using(new EditorGUI.DisabledScope(busy||!Application.isPlaying))if(GUILayout.Button("준비 및 전투 열기 / 이어하기"))Launch();
  EditorGUILayout.HelpBox(message,MessageType.Info);
 }
 async void Launch(){busy=true;try{await Ce03RailLocalRuntime.Open(connectionPath);message="로컬 전투 실행 중";}catch(Exception e){message=e.Message;Debug.LogException(e);}finally{busy=false;Repaint();}}
}

public sealed class Ce03RailLocalRuntime:MonoBehaviour {
 CombatSceneDriver driver;bool applicationPaused,applicationFocused=true;
 public static async Task<CombatSceneDriver> Prepare(string connectionPath,string folder){
  folder=Path.GetFullPath(folder);if(!folder.StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D output required.");Directory.CreateDirectory(folder);
  var c=Ce03NormalUiCapture.Read(connectionPath);
  if((bool?)c["disposable"]!=true||(string)c["dataLifetime"]!="PROCESS"||(long?)c["expiresAt"]<=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())throw new InvalidOperationException("Live disposable connection required.");
  string owner=(string)c["accountId"],authorization=(string)c["authorization"];if(authorization==null||!authorization.StartsWith("Bearer ",StringComparison.Ordinal))throw new FormatException("Missing local bearer.");
  var uri=new Uri(((string)c["origin"]).TrimEnd('/')+"/");
  var client=new Ce03LocalClient(uri,owner,new Ce03LoopbackTransport(uri),new GameApiAccessToken(authorization.Substring(7)),_=>throw new InvalidOperationException("Rail preparation has no mutable HTTP action."));
  string preparedPath=Path.Combine(folder,"preparation.json"),sessionPath=Path.Combine(folder,"session.json");Ce03RailPreparation preparation;JObject identity;
  if(File.Exists(sessionPath)){
   identity=Ce03NormalUiCapture.Read(sessionPath);if((string)identity["owner"]!=owner||(string)identity["origin"]!=uri.AbsoluteUri)throw new InvalidOperationException("Local owner/origin differs; preserve session.");
   preparation=new Ce03RailPreparation(Ce03NormalUiCapture.Read(preparedPath),owner,(long)identity["supportRevision"]);
   Ce03RailPresentationBinding.Verify(preparation);
  }else{
   var support=await client.ReadSupportAsync();long revision=(long)support["selection"]["revision"];
   preparation=await client.PrepareRailAsync(new JObject{["operationId"]="opn_01_last_departure",["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1},revision);
   Ce03RailPresentationBinding.Verify(preparation);
   identity=new JObject{["owner"]=owner,["origin"]=uri.AbsoluteUri,["supportRevision"]=revision,["sessionId"]=Guid.NewGuid().ToString("N"),["seed"]=1};
   Ce03NormalUiCapture.Atomic(preparedPath,Ce03CommandContract.Canonical(preparation.Copy()));Ce03NormalUiCapture.Atomic(sessionPath,Ce03CommandContract.Canonical(identity));
  }
  var session=new Ce03RailSession(preparation,(string)identity["sessionId"],(uint)identity["seed"]);
  string checkpoint=Path.Combine(folder,"checkpoint.json");var store=new CombatCheckpointStore(checkpoint,()=>File.Exists(checkpoint)?File.ReadAllText(checkpoint):null,raw=>Ce03NormalUiCapture.Atomic(checkpoint,raw));
  var driver=session.CreateDriver(store,()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()<(long)c["expiresAt"]);driver.Open();return driver;
 }
 public static async Task Open(string connectionPath){
  if(GameObject.Find("RailLocalBattle")!=null)throw new InvalidOperationException("Rail combat already open.");
  string folder="D:/Project/LASTRAIL/unity-project/QA/RailLocalPlay/"+Ce03CommandContract.Sha(Path.GetFullPath(connectionPath)).Substring(0,20);
  var driver=await Prepare(connectionPath,folder);
  var go=new GameObject("RailLocalBattle",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)new GameObject("RailLocalEvents",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
  go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
  go.AddComponent<Ce03RailLocalRuntime>().driver=driver;
 }
 void Update(){if(driver==null||applicationPaused||!applicationFocused||driver.RecoveryRequired||driver.Outcome!=BasicCombatOutcome.Running)return;try{driver.Advance((long)(Math.Min(Time.unscaledDeltaTime,.1f)*TimeSpan.TicksPerSecond),8);}catch(Exception e){Debug.LogException(e);enabled=false;}}
 void OnApplicationPause(bool paused){applicationPaused=paused;}
 void OnApplicationFocus(bool focused){applicationFocused=focused;}
}
#endif
