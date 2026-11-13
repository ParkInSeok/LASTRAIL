using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    // QA-only composition of unapproved candidates. No Resources import or production replacement.
    public sealed class RunVictoryCandidateQaTests
    {
        const string Art="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/victory-assets-v01/";
        const string Fixtures="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
        sealed class Wire:IRunApiHttpTransport
        {
            public int Reads,Writes;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
            {
                if(r.Method!="GET"){Writes++;throw new InvalidOperationException("QA forbids mutations");}Reads++;
                if(r.EndpointPath.EndsWith("/lifecycle"))
                {
                    var json=JObject.Parse(File.ReadAllText(Fixtures+"result-lost-finish-restored.json"));json["runStatus"]="COMPLETED";json["finish"]["outcome"]="VICTORY";
                    return Task.FromResult(RunApiHttpResponse.Success(200,json.ToString()));
                }
                if(r.EndpointPath.Contains("/reward-preview/"))return Task.FromResult(RunApiHttpResponse.Success(200,File.ReadAllText(Fixtures+"result-preview.json")));
                return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY unrelated endpoint unavailable"));
            }
        }
        static Texture2D LoadCandidate(string file,string expected,List<Texture2D> owned)
        {
            byte[] bytes=File.ReadAllBytes(Art+file);using var sha=SHA256.Create();Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(expected),file);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);owned.Add(texture);Assert.That(texture.LoadImage(bytes),Is.True);texture.wrapMode=TextureWrapMode.Clamp;return texture;
        }
        static Rect Uv(Texture2D t,float left,float top,float width,float height)=>new Rect(left/t.width,1-(top+height)/t.height,width/t.width,height/t.height);
        static RawImage Image(string name,Transform parent,Texture2D texture,Rect area,Rect uv)
        {
            var rect=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(rect,area.x,area.y,area.width,area.height);
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.uvRect=uv;image.raycastTarget=false;return image;
        }
        [UnityTest,Explicit("Read-only native preview of unapproved Direction candidates; not runtime adoption.")]
        public IEnumerator CaptureCandidateComposition()
        {
            yield return new EnterPlayMode();var output=Path.GetFullPath("../QA/Result22Candidate-20260921/host-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("RESULT22_CANDIDATE_OUTPUT="+output);
            var records=new JArray();var owned=new List<Texture2D>();
            try
            {
                var trainTexture=LoadCandidate("train-v01.png","3baacb01d35890c35dbb0da5005d92c568c63efa8434865f6dc87a7fefb78990",owned);
                var frameTexture=LoadCandidate("frame-v02.png","4fe7a1215025ca9a4f1c7f43af50dc8d0bf1e10b1e3902039a5c0cd5fcdaef79",owned);
                var accentTexture=LoadCandidate("title-accent-v02.png","1ec5d178d0cbfa3ec136276783225979d36525933b5b4f665b5a80d302cd99fe",owned);
                foreach(int width in new[]{360,720})
                {
                    int height=width*16/9;var wire=new Wire();var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
                    var root=new GameObject("CandidateHost",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
                    var safe=AuthFormVisuals.Rect("SafeArea",root.transform);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.sizeDelta=Vector2.zero;
                    var co=new GameObject("CandidateCamera",typeof(Camera));var camera=co.GetComponent<Camera>();var target=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                    var events=new GameObject("CandidateEvents",typeof(EventSystem));
                    try
                    {
                        camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
                        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                        var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY CANDIDATE",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(owner);deck.OpenRunLifecycle("run-smoke-1");yield return null;
                        var view=root.GetComponentInChildren<RunVictoryView>();Assert.That(view,Is.Not.Null);
                        void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
                        byte[] Capture(string state)
                        {
                            Draw();foreach(var text in view.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name);}
                            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();var bytes=pixels.EncodeToPNG();File.WriteAllBytes(Path.Combine(output,width+"-"+state+".png"),bytes);
                            records.Add(new JObject{["width"]=width,["state"]=state,["writes"]=wire.Writes});Assert.That(wire.Writes,Is.Zero);return bytes;
                        }
                        var baseline=Capture("baseline");
                        var train=view.transform.Find("VictoryTrain").GetComponent<RawImage>();var tr=train.rectTransform;var originalTexture=train.texture;var originalUv=train.uvRect;bool originalEnabled=train.enabled;var originalPosition=tr.anchoredPosition;var originalSize=tr.sizeDelta;
                        train.texture=trainTexture;train.uvRect=Uv(trainTexture,14,18,1886,775);train.enabled=true;AuthFormVisuals.Place(tr,0,215,920,920f*775/1886);
                        var accent=Image("CandidateTitleAccent",view.transform,accentTexture,new Rect(210,203,520,520f*105/2038),Uv(accentTexture,19,316,2038,105));
                        var metric=view.transform.Find("VictoryLower/VictoryMetrics");var oldFrame=metric.Find("Frame");bool oldFrameVisible=oldFrame.gameObject.activeSelf;oldFrame.gameObject.SetActive(false);
                        var rim=AuthFormVisuals.Rect("CandidateMetricFrame",metric);AuthFormVisuals.Place(rim,0,0,895,106);
                        float[] sx={16,76,2030,2090},sy={267,317,429,479},dx={0,22,873,895},dy={0,19,87,106};
                        for(int y=0;y<3;y++)for(int x=0;x<3;x++)if(x!=1||y!=1)Image("Border"+x+y,rim,frameTexture,new Rect(dx[x],dy[y],dx[x+1]-dx[x],dy[y+1]-dy[y]),Uv(frameTexture,sx[x],sy[y],sx[x+1]-sx[x],sy[y+1]-sy[y]));
                        Assert.That(rim.childCount,Is.EqualTo(8));yield return null;Capture("candidate");
                        train.enabled=false;Capture("hide-train");train.enabled=true;rim.gameObject.SetActive(false);Capture("hide-frame");rim.gameObject.SetActive(true);accent.gameObject.SetActive(false);Capture("hide-accent");accent.gameObject.SetActive(true);
                        Draw();var details=view.GetComponentsInChildren<Button>().Single(b=>b.name=="VictoryDetails");var rect=(RectTransform)details.transform;
                        var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                        var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(details));ExecuteEvents.Execute(details.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                        var refresh=view.GetComponentsInChildren<Button>().Single(b=>b.name=="VictoryRefresh");int before=wire.Reads;refresh.onClick.Invoke();yield return null;Assert.That(wire.Reads-before,Is.EqualTo(2));details.onClick.Invoke();
                        UnityEngine.Object.DestroyImmediate(rim.gameObject);UnityEngine.Object.DestroyImmediate(accent.gameObject);oldFrame.gameObject.SetActive(oldFrameVisible);train.texture=originalTexture;train.uvRect=originalUv;train.enabled=originalEnabled;tr.anchoredPosition=originalPosition;tr.sizeDelta=originalSize;yield return null;
                        Assert.That(Capture("restored"),Is.EqualTo(baseline),"QA injection must restore exact baseline pixels.");
                    }
                    finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(co);UnityEngine.Object.DestroyImmediate(events);}
                    yield return null;
                }
                File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["status"]="UNAPPROVED_QA_ONLY",["source"]="QA/DirectionImpact-20260921/victory-assets-v01/asset-check.json compositionContract",["productionChanges"]=false,["pngPixelChanges"]=false,["metalTitleOrLogoImplemented"]=false,["cases"]=records}.ToString());
            }
            finally{foreach(var texture in owned)UnityEngine.Object.DestroyImmediate(texture);}
            yield return new ExitPlayMode();
        }
    }
}