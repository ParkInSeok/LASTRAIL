#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class Ce03LocalSupportBootstrap {
 [MenuItem("LAST RAIL/Local QA/Support Selection")]
 public static void Open(){
  if(!Application.isPlaying)throw new InvalidOperationException("Use a disposable PlayMode scene.");
  if(GameObject.Find("LocalSupportCanvas")!=null)throw new InvalidOperationException("Local support screen already open.");
  const string path="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/connection.private.json";
  var config=JObject.Parse(File.ReadAllText(path));if((bool?)config["disposable"]!=true||(string)config["dataLifetime"]!="PROCESS"||(long)config["expiresAt"]<=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())throw new InvalidOperationException("Live disposable bootstrap required.");
  var uri=new Uri((string)config["origin"]+"/");string owner=(string)config["accountId"],authorization=(string)config["authorization"];if(authorization==null||!authorization.StartsWith("Bearer ",StringComparison.Ordinal))throw new FormatException();
  string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Ce03CommandIntegration"));Directory.CreateDirectory(dir);string journal=Path.Combine(dir,"local-journal-"+Ce03CommandContract.Sha(owner+uri.Authority).Substring(0,20)+".json");
  void Persist(string value){string temp=journal+".tmp";byte[] bytes=new UTF8Encoding(false,true).GetBytes(value);using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){file.Write(bytes,0,bytes.Length);file.Flush(true);}if(File.Exists(journal))File.Replace(temp,journal,null);else File.Move(temp,journal);}
  var client=new Ce03LocalClient(uri,owner,new Ce03LoopbackTransport(uri),new GameApiAccessToken(authorization.Substring(7)),Persist,File.Exists(journal)?File.ReadAllText(journal):null);
  var go=new GameObject("LocalSupportCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
  var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=1;
  if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null){var events=new GameObject("LocalSupportEvents",typeof(EventSystem));events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();}
  Ce03SupportSelectionPanel.Open(go.transform,client,()=>UnityEngine.Object.Destroy(go));
 }
}
#endif
