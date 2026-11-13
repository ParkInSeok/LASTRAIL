#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PreparationAssetQualityQa
{
    public static void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D output required");
            Directory.CreateDirectory(output);var assets=new JArray();
            var names=new[]{"train_locomotive-v03","train_carriage_armored-v03","train_carriage_cargo-v03","support_fire-v01","support_barrier-v01","support_repair-v01"};
            foreach(var name in names)
            {
                string key="CombatSecondFinal/"+name,path=LastRail.Resources.RemoteContentCatalog.AssetPath(key);
                var imported=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var original=new Texture2D(2,2);try
                {
                    original.LoadImage(File.ReadAllBytes(path));int x0=original.width,y0=original.height,x1=-1,y1=-1,solid=0,clear=0,partial=0;
                    var pixels=original.GetPixels32();for(int y=0;y<original.height;y++)for(int x=0;x<original.width;x++){byte a=pixels[y*original.width+x].a;if(a==0)clear++;else if(a==255)solid++;else partial++;if(a>=128){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}}
                    assets.Add(new JObject{["key"]=key,["path"]=Path.GetFullPath(path),["sourceWidth"]=original.width,["sourceHeight"]=original.height,["loadedWidth"]=imported.width,["loadedHeight"]=imported.height,["loadedFormat"]=imported.format.ToString(),["filter"]=imported.filterMode.ToString(),["mipmapCount"]=imported.mipmapCount,["defaultMax"]=importer.GetDefaultPlatformTextureSettings().maxTextureSize,["compression"]=importer.textureCompression.ToString(),["alphaZero"]=clear,["alphaOpaque"]=solid,["alphaPartial"]=partial,["alpha128BoundsBottomLeft"]=new JArray(x0,y0,x1,y1)});
                }
                finally{UnityEngine.Object.DestroyImmediate(original);}
            }
            var views=new JArray();foreach(int height in new[]{1280,1600})
            {
                var root=new GameObject("AssetContract",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(941,height*941f/720f);
                try
                {
                    var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                    var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,null,null,null);
                    view.SetViewportHeight(height*941f/720f);
                    for(int i=0;i<3;i++)view.Carriage(i,"QA", "CombatSecondFinal/"+names[i]);
                    view.Support(0,"support_fire","지원 사격");view.Support(1,"barrier","방어막");view.Support(2,"emergency_repair","긴급 수리");
                    foreach(var image in view.GetComponentsInChildren<RawImage>().Where(x=>x.name.StartsWith("PreparationCarriage")||x.name.StartsWith("PreparationSupport")))
                    {
                        var r=image.rectTransform;views.Add(new JObject{["height"]=height,["name"]=image.name,["texture"]=image.texture?.name,["enabled"]=image.enabled,["color"]=image.color.ToString(),["uv"]=image.uvRect.ToString(),["logicalRect"]=new JArray(r.anchoredPosition.x,-r.anchoredPosition.y,r.sizeDelta.x,r.sizeDelta.y),["derivedScreenSizeAt720"]=new JArray(r.sizeDelta.x*720f/941f,r.sizeDelta.y*720f/941f),["material"]=image.material.name,["shader"]=image.material.shader.name,["raycastTarget"]=image.raycastTarget});
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Read-only actual imported textures and actual Preparation View properties with explicit fixture assignments; no rendered image edit, import setting change or network.",["assets"]=assets,["views"]=views}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
