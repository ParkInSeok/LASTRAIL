#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastRail.Battle;
// Isolated CPU weighted-mesh feasibility stage. Never used by ApprovedCombatView.
[ExecuteAlways,RequireComponent(typeof(CanvasRenderer))]
public sealed class KyleDiagnosticMesh:MaskableGraphic {
 public Texture2D Source;public float Phase;public override Texture mainTexture=>Source;
 static Vector2 Rotate(Vector2 point,Vector2 pivot,float angle){float a=angle*Mathf.Deg2Rad;var p=point-pivot;return pivot+new Vector2(p.x*Mathf.Cos(a)-p.y*Mathf.Sin(a),p.x*Mathf.Sin(a)+p.y*Mathf.Cos(a));}
 public static Vector2 Deform(Vector2 p,float phase){
  // Initial visual annotations, not verified anatomical pivots or sockets.
  float arm=Mathf.Clamp01((p.y-90)/30)*Mathf.Clamp01((385-p.y)/20)*Mathf.Clamp01((p.x-122)/15)*Mathf.Clamp01((235-p.x)/20);
  if(p.y>380||p.y<90||p.x>235||p.x<122)return p;
  var q=Rotate(p,new Vector2(195,110),-70*phase);
  return Vector2.Lerp(p,q,arm);
 }
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();const int cols=42,rows=52;
  for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){var rest=new Vector2(x*10,y*512f/rows);var p=Deform(rest,Phase);m.AddVert(new Vector2(p.x,-p.y),Color.white,new Vector2(rest.x/1536,1-rest.y/1024));}
  for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){int n=y*(cols+1)+x;m.AddTriangle(n,n+cols+1,n+1);m.AddTriangle(n+1,n+cols+1,n+cols+2);}
 }
}
public static class KyleBoundedRigDiagnostic {
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rigOutput");if(index<0||Directory.Exists(args[index+1]))throw new ArgumentException("Fresh rig output required");string output=args[index+1];Directory.CreateDirectory(output);
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("RigStageCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.04f);var target=new RenderTexture(1440,720,24);camera.targetTexture=target;
  var canvasGo=new GameObject("RigDiagnosticOnly",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,720);scaler.matchWidthOrHeight=1;
  RectTransform Rect(string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(canvasGo.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");var source=assets.ActorAtlas("op_kyle");
  void Label(string message,float x){var label=Rect(message,x,28,620,60).gameObject.AddComponent<TextMeshProUGUI>();label.font=assets.Font;label.fontSize=24;label.text=message;label.color=Color.white;}
  Label("승인 원화 · 고정 기준",95);Label("가중 메쉬 진단 · 제품 미적용",795);
  var reference=Rect("Rest",95,110,420,512).gameObject.AddComponent<RawImage>();reference.texture=source;reference.uvRect=new UnityEngine.Rect(0,.5f,420f/1536,.5f);
  var mesh=Rect("WeightedMesh",795,110,420,512).gameObject.AddComponent<KyleDiagnosticMesh>();mesh.Source=source;
  var psi=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-y -v error -f rawvideo -pix_fmt rgb24 -video_size 1440x720 -framerate 60 -i pipe:0 -vf vflip -an -c:v libx264 -crf 20 -pix_fmt yuv420p \""+output+"/rig-diagnostic.mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true};
  var encoder=System.Diagnostics.Process.Start(psi);var pixels=new Texture2D(1440,720,TextureFormat.RGB24,false);var frames=new JArray();float maxFeet=0;
  for(int frame=0;frame<120;frame++){
   float phase=Mathf.Sin(frame/119f*Mathf.PI);mesh.Phase=phase;mesh.SetVerticesDirty();mesh.SetMaterialDirty();mesh.Rebuild(CanvasUpdate.PreRender);Canvas.ForceUpdateCanvases();camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new UnityEngine.Rect(0,0,1440,720),0,0);pixels.Apply();RenderTexture.active=prior;
   var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);if(frame==0||frame==60||frame==119)File.WriteAllBytes(output+"/phase-"+frame+".png",pixels.EncodeToPNG());
   foreach(var foot in new[]{new Vector2(149,494),new Vector2(294,484)})maxFeet=Mathf.Max(maxFeet,Vector2.Distance(foot,KyleDiagnosticMesh.Deform(foot,phase)));
   var tip=KyleDiagnosticMesh.Deform(new Vector2(222,367),phase);frames.Add(new JObject{["frame"]=frame,["phase"]=phase,["knifeTipEstimatedX"]=tip.x,["knifeTipEstimatedY"]=tip.y});
  }
  encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed");
  File.WriteAllText(output+"/result.json",new JObject{["scope"]="ISOLATED_EDITOR_WEIGHTED_MESH_FEASIBILITY",["runtimeIntegrated"]=false,["actualAttackDueBound"]=false,["frames"]=120,["fps"]=60,["maxFootDisplacementSourcePixels"]=maxFeet,["pivotStatus"]="VISUALLY_ESTIMATED_NOT_VERIFIED",["source"]=AssetDatabase.GetAssetPath(source),["cropTopLeft"]=new JArray(0,0,420,512),["missing"]="No separated arm/torso/knife or restored occluded surfaces; visual deformation gate required",["samples"]=frames}.ToString());
  target.Release();Debug.Log("Kyle diagnostic rendered; visual gate pending");
 }
}
#endif
