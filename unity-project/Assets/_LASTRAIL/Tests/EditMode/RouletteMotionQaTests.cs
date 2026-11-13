using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class RouletteMotionQaTests
    {
        [UnityTest] public IEnumerator ResultIsRevealedAfterMotionAndReplayDoesNotRestart()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("MotionQA", typeof(RectTransform), typeof(Canvas));
            var output = Path.GetFullPath("../QA/RouletteMotion-20260917/run-"+System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            Debug.Log("Roulette motion QA: "+output);
            try
            {
                var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                ((RectTransform)root.transform).sizeDelta = new Vector2(720,1280);
                var popup = SharedRoulettePopup.Create(root.transform,font,"QA");
                popup.transform.localScale = Vector3.one*(720f/941);
                var rows = JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":3120},{\"id\":\"cur_machine_parts\",\"quantity\":20},{\"id\":\"cur_operator_exp\",\"quantity\":640}]");
                var result = new JObject { ["multiplier"]=4, ["rewardClaimId"]="motion-qa", ["rewardLines"]=rows };
                void Show(JObject receipt) => popup.Render(receipt,rows,"","확인",true,false,receipt!=null,false,false);
                Show(null);
                yield return null;
                Show(result);
                var center = root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="CommittedMultiplier");
                Assert.That(center.text,Is.Not.EqualTo("×4"),"Result must not precede deceleration");
                Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="QAMain").interactable,Is.False);
                var frames = new JArray(); float began = Time.unscaledTime;
                for(int i=0;Time.unscaledTime-began<7.5f;i++)
                {
                    if(Time.unscaledTime-began<6f)
                    {
                        Assert.That(center.text,Is.Not.EqualTo("×4"),"Spin must last at least six seconds");
                        Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="QAMain").interactable,Is.False);
                    }
                    string file=$"frame-{i:D4}.png";
                    frames.Add(new JObject{["file"]=file,["time"]=Time.unscaledTime-began});
                    Capture(root,720,1280,Path.Combine(output,file));
                    Show(result); // repeated network redraw must not reset motion
                    yield return new WaitForSecondsRealtime(1f/30);
                }
                File.WriteAllText(Path.Combine(output,"frames.json"),frames.ToString());
                Assert.That(center.text,Is.EqualTo("×4"));
                Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="QAMain").interactable,Is.True);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity").Select(t=>t.text),Is.EqualTo(new[]{"3,120","20","640"}));
                root.SetActive(false);root.SetActive(true);Show(result);
                Assert.That(center.text,Is.EqualTo("×4"),"Reopen settled receipt without spinning again");
                rows[0]["quantity"]=9007199254740991L;
                rows.Add(new JObject{["id"]="cur_tactical_data",["quantity"]=9007199254740991L});
                Show(result);
                yield return null;
                foreach(int width in new[]{360,480})
                {
                    int height=width*16/9;
                    popup.transform.localScale=Vector3.one*(width/941f);
                    Capture(root,width,height,Path.Combine(output,$"long-quantity-{width}.png"));
                    foreach(var text in root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity"))
                    { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing,Is.False,text.text); }
                    var scroll=root.GetComponentInChildren<ScrollRect>();scroll.horizontalNormalizedPosition=1;
                    Capture(root,width,height,Path.Combine(output,$"last-card-{width}.png"));
                }
            }
            finally { Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        static void Capture(GameObject root,int width,int height,string path)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var cameraObject=new GameObject("QACamera");var camera=cameraObject.AddComponent<Camera>();
            var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=(float)width/height;
                camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.04f,.05f);
                camera.targetTexture=target;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                ((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
        }
    }
}
