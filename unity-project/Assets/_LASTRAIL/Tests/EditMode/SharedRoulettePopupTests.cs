using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public sealed class SharedRoulettePopupTests {
  [Test] public void LongQuantitiesKeepReadableTypeAndExactDigitsByWideningCards(){
   var root=new GameObject("LongQuantityQA",typeof(RectTransform),typeof(Canvas));
   try{
    ((RectTransform)root.transform).sizeDelta=new Vector2(360,640);
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    var view=SharedRoulettePopup.Create(root.transform,font,"QA");view.transform.localScale=Vector3.one*(360f/941);
    var rows=JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":9007199254740991},{\"id\":\"cur_machine_parts\",\"quantity\":20},{\"id\":\"cur_operator_exp\",\"quantity\":640}]");
    view.Render(new JObject{["multiplier"]=4,["rewardClaimId"]="long",["rewardLines"]=rows},null,"","확인",true,false,true,false,false);
    Canvas.ForceUpdateCanvases();
    var quantity=root.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="RewardQuantity");quantity.ForceMeshUpdate();
    Assert.That(quantity.text,Is.EqualTo("9,007,199,254,740,991"));
    Assert.That(quantity.fontSize,Is.GreaterThanOrEqualTo(32),"Long quantities must not become microscopic");
    Assert.That(quantity.isTextOverflowing,Is.False,$"rect={quantity.rectTransform.rect}, preferred={quantity.GetPreferredValues()}, margin={quantity.margin}, lines={quantity.textInfo.lineCount}, overflowChar={quantity.firstOverflowCharacterIndex}");
    Assert.That(quantity.textInfo.lineCount,Is.EqualTo(1));
    Assert.That(root.GetComponentInChildren<ScrollRect>().horizontal,Is.True,"Three wide cards also need scroll access");
    typeof(AccountScreenIntegrationTests).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{root,"readable-long-quantity.png"});
   }finally{Object.DestroyImmediate(root);}
  }
  [Test] public void CommittedResultUsesServerQuantitiesAndOnlyConfirmation(){
   var root=new GameObject("SharedRouletteQA",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
   try{
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    var view=SharedRoulettePopup.Create(root.transform,font,"QA");view.transform.localScale=Vector3.one*(720f/941);
    int calls=0;view.Bind(()=>calls++,null,null,null);view.Bind(()=>calls++,null,null,null);
    var rows=JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":3120},{\"id\":\"cur_machine_parts\",\"quantity\":20},{\"id\":\"cur_operator_exp\",\"quantity\":640}]");
    var result=new JObject{["multiplier"]=4,["rewardClaimId"]="qa",["rewardLines"]=rows};
    view.Render(result,new JArray(),"지급 완료","확인",true,false,true,false,false);
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity").Select(t=>t.text),Is.EqualTo(new[]{"3,120","20","640"}));
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CommittedMultiplier").text,Is.EqualTo("×4"));
    var buttons=root.GetComponentsInChildren<Button>();Assert.That(buttons.Length,Is.EqualTo(1));buttons[0].onClick.Invoke();Assert.That(calls,Is.EqualTo(1));view.Unbind();buttons[0].onClick.Invoke();Assert.That(calls,Is.EqualTo(1));
    typeof(AccountScreenIntegrationTests).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{root,"shared-roulette-result-4x.png"});
    var retained=root.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="RewardQuantity").transform.parent;
    rows.Add(new JObject{["id"]="cur_tactical_data",["quantity"]=80});
    view.Render(result,null,"","확인",true,false,true,false,false);
    Assert.AreSame(retained,root.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="RewardQuantity").transform.parent);
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="RewardQuantity"),Is.EqualTo(4));
    Assert.That(root.GetComponentInChildren<ScrollRect>().horizontal,Is.True);
    view.ConfigureProbability("현재 제공되지 않습니다.",new[]{2});
    view.Render(null,rows,"광고 확인 중…","다시 확인",true,false,false,true,false);
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("Multiplier")).All(t=>t.text=="2배"),Is.True);
    view.Render(null,rows,"기본 보급을 이미 수령했습니다.","확인",true,false,true,false,false);
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="RewardQuantity"),Is.False);
    Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RouletteNotice").text,Does.Contain("이미 수령"));
    view.Bind(()=>calls++,null,null,null);
    view.Render(null,rows,"","룰렛 돌리기",true,false,false,false,false);
    var odds=root.GetComponentsInChildren<Button>().Single(b=>b.name=="RouletteOdds");
    view.Unbind();odds.onClick.Invoke();
    Assert.That(view.transform.Find("RoulettePanel/ProbabilityDetails").gameObject.activeSelf,Is.False);
   }finally{Object.DestroyImmediate(root);}
  }
 }
}
