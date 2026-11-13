#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OperatorCodexQa
{
    public static void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatCodex/kyle-alpha-baseline-v05.png");
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var steel=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatCodex/codex-dark-steel-surface-v01.png");steel.npotScale=TextureImporterNPOTScale.None;steel.mipmapEnabled=true;steel.filterMode=FilterMode.Trilinear;steel.textureCompression=TextureImporterCompression.Uncompressed;steel.maxTextureSize=4096;steel.wrapMode=TextureWrapMode.Clamp;steel.SaveAndReimport();
            var cases=new JArray();foreach(int height in new[]{1280,1600})Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual lobby codex menu and canonical local identity/skill-name catalogs. No AccountScreenRuntime, ownership/discovery inference, network or mutation. Kyle true-alpha baseline candidate via independent screen-specific UV crop; exact13 visual acceptance remains open.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("OperatorCodexQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);
            var clicks=new JArray();
            void Click(string name){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);if(!button.interactable)throw new InvalidOperationException("Disabled "+name);var rect=(RectTransform)button.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new System.Collections.Generic.List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("First hit mismatch "+name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks.Add(name);}
            int count=0;var overflow=new JArray();
            void Record(string name)
            {
                Canvas.ForceUpdateCanvases();deck.EvaluateLayout();foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&label.GetComponentInParent<OperatorCodexView>()!=null)overflow.Add(name+":"+label.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"codex-"+height+"-"+name+".png"),pixels.EncodeToPNG());count++;
            }
            Click("LobbyMenu");Click("OpenOperatorCodex");Record("kyle-skills");
            var view=root.GetComponentInChildren<OperatorCodexView>();var person=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="CodexPortrait");var room=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="CodexRoom");
            if(person.texture==null)throw new InvalidOperationException("Kyle independent alpha missing");
            var original=room.texture;person.enabled=false;Record("hide-person");person.enabled=true;room.enabled=false;Record("hide-room");room.enabled=true;room.texture=Texture2D.whiteTexture;Record("swap-room");room.texture=original;Record("restored");
            var plates=view.GetComponentsInChildren<RawImage>().Where(x=>x.name=="ActiveSkillPlate"||x.name=="UltimateSkillPlate").ToArray();if(plates.Length!=2)throw new InvalidOperationException("Two independent skill plates required");
            var plateTextures=plates.Select(p=>p.texture).ToArray();foreach(var plate in plates)plate.enabled=false;Record("hide-skill-plates");foreach(var plate in plates)plate.enabled=true;Record("restore-skill-plates");
            var frames=plates.Select(p=>p.transform.Find("Frame").GetComponent<Image>()).ToArray();foreach(var frame in frames)frame.enabled=false;Record("hide-skill-frames");foreach(var frame in frames)frame.enabled=true;Record("restore-skill-frames");
            foreach(var plate in plates)plate.texture=Texture2D.whiteTexture;Record("swap-skill-plates");for(int i=0;i<plates.Length;i++)plates[i].texture=plateTextures[i];Record("restored-skill-material");
            var returnSkin=view.GetComponentsInChildren<LastRail.Auth.IndependentMetalSurface>().Single(s=>s.transform.parent.name=="CodexListReturn");
            foreach(var role in new[]{"frame","plate","lights"}){if(role=="frame")returnSkin.Frame.enabled=false;else if(role=="plate")returnSkin.Backplate.enabled=false;else returnSkin.Lights.gameObject.SetActive(false);Record("hide-return-"+role);returnSkin.Frame.enabled=true;returnSkin.Backplate.enabled=true;returnSkin.Lights.gameObject.SetActive(true);Record("restore-return-"+role);}
            var tags=view.GetComponentsInChildren<Image>().Where(x=>x.name=="ActiveTagPlate"||x.name=="UltimateTagPlate").ToArray();foreach(var tag in tags)tag.gameObject.SetActive(false);Record("hide-tag-plates");foreach(var tag in tags)tag.gameObject.SetActive(true);Record("restore-tag-plates");
            var dividers=view.GetComponentsInChildren<Image>().Where(x=>x.name.StartsWith("SkillDivider")).ToArray();foreach(var line in dividers)line.enabled=false;Record("hide-dividers");foreach(var line in dividers)line.enabled=true;Record("restore-dividers");
            var tabSkins=view.GetComponentsInChildren<LastRail.Auth.IndependentMetalSurface>().Where(s=>s.transform.parent.name.StartsWith("CodexTab")).ToArray();if(tabSkins.Length!=3)throw new InvalidOperationException("Three tab surfaces required");var tabLights=tabSkins.Select(s=>s.Lights.gameObject.activeSelf).ToArray();
            foreach(var role in new[]{"frame","plate","lights"}){foreach(var skin in tabSkins){if(role=="frame")skin.Frame.enabled=false;else if(role=="plate")skin.Backplate.enabled=false;else skin.Lights.gameObject.SetActive(false);}Record("hide-tab-"+role);for(int i=0;i<tabSkins.Length;i++){tabSkins[i].Frame.enabled=true;tabSkins[i].Backplate.enabled=true;tabSkins[i].Lights.gameObject.SetActive(tabLights[i]);}Record("restore-tab-"+role);}
            var tabIcons=view.GetComponentsInChildren<CodexTabGlyph>();foreach(var glyph in tabIcons)glyph.enabled=false;Record("hide-tab-icons");foreach(var glyph in tabIcons)glyph.enabled=true;Record("restore-tab-icons");
            var arrows=view.GetComponentsInChildren<CodexBackChevron>();foreach(var arrow in arrows)arrow.enabled=false;Record("hide-arrows");foreach(var arrow in arrows)arrow.enabled=true;Record("restore-arrows");
            var shieldLayers=view.GetComponentsInChildren<CodexShieldLayer>();
            foreach(var role in new[]{CodexShieldLayer.Role.Center,CodexShieldLayer.Role.Bevel,CodexShieldLayer.Role.Rim,CodexShieldLayer.Role.Rays}){var layers=shieldLayers.Where(l=>l.LayerRole==role).ToArray();if(layers.Length!=(role==CodexShieldLayer.Role.Rays?1:2))throw new InvalidOperationException("Shield layer ownership "+role);foreach(var layer in layers){if(layer.raycastTarget)throw new InvalidOperationException("Shield intercepts input");layer.enabled=false;}Record("hide-shield-"+role);foreach(var layer in layers)layer.enabled=true;Record("restore-shield-"+role);}
            var centers=shieldLayers.Where(l=>l.LayerRole==CodexShieldLayer.Role.Center).ToArray();var steelTexture=centers[0].LayerTexture;foreach(var center in centers)center.LayerTexture=Texture2D.whiteTexture;Record("swap-shield-metal");foreach(var center in centers)center.LayerTexture=steelTexture;Record("restore-shield-metal");
            var originalColors=plates.Select(p=>p.color).ToArray();foreach(var plate in plates){plate.texture=Texture2D.whiteTexture;plate.color=Color.white;}Record("shield-white-backdrop");foreach(var plate in plates)plate.color=new Color(.7f,.2f,.45f);Record("shield-color-backdrop");for(int i=0;i<plates.Length;i++){plates[i].texture=plateTextures[i];plates[i].color=originalColors[i];}Record("restore-shield-backdrop");
            var geometry=new JArray();foreach(var r in view.GetComponentsInChildren<RectTransform>().Where(r=>new[]{"CodexHeader","CodexBrandPlate","HeaderBackArrow","ReturnArrow","CodexTab0","CodexTab1","CodexTab2","ActiveSkillPlate","UltimateSkillPlate","ActiveShieldSymbol","UltimateShieldSymbol","ActiveSkillName","UltimateSkillName"}.Contains(r.name))){var corners=new Vector3[4];r.GetWorldCorners(corners);var min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);geometry.Add(new JObject{["name"]=r.name,["logicalWidth"]=r.rect.width,["logicalHeight"]=r.rect.height,["screenX"]=min.x,["screenTop"]=height-max.y,["screenWidth"]=max.x-min.x,["screenHeight"]=max.y-min.y});}
            Click("CodexTab0");Record("kyle-basic");Click("CodexTab2");if(view.CurrentState.Body!=OperatorCodexEntry.Unknown)throw new InvalidOperationException("Unapproved biography exposed");Record("kyle-story-unknown");
            Click("BackOperatorCodex");Record("list");Click("CodexOperator1");Click("CodexTab1");Record("reina-skills");Click("BackOperatorCodex");Click("CodexOperator14");Record("last-operator-unknown-skills");
            Click("BackOperatorCodex");Click("CodexListReturn");if(deck.IsDialogOpen)throw new InvalidOperationException("Codex return failed");Record("lobby-return");
            if(overflow.Count!=0)throw new InvalidOperationException("Codex overflow "+overflow);
            cases.Add(new JObject{["height"]=height,["captures"]=count,["nativeFirstHitClicks"]=clicks,["geometry"]=geometry,["tabs"]=3,["operatorCount"]=view.CurrentState.Entries.Count,["networkRequests"]=0,["lobbyReturn"]=true,["overflow"]=overflow});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif


