using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class AugmentSparseSelectionQaTests
    {
        [UnityTest] public IEnumerator SparsePublicSelectCapabilitiesAtPortraitSizes()
        {
            yield return new EnterPlayMode();
            string output=Path.GetFullPath("../QA/OperatorInformation-20261019/SparsePortrait110");Directory.CreateDirectory(output);Debug.Log("AUGMENT_UNIT_QA="+output);
            var rows=new JArray();
            foreach(int height in new[]{1280,1600})
            {
                var root=new GameObject("TEST_ONLY_NO_COMBAT_HOST",typeof(RectTransform),typeof(Canvas));var cameraObject=new GameObject("Camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
                var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                try
                {
                    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;((RectTransform)root.transform).sizeDelta=new Vector2(1000,height/.72f);root.transform.localScale=Vector3.one*.72f;
                    camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.backgroundColor=new Color(.03f,.04f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                    var label=AuthFormVisuals.Text(root.transform,"TestScope","TEST ONLY · 독립 입력/표시 검사 · 승인 원화 미연결",font,22,false,TextAlignmentOptions.Center);AuthFormVisuals.Place(label.rectTransform,20,20,960,56);
                    var view=AugmentSelectionView.Create(root.transform,font);view.Bind(_=>{},(_,__)=>{},()=>{});
                    foreach(string state in new[]{"one-slot","three-slots"})
                    {
                        var value=(JObject)CombatO22OriginalTests.Packet()["definition"]["selectionSource"]["selection"];value["state"]="SELECTING";value["grantRevision"]=0;value["selectedCandidateId"]=null;
                        if(state=="one-slot"){var slots=(JArray)value["slots"];while(slots.Count>1)slots.RemoveAt(1);}
                        view.RenderWithCapabilities(AugmentSelectionSnapshot.Parse(value.ToString()),false,false,"",(_,id)=>new AugmentCardPresentation("검증 후보 "+id,"TEST ONLY","원정의 표시 연결 검사",null),false,false);
                        yield return null;Canvas.ForceUpdateCanvases();camera.Render();
                        foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+":"+text.text);}
                        RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();string name=height+"-"+state+".png";File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());
                        rows.Add(new JObject{{"file",name},{"state",state},{"overflow",false},{"runtimeHost",false},{"approvedArt",false}});
                    }
                }
                finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);}
            }
            File.WriteAllText(Path.Combine(output,"verification.json"),rows.ToString());yield return new ExitPlayMode();
        }
    }
}
