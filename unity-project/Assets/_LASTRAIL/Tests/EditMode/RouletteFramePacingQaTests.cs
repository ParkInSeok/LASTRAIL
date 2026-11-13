using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class RouletteFramePacingQaTests
    {
        [UnityTest] public IEnumerator MeasureWithoutCaptureThenRecordAsyncAfterRendering()
        {
            yield return new EnterPlayMode();
            var output=Path.GetFullPath("../QA/RouletteRewardReveal-20260917/async-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);
            Debug.Log("FRAME_PACING_OUTPUT="+output);
            var results=new JArray();
            foreach(bool combat in new[]{false,true})
                foreach(string mode in new[]{"baseline720","baseline360","async360"})
                    yield return Measure(output,combat,mode,results);
            File.WriteAllText(Path.Combine(output,"summary.json"),new JObject{["scope"]="Local production hosts in Unity Editor PlayMode. Timing collected in memory. Async videos at360x640; no PNG/file IO during spin. Not device performance.",["cases"]=results}.ToString());
            yield return new ExitPlayMode();
        }
        static IEnumerator Measure(string output,bool combat,string mode,JArray results)
        {
            bool record=mode=="async360";int width=mode=="baseline720"?720:360,height=width*16/9;
            string folder=Path.Combine(output,(combat?"combat-":"supply-")+mode);Directory.CreateDirectory(folder);
            var root=new GameObject("PacingHost",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            var cameraObject=new GameObject("PacingCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
            var wire=new RouletteFullFlowQaTests.Wire(combat,false){Status="AD_VERIFIED"};
            string run=(string)wire.Lifecycle["runId"];
            var journal=new RouletteFullFlowQaTests.Journal{Row=new PendingAccountAction("a",combat?"combat-ad-presented":"ad-presented",combat?run:(string)wire.Pending["intentId"],0,"local_pacing_key",wire.Intent().ToString())};
            var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
            using var account=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
            var frameTimes=new double[4096];int samples=0,captured=0,pending=0,errors=0,overCapacity=0,lastCaptured=-1;
            var imageTimes=new double[256];byte[][] pixels=null;
            if(record){Assert.That(SystemInfo.supportsAsyncGPUReadback,Is.True);pixels=new byte[256][];for(int i=0;i<pixels.Length;i++)pixels[i]=new byte[width*height*4];}
            bool recording=false;double began=0,nextCapture=0;
            void ReadRendered(Camera rendered)
            {
                if(!recording||rendered!=camera||lastCaptured==Time.frameCount)return;
                double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<nextCapture)return;
                nextCapture+=1.0/30;lastCaptured=Time.frameCount;
                if(captured>=pixels.Length){overCapacity++;return;}
                int index=captured++;imageTimes[index]=elapsed;pending++;
                AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32,request=>
                {
                    if(request.hasError)errors++;else request.GetData<byte>().CopyTo(pixels[index]);pending--;
                });
            }
            void PipelineRendered(ScriptableRenderContext context,Camera rendered)=>ReadRendered(rendered);
            try
            {
                camera.orthographic=true;camera.orthographicSize=640;camera.aspect=720f/1280;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                Button Button(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                if(combat){deck.OpenRunLifecycle(run);yield return null;Button("VictoryAd").onClick.Invoke();}
                else{Button("Shortcut3").onClick.Invoke();yield return null;Button("OpenOfflineRoulette").onClick.Invoke();}
                for(int i=0;i<10;i++)yield return null;
                Button(combat?"CombatAdMain":"AdMainAction").onClick.Invoke();Assert.That(wire.Spins,Is.EqualTo(1));
                Camera.onPostRender+=ReadRendered;RenderPipelineManager.endCameraRendering+=PipelineRendered;
                int gen0=GC.CollectionCount(0),gen1=GC.CollectionCount(1),gen2=GC.CollectionCount(2);
                began=Time.realtimeSinceStartupAsDouble;recording=record;wire.Complete();
                double previous=began;int lastFrame=Time.frameCount;
                while(Time.realtimeSinceStartupAsDouble-began<7.8)
                {
                    yield return null;
                    if(Time.frameCount==lastFrame)continue;
                    double now=Time.realtimeSinceStartupAsDouble;Assert.That(samples,Is.LessThan(frameTimes.Length));frameTimes[samples++]=(now-previous)*1000;previous=now;lastFrame=Time.frameCount;
                }
                recording=false;
                var elapsedTimes=frameTimes.Take(samples).ToArray();var sorted=elapsedTimes.OrderBy(x=>x).ToArray();
                double Percentile(double p)=>sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(p*sorted.Length)-1)];
                var row=new JObject{["host"]=combat?"combat":"supply",["mode"]=mode,["width"]=width,["height"]=height,["frames"]=samples,["p50ms"]=Percentile(.5),["p95ms"]=Percentile(.95),["maximumMs"]=sorted.Last(),["above33ms"]=elapsedTimes.Count(x=>x>33.333333),["above50ms"]=elapsedTimes.Count(x=>x>50),["gc0"]=GC.CollectionCount(0)-gen0,["gc1"]=GC.CollectionCount(1)-gen1,["gc2"]=GC.CollectionCount(2)-gen2,["targetFrameRate"]=Application.targetFrameRate,["vSyncCount"]=QualitySettings.vSyncCount,["graphics"]=SystemInfo.graphicsDeviceName,["pipeline"]=GraphicsSettings.currentRenderPipeline?.name??"BuiltIn",["asyncReadback"]=record,["capturedFrames"]=captured};
                Assert.That(root.GetComponentInChildren<SharedRouletteVisual>().IsAnimating,Is.False);Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CommittedMultiplier").text,Is.EqualTo("×4"));
                double waitBegan=Time.realtimeSinceStartupAsDouble;while(pending>0){Assert.That(Time.realtimeSinceStartupAsDouble-waitBegan,Is.LessThan(10));yield return null;}
                Assert.That(errors,Is.Zero);Assert.That(overCapacity,Is.Zero);
                // All serialization, compression, and disk writes happen after timing and motion finish.
                File.WriteAllText(Path.Combine(folder,"timing.json"),new JObject{["summary"]=row,["milliseconds"]=new JArray(elapsedTimes)}.ToString());
                if(record)
                {
                    Assert.That(captured,Is.GreaterThan(120),"Record enough unique rendered frames to improve on synchronous PNG capture");
                    var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var frames=new JArray();
                    try{for(int i=0;i<captured;i++){texture.LoadRawTextureData(pixels[i]);texture.Apply(false);string file=$"frame-{i:D4}.png";File.WriteAllBytes(Path.Combine(folder,file),texture.EncodeToPNG());frames.Add(new JObject{["file"]=file,["time"]=imageTimes[i]});}}
                    finally{UnityEngine.Object.DestroyImmediate(texture);}
                    File.WriteAllText(Path.Combine(folder,"frames.json"),frames.ToString());
                }
                results.Add(row);Debug.Log("FRAME_PACING "+row.ToString(Newtonsoft.Json.Formatting.None));
            }
            finally
            {
                recording=false;Camera.onPostRender-=ReadRendered;RenderPipelineManager.endCameraRendering-=PipelineRendered;
                camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(root);
            }
            yield return null;
        }
    }
}
