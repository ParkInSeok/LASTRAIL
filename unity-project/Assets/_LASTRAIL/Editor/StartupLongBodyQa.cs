#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Presentation.Startup;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class StartupLongBodyQa
{
    public static void Run()
    {
        try
        {
            string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA required");
            Directory.CreateDirectory(output);var cases=new JArray();foreach(int height in new[]{1280,1600})Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="47 actual startup presentation/native ScrollRect handlers in Editor camera fixture. No external operations.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Capture(string output,int height,JArray cases)
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
        var cameraObject=new GameObject("Camera",typeof(Camera));var events=new GameObject("Events",typeof(EventSystem));
        var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1080f*height/720/2;camera.aspect=720f/height;
            camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var view=root.GetComponent<StartupScreenRootBehaviour>();
            var normal=StartupScreenViewState.UpdateNotice("HIDDEN_VERSION_SENTINEL","업데이트 후 다시 접속해 주세요.");view.Render(normal);
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1080,1080f*height/720);rect.position=Vector3.zero;rect.localScale=Vector3.one;
            var safe=(RectTransform)root.transform.Find("SafeArea");safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=new Vector2(24,36);safe.offsetMax=new Vector2(-24,-36);
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)){canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;}
            Canvas.ForceUpdateCanvases();
            int callbacks=0,wheels=0,drags=0,keyboardMoves=0,captures=0;view.PrimaryActionRequested+=()=>callbacks++;
            view.SecondaryActionRequested+=()=>throw new InvalidOperationException("Unexpected secondary action");
            var body=root.GetComponentsInChildren<ScrollRect>(true).Single(s=>s.name=="PopupBodyScroll");
            var lead=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="StartupDetails");var tail=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="PopupFollowup");
            var rule=root.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="BodySeparator");var primary=root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="StartupPrimary");
            void Draw(){foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true);Canvas.ForceUpdateCanvases();if(body.gameObject.activeInHierarchy)body.Rebuild(CanvasUpdate.PostLayout);camera.Render();}
            void Render(StartupScreenViewState state){view.Render(state);Draw();}
            void Save(string name){Draw();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/body-"+height+"-"+name+".png",pixels.EncodeToPNG());captures++;}
            var geometry=new JArray();var inputTimeline=new JArray();
            void Verify(StartupScreenViewState state,bool overflow,string stage)
            {
                Draw();lead.ForceMeshUpdate(true);tail.ForceMeshUpdate(true);
                var boundary=System.Text.RegularExpressions.Regex.Match(state.Details,@"\r?\n\r?\n");
                if((boundary.Success?lead.text+boundary.Value+tail.text:lead.text)!=state.Details)throw new InvalidOperationException("Copy loss "+stage);
                float size=state.Kind==StartupScreenKind.UpdateNotice?32:30;
                if(lead.fontSize!=size||(tail.gameObject.activeSelf&&tail.fontSize!=size)||lead.isTextOverflowing||(tail.gameObject.activeSelf&&tail.isTextOverflowing))throw new InvalidOperationException("Readable font/full fields "+stage);
                if(body.gameObject.activeInHierarchy!=overflow)throw new InvalidOperationException("Overflow routing "+stage);
                if(!tail.gameObject.activeSelf&&rule.gameObject.activeSelf)throw new InvalidOperationException("Single paragraph rule "+stage);
                if(overflow)
                {
                    var last=tail.gameObject.activeSelf?tail:lead;
                    float lastY=body.viewport.InverseTransformPoint(last.transform.TransformPoint(new Vector3(0,last.textBounds.min.y,0))).y;
                    if(body.verticalNormalizedPosition<=.001f&&(lastY<body.viewport.rect.yMin+3.9f||lastY>body.viewport.rect.yMax))throw new InvalidOperationException("Last glyph not reached "+lastY);
                    if(body.verticalNormalizedPosition>=.999f){float top=body.viewport.InverseTransformPoint(lead.transform.TransformPoint(new Vector3(0,lead.textBounds.max.y,0))).y;if(top>body.viewport.rect.yMax+.01f)throw new InvalidOperationException("First glyph clipped");}
                    geometry.Add(new JObject{["stage"]=stage,["text"]=state.Details,["font"]=size,["normalized"]=body.verticalNormalizedPosition,["viewportHeight"]=body.viewport.rect.height,["contentHeight"]=body.content.rect.height,["lastGlyphY"]=lastY,["viewportBottom"]=body.viewport.rect.yMin,["thumbValue"]=body.verticalScrollbar.value,["thumbSize"]=body.verticalScrollbar.size});
                }
            }
            PointerEventData Hit(RectTransform at)
            {
                var p=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,at.TransformPoint(at.rect.center))};
                var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(p,hits);if(hits.Count==0)throw new InvalidOperationException("No native hit "+at.name);
                p.pointerCurrentRaycast=p.pointerPressRaycast=hits[0];return p;
            }
            foreach(var pair in new[]{("normal",normal),("network",StartupScreenViewState.NetworkInterrupted("네트워크 상태를 확인한 뒤 다시 시도해 주세요.")),("internal",StartupScreenViewState.InternalAccessDenied())}){Render(pair.Item2);Verify(pair.Item2,false,pair.Item1);Save(pair.Item1);}
            string longText=string.Join(" ",Enumerable.Repeat("긴 안내문도 순서대로 보존하고 연결 상태를 확인한 뒤 다시 시도해 주세요.",6));
            foreach(var pair in new[]{("long-followup",StartupScreenViewState.UpdateNotice("",longText)),("long-lead",StartupScreenViewState.NetworkInterrupted(longText)),("long-single",StartupScreenViewState.DownloadError(longText))})
            {
                string name=pair.Item1;var state=pair.Item2;Render(state);int callbacksBeforeScroll=callbacks;Verify(state,true,name+"-top");Save(name+"-top");
                if(name=="long-followup")foreach(var item in new[]{lead.gameObject,tail.gameObject,rule.gameObject,body.verticalScrollbar.gameObject}){item.SetActive(false);Save("hide-"+item.name);item.SetActive(true);Save("restore-"+item.name);}
                var pointer=Hit(body.viewport);if(pointer.pointerCurrentRaycast.gameObject.GetComponentInParent<ScrollRect>()!=body)throw new InvalidOperationException("Viewport first hit");
                pointer.scrollDelta=new Vector2(0,-3);ExecuteEvents.ExecuteHierarchy(pointer.pointerCurrentRaycast.gameObject,pointer,ExecuteEvents.scrollHandler);wheels++;
                if(body.verticalNormalizedPosition>=.999f)throw new InvalidOperationException("Native wheel did not scroll");Verify(state,true,name+"-middle");Save(name+"-middle");
                pointer=Hit(body.viewport);ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.beginDragHandler);
                pointer.position+=Vector2.up*100;ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.dragHandler);pointer.position+=Vector2.up*2000;ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.endDragHandler);drags++;
                if(body.verticalNormalizedPosition>.001f)throw new InvalidOperationException("Native drag did not reach end");Verify(state,true,name+"-end");Save(name+"-end");
                Render(state);if(body.verticalNormalizedPosition>.001f)throw new InvalidOperationException("Same body reset reading");Save(name+"-same-body");
                var buttonPointer=Hit((RectTransform)primary.transform);if(buttonPointer.pointerCurrentRaycast.gameObject.GetComponentInParent<Button>()!=primary)throw new InvalidOperationException("Button intercepted by viewport");
                float position=body.verticalNormalizedPosition;buttonPointer.scrollDelta=new Vector2(0,3);ExecuteEvents.ExecuteHierarchy(buttonPointer.pointerCurrentRaycast.gameObject,buttonPointer,ExecuteEvents.scrollHandler);if(Mathf.Abs(body.verticalNormalizedPosition-position)>.001f)throw new InvalidOperationException("Outside wheel intercepted");
                if(callbacks!=callbacksBeforeScroll||!primary.interactable)throw new InvalidOperationException("Scroll issued command or locked button");
                int before=callbacks;ExecuteEvents.Execute(primary.gameObject,buttonPointer,ExecuteEvents.pointerClickHandler);if(callbacks!=before+1)throw new InvalidOperationException("Popup callback duplicate");
                inputTimeline.Add(new JObject{["case"]=name,["callbacksBeforeScroll"]=callbacksBeforeScroll,["callbacksAfterScroll"]=before,["primaryInteractableBeforeClick"]=true,["firstHit"]=primary.name,["callbacksAfterClick"]=callbacks,["normalizedBeforeClick"]=position,["primaryInteractableAfterClick"]=primary.interactable});
                body.verticalNormalizedPosition=1;Draw();var move=new AxisEventData(events.GetComponent<EventSystem>()){moveDir=MoveDirection.Down,moveVector=Vector2.down};events.GetComponent<EventSystem>().SetSelectedGameObject(body.verticalScrollbar.gameObject);ExecuteEvents.Execute(body.verticalScrollbar.gameObject,move,ExecuteEvents.moveHandler);keyboardMoves++;Draw();
                if(body.verticalNormalizedPosition>=.999f)throw new InvalidOperationException("Keyboard scrollbar move failed");Save(name+"-keyboard");
                Render(normal);Verify(normal,false,"normal-return");Save(name+"-normal-return");Render(state);if(body.verticalNormalizedPosition<.999f)throw new InvalidOperationException("New body did not start top");Save(name+"-new-entry");
            }
            var empty=StartupScreenViewState.UpdateNotice("","");Render(empty);Verify(empty,false,"empty-followup");Save("empty-followup");
            Render(StartupScreenViewState.UpdateNotice("",longText));root.SetActive(false);float inactive=body.verticalNormalizedPosition;var inactivePointer=new PointerEventData(events.GetComponent<EventSystem>()){scrollDelta=new Vector2(0,-3)};ExecuteEvents.Execute(body.gameObject,inactivePointer,ExecuteEvents.scrollHandler);if(Mathf.Abs(body.verticalNormalizedPosition-inactive)>.001f)throw new InvalidOperationException("Disabled scroll callback");root.SetActive(true);
            Render(StartupScreenViewState.Waiting());if(body.gameObject.activeInHierarchy||lead.GetComponentInParent<RectMask2D>()!=null)throw new InvalidOperationException("Popup mask leaked");
            Render(normal);Verify(normal,false,"normal-restored");Save("normal-restored");
            if(root.GetComponentsInChildren<ScrollRect>(true).Count(s=>s.name=="PopupBodyScroll")!=1)throw new InvalidOperationException("Duplicate scroll hierarchy");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["nativeWheels"]=wheels,["nativeContinuousDrags"]=drags,["keyboardMoves"]=keyboardMoves,["buttonCallbacks"]=callbacks,["inputTimeline"]=inputTimeline,["geometry"]=geometry,["outsideWheelNotIntercepted"]=true,["inactiveNoScroll"]=true,["nonPopupMaskRemoved"]=true,["localOnly"]=true});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
#endif
