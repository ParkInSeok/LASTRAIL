#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 // Actual accepted MAIN events, actual Server-module isolated source, actual Transform
 // producer and original node callback. No art/operating/multi/pierce claim.
 public sealed class MiraFirstNativeFeedbackAcceptanceTests {
  static Vector3 P(JToken a)=>new Vector3((float)(long)a[0],(float)(long)a[1],(float)(long)a[2])/1000f;
  [Test]public void ActualAcceptedNodePrimaryContactsAndNativeViewGpuCapture(){
   Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Requires dedicated -WithGraphics acceptance run");
   var binding=BMiraFirstNativeFixture.Binding();var node=BMiraFirstNativeFixture.Node();var paths=BMiraFirstNativeFixture.Paths(binding.Composition.Attack);
   var firstSource=BMiraFirstNativeFixture.Source();var context=BMiraFirstTrialContext.Read(firstSource,BMiraFirstTrialContextTests.Policy());
   var config=global::CombatRailJourneyTests.Config();config["offers"]=new JArray();config["throttleMaximumBps"]=13000;config["throttleCruiseBps"]=10000;var motion=BTravelRuntimeConfiguration.LocalValidation(config,BDirectAttackInput.Digest(config));
   var scene=BMiraFirstNativeFixture.Scene();var viewObject=new GameObject("ActualNativeFeedbackView");var cameraObject=new GameObject("FirstNativeFeedbackQaCamera");var overlay=new GameObject("QA_context_wires_NOT_ART");
   var feedback=new MiraFirstFeedbackController();var pulses=new List<JObject>();BNodeSessionController controller=null;RenderTexture rt=null;Material wire=null;Camera camera=null;
   string output=Environment.GetEnvironmentVariable("LASTRAIL_MIRA_FEEDBACK_QA_OUTPUT")??"D:/Project/LASTRAIL/QA/MiraFirstLive-20261015/NativeFeedback01";
   try{
    Assert.That(output.StartsWith("D:/Project/LASTRAIL/QA/MiraFirstLive-20261015/",StringComparison.OrdinalIgnoreCase),Is.True);Assert.That(Directory.Exists(output),Is.False,"Fresh capture output directory required");Directory.CreateDirectory(output);
    // Nontrivial scene basis detects accidental world/basis-local mixing.
    scene.transform.position=new Vector3(2,.1f,-1);scene.transform.rotation=Quaternion.Euler(0,15,0);
    var root=BDirectAttackBasicLoop.CreateMiraFirstTrialRuntime(binding,node,paths,context,scene,10000,motion);
    root.ObserveMiraFirstOriginal("qa-shield",new JObject{["kind"]="LOCAL_ORIGINAL",["eventId"]="qa-shield",["operation"]="SHIELD",["amount"]=10,["expiry"]=10000});
    var source=node.OriginalSource.Capture();var trust=new JObject(new[]{"origin","nativeContextId","seedHash","sourceHash","nodeId","visitDepth","priorSourceHash"}.Select(k=>new JProperty(k,source[k].DeepClone())));
    controller=new BNodeSessionController(firstSource.AccountId,firstSource.RunId);controller.BindVerifiedNode(source,trust,0);controller.AttachLocalTravel(node,motion,(n,m)=>root,controller.Revision);
    var view=viewObject.AddComponent<MiraFirstFeedbackView>();view.BindBasis(scene.transform);
    feedback.Bind(frame=>{foreach(JObject p in (JArray)frame["pulses"])pulses.Add((JObject)p.DeepClone());view.ApplyFrame(frame);},view.Clear,controller.Capture());controller.Bind(feedback.Observe);
    int hp=root.Hp(BMiraFirstNativeFixture.TargetId),index=0;
    JArray Shots()=>(JArray)root.Capture()["miraFirstTrial"]["shots"];
    void Frame()=>controller.AdvanceMiraFirstFrame("qa-frame-"+(++index),binding.Composition.Attack.StepTimeSpanTicks,scene,controller.Revision);
    for(int n=0;n<200&&Shots().Count==0;n++)Frame();Assert.That(Shots().Count,Is.EqualTo(1));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp));Frame();Frame();
    wire=new Material(Shader.Find("Unlit/Color")){color=Color.cyan};overlay.transform.SetParent(scene.transform,false);
    void Line(string id,Vector3[] points){var go=new GameObject(id);go.transform.SetParent(overlay.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=.018f;line.sharedMaterial=wire;}
    var observation=scene.Observe(root.Tick,1000);
    foreach(var target in ((JArray)observation["targets"]).Take(2)){var foot=P(target["feetMm"]);float r=(float)(long)target["radiusMm"]/1000,h=(float)(long)target["heightMm"]/1000;foreach(float y in new[]{r,h-r})Line((string)target["id"],Enumerable.Range(0,33).Select(n=>foot+new Vector3(Mathf.Cos(n*Mathf.PI/16)*r,y,Mathf.Sin(n*Mathf.PI/16)*r)).ToArray());Line("height",new[]{foot,foot+Vector3.up*h});}
    var muzzle=P(observation["source"]["muzzleMm"]);Line("source-feet-socket",new[]{P(observation["source"]["feetMm"]),muzzle});
    camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.6f;camera.transform.position=scene.transform.TransformPoint(new Vector3(19,8,-5));camera.transform.LookAt(scene.transform.TransformPoint(new Vector3(12.9f,3,3)));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;
    foreach(var renderer in view.GetComponentsInChildren<Renderer>()){var v=camera.WorldToViewportPoint(renderer.bounds.center);Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));Assert.That(v.z,Is.GreaterThan(0));}
    byte[] Render(string name){var old=RenderTexture.active;Texture2D image=null;try{camera.Render();RenderTexture.active=rt;image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();var bytes=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(output,name+".png"),bytes);return bytes;}finally{RenderTexture.active=old;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}}
    var before=root.Capture();var on=Render("primary-in-flight-on");view.SetVisible(false);var off=Render("primary-in-flight-off");Assert.That(on.SequenceEqual(off),Is.False);Assert.That(JToken.DeepEquals(before,root.Capture()),Is.True);view.SetVisible(true);
    for(int n=0;n<70&&(string)Shots()[0]["status"]=="FLYING";n++)Frame();Assert.That((string)Shots()[0]["status"],Is.EqualTo("HIT"));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp-33));Render("actual-shield-and-hp-contact");
    Assert.That(pulses.Count(p=>(string)p["kind"]=="FIRE"),Is.EqualTo(1));Assert.That(pulses.Count(p=>(string)p["kind"]=="CONTACT"),Is.EqualTo(1));Assert.That(pulses.Count(p=>(string)p["kind"]=="SHIELD"),Is.EqualTo(1));Assert.That(pulses.Count(p=>(string)p["kind"]=="HP"),Is.EqualTo(1));
    var originalCheckpoint=controller.Capture();var saved=JObject.Parse(originalCheckpoint.ToString());Assert.That(BDirectAttackInput.Digest(saved),Is.EqualTo(BDirectAttackInput.Digest(originalCheckpoint)));using var restored=BMiraFirstNodeCheckpoint.Restore(saved,BDirectAttackInput.Digest(saved),trust,binding,node,paths,context,scene,10000,motion);Assert.That(JToken.DeepEquals(saved,restored.Capture()),Is.True);
    int count=pulses.Count;feedback.Bind(frame=>{foreach(JObject p in (JArray)frame["pulses"])pulses.Add((JObject)p.DeepClone());view.ApplyFrame(frame);},view.Clear,restored.Capture());Assert.That(pulses.Count,Is.EqualTo(count),"Restore/rebind must not repeat old flashes");
    var frozen=root.Capture();view.transform.localPosition=Vector3.right;Assert.Throws<InvalidOperationException>(()=>feedback.Observe(controller.Capture()));Assert.That(JToken.DeepEquals(frozen,root.Capture()),Is.True);view.transform.localPosition=Vector3.zero;
    File.WriteAllText(Path.Combine(output,"acceptance.json"),new JObject{["version"]=BDirectAttackBasicLoop.MiraFirstVersion,["profile"]=BMiraFirstTestSource.Profile,["contextHash"]=context.Hash,["firstSourceHash"]=firstSource.Hash,["nativeNodeSourceHash"]=node.SourceHash,["width"]=1920,["height"]=1080,["graphics"]=SystemInfo.graphicsDeviceType.ToString(),["gpu"]=SystemInfo.graphicsDeviceName,["sourceHp"]=root.Hp("op_mira"),["targetHpBefore"]=hp,["targetHpAfter"]=root.Hp(BMiraFirstNativeFixture.TargetId),["acceptedPulses"]=new JArray(pulses),["toggleChangesAuthority"]=false,["restoredHistoricalPulses"]=0,["wholeNodeRestoreEqual"]=true,["basisMisbindRejected"]=true,["scope"]="ACTUAL_ISOLATED_NATIVE_MAIN_EVENTS_AND_DIAGNOSTIC_VIEW_NOT_FINAL_ART_OR_MULTI_PIERCE_OR_OPERATING"}.ToString());
   }finally{feedback.Unbind();controller?.Unbind();controller?.Dispose();if(camera!=null)camera.targetTexture=null;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(wire!=null)UnityEngine.Object.DestroyImmediate(wire);UnityEngine.Object.DestroyImmediate(viewObject);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(overlay);UnityEngine.Object.DestroyImmediate(scene.gameObject);}
  }
 }
}
#endif