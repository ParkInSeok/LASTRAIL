using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class CombatControlsPreview
{
    public static void Capture()
    {
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"));
        var cameraGo=new GameObject("ControlsPreviewCamera");var camera=cameraGo.AddComponent<Camera>();var target=new RenderTexture(720,1280,24);
        try{
            BasicCombatSimulation Fresh(){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);}
            var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();driver.Advance(500000*40,40);go.AddComponent<CombatLiveHudBinding>().Bind(driver);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(720,1280,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,720,1280),0,0);texture.Apply();
            File.WriteAllBytes("D:/Project/LASTRAIL/unity-project/QA/ScreenIntegration/manual-controls-preview.png",texture.EncodeToPNG());RenderTexture.active=previous;Object.DestroyImmediate(texture);
        }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraGo);target.Release();Object.DestroyImmediate(target);}
    }
}
