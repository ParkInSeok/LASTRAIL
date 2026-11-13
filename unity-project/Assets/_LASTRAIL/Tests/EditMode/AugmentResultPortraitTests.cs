using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class AugmentResultPortraitTests
{
    [UnityTest] public IEnumerator LongCopyAndResultScrollAtPortraitSizes()
    {
        Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"Graphics required");
        yield return new EnterPlayMode();
        string output=Path.GetFullPath("../QA/OperatorInformation-20261019/Portrait82");Directory.CreateDirectory(output);
        foreach(int height in new[]{1280,1600})
        {
            var root=new GameObject("PortraitResult",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
            var eo=new GameObject("Events",typeof(EventSystem));var co=new GameObject("Camera",typeof(Camera));var camera=co.GetComponent<Camera>();
            var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try {
                camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var card=HealingAugmentPresentation.FromPinned("augment.utility-definitions.v7.20261019",JObject.Parse("{id:'O23',name:'생명 역류',rules:{excessToDamageBps:6000,parentCasterAttackCapBps:15000,projectileProfile:{speedMmPerSecond:36000,maximumRangeMm:24000}}}"),JObject.Parse("{o02AndBackflowAllocationBps:[5000,5000],o02ConversionBps:5000,backflowConversionBps:6000}"));
                var view=AugmentNodeResultView.Create(root.transform,font);
                view.Bind(new AugmentNodeResult("qa-run","qa-node",42,Enumerable.Range(0,10).Select(i=>new AugmentNodeResultEntry("qa-"+i,card,i==1?AugmentRecordCoverage.None:i==2?AugmentRecordCoverage.Partial:AugmentRecordCoverage.Complete,i==2?"재접속 이후 확인된 기록":null,i==1?null:new[]{new AugmentResultMetric("적 HP 피해",i==0?0:420),new AugmentResultMetric("적 보호막 피해",0)}))));
                Button Find(string id)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==id);
                void Capture(string suffix){Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,height+"-"+suffix+".png"),pixels.EncodeToPNG());}
                Find("AugmentEffectsEntry").onClick.Invoke();yield return null;Capture("list");
                var list=view.GetComponentsInChildren<ScrollRect>().Single();Assert.Greater(list.content.rect.height,list.viewport.rect.height);
                list.verticalNormalizedPosition=.5f;Canvas.ForceUpdateCanvases();float position=list.verticalNormalizedPosition;
                Find("AugmentResultCard0").onClick.Invoke();yield return null;Capture("detail");
                var title=view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DetailTitle");title.ForceMeshUpdate();Assert.IsFalse(title.isTextOverflowing);
                Find("DetailExamples").onClick.Invoke();yield return null;Capture("examples");
                Find("DetailClose").onClick.Invoke();Assert.AreEqual(position,list.verticalNormalizedPosition,.01f);
                view.Unbind();Assert.IsFalse(Find("AugmentEffectsEntry").gameObject.activeSelf);
            } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);Object.DestroyImmediate(root);Object.DestroyImmediate(co);Object.DestroyImmediate(eo);}
            yield return null;
        }
        yield return new ExitPlayMode();
    }
}
