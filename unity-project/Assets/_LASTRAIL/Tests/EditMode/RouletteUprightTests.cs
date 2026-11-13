using System.Collections;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class RouletteUprightTests
    {
        [UnityTest] public IEnumerator OrbitingLabelsStayUprightAndInterruptedReceiptRestores()
        {
            yield return new EnterPlayMode();
            var root=new GameObject("UprightQA",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var popup=SharedRoulettePopup.Create(root.transform,font,"QA");
                var visual=root.GetComponentInChildren<SharedRouletteVisual>();
                var labels=root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("Multiplier")).ToArray();
                var rotor=(RectTransform)labels[0].transform.parent;
                var wheel=rotor.parent;
                var centers=labels.Select(t=>rotor.InverseTransformPoint(t.rectTransform.TransformPoint(t.rectTransform.rect.center))).ToArray();
                for(int i=0;i<labels.Length;i++)
                {
                    float angle=(90-i*45)*Mathf.Deg2Rad;
                    Assert.That(Vector3.Distance(centers[i],new Vector3(Mathf.Cos(angle)*190,Mathf.Sin(angle)*190,0)),Is.LessThan(.01f),"Preserve original sector position");
                }
                var center=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CommittedMultiplier");
                var centerRotation=center.transform.rotation;
                var rows=JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":3120}]");
                var receipt=new JObject{["multiplier"]=4,["rewardClaimId"]="upright-test",["rewardLines"]=rows};
                void Show(JObject result)=>popup.Render(result,rows,"","확인",true,false,result!=null,false,false);
                void Check()
                {
                    for(int i=0;i<labels.Length;i++)
                    {
                        Assert.That(Quaternion.Angle(labels[i].transform.rotation,wheel.rotation),Is.LessThan(.01f),"Screen orientation "+labels[i].name);
                        Assert.That(Vector3.Distance(labels[i].rectTransform.TransformPoint(labels[i].rectTransform.rect.center),rotor.TransformPoint(centers[i])),Is.LessThan(.01f),"Label center must remain attached to its sector");
                    }
                    Assert.That(Quaternion.Angle(center.transform.rotation,centerRotation),Is.LessThan(.01f));
                }
                Show(null);yield return null;Show(receipt);
                for(int i=0;i<4;i++)
                {
                    yield return new WaitForSecondsRealtime(.12f);Assert.That(visual.IsAnimating,Is.True);
                    Check();Assert.That(center.text,Is.Not.EqualTo("×4"));
                }
                Assert.That(Quaternion.Angle(rotor.localRotation,Quaternion.identity),Is.GreaterThan(.1f),"Exercise a rotated rotor");
                root.SetActive(false);root.SetActive(true);Show(receipt);yield return null;
                Assert.That(visual.IsAnimating,Is.False);Check();
                Assert.That(Quaternion.Angle(rotor.localRotation,Quaternion.identity),Is.LessThan(.01f));
                Assert.That(center.text,Is.EqualTo("×4"));Assert.That(labels[0].text,Is.EqualTo("4배"));
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RewardQuantity").text,Is.EqualTo("3,120"));
            }
            finally{Object.DestroyImmediate(root);}
            yield return new ExitPlayMode();
        }
    }
}
