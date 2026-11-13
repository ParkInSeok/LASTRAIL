using System.Collections;
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
    public sealed class RouletteRewardRevealTests
    {
        [Test] public void EveryCountupValueFitsItsFixedCardWithoutWrapping()
        {
            var root=new GameObject("CountupWidthQA",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=SharedRouletteVisual.Create(root.transform,font);
                var rows=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"cur_credits\",\"quantity\":9007199254740991}]");
                // Four cards reproduce the actual scrolled layout; later exercise the long maximum too.
                for(int i=0;i<3;i++)rows.Add(rows[0].DeepClone());
                foreach(long endpoint in new[]{9007199254740991L,long.MaxValue})
                {
                    rows[0]["quantity"]=endpoint;view.Present(new JObject{["rewardClaimId"]=endpoint.ToString(),["multiplier"]=4,["rewardLines"]=rows},null);
                    var quantity=view.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="RewardQuantity");
                    var card=(RectTransform)quantity.transform.parent;float width=card.rect.width;
                    void Check(string value)
                    {
                        quantity.text=value;quantity.ForceMeshUpdate();
                        Assert.That(quantity.textInfo.lineCount,Is.EqualTo(1),value);
                        Assert.That(quantity.isTextOverflowing,Is.False,value);
                        Assert.That(quantity.GetPreferredValues(value,Mathf.Infinity,Mathf.Infinity).x,Is.LessThanOrEqualTo(quantity.rectTransform.rect.width),value);
                        Assert.That(card.rect.width,Is.EqualTo(width));Assert.That(quantity.fontSize,Is.EqualTo(38));
                    }
                    Check("5,310,985,640,787,964");
                    for(int step=0;step<=1000;step++)Check(((long)((decimal)endpoint*step/1000)).ToString("N0",System.Globalization.CultureInfo.InvariantCulture));
                    for(int digit=0;digit<=9;digit++)
                    {
                        string probe=new string((char)('0'+digit),endpoint.ToString().Length);
                        for(int index=probe.Length-3;index>0;index-=3)probe=probe.Insert(index,",");
                        Check(probe);
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void PreviewBoundaryPreservesExplicitKindWithoutInventingLegacyKind()
        {
            var explicitKind=new LastRail.OfflineRewards.OfflineRewardLineDto("a","cur_credits","credits",780,"개","OFFLINE_REWARD",kind:"CURRENCY");
            Assert.That(explicitKind.Kind,Is.EqualTo("CURRENCY"));
            var legacy=new LastRail.OfflineRewards.OfflineRewardLineDto("a","cur_credits","credits",780,"개","OFFLINE_REWARD");
            Assert.That(legacy.Kind,Is.Null);
        }
        [UnityTest] public IEnumerator ActualHostsDropSpinningViewOnAccountReplacement()
        {
            yield return new EnterPlayMode();
            foreach(bool combat in new[]{false,true})
            {
                var root=new GameObject("AccountReplacementQA",typeof(RectTransform),typeof(Canvas));
                ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
                var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
                var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
                var wire=new RouletteFullFlowQaTests.Wire(combat,false){Status="AD_VERIFIED"};
                var journal=new RouletteFullFlowQaTests.Journal();string run=(string)wire.Lifecycle["runId"];
                journal.Row=new PendingAccountAction("a",combat?"combat-ad-presented":"ad-presented",combat?run:(string)wire.Pending["intentId"],0,"local_switch",wire.Pending.ToString());
                var session=new LastRail.Auth.GameApiAuthSessionSnapshot("a","ACTIVE",new LastRail.Auth.GameApiAccessToken("lst1.fixture"),System.DateTime.UtcNow.AddHours(1),new LastRail.Auth.GameApiRefreshToken("fixture"),System.DateTime.UtcNow.AddDays(1));
                using var account=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
                try
                {
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                    var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                    Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                    if(combat){deck.OpenRunLifecycle(run);yield return null;Find("VictoryAd").onClick.Invoke();}
                    else{Find("Shortcut3").onClick.Invoke();yield return null;Find("OpenOfflineRoulette").onClick.Invoke();}
                    if(!combat)Assert.That(account.Offline.RewardLines.All(r=>r.Kind=="CURRENCY"),Is.True,"Wire boundary must preserve explicit kind");
                    yield return null;var oldClick=Find(combat?"CombatAdMain":"AdMainAction").onClick;
                    oldClick.Invoke();wire.Complete();yield return null;
                    var visual=root.GetComponentInChildren<SharedRouletteVisual>();Assert.That(visual.IsAnimating,Is.True);
                    deck.BindAccount(null);oldClick.Invoke();
                    Assert.That(visual.IsAnimating,Is.False);Assert.That(root.GetComponentInChildren<SharedRoulettePopup>(),Is.Null);
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="RewardQuantity"),Is.False);
                    Assert.That(wire.Spins,Is.EqualTo(1));Assert.That(journal.Row,Is.Not.Null,"Stale click cannot acknowledge another account's receipt");
                    yield return null;
                }
                finally{Object.DestroyImmediate(root);}
            }
            yield return new ExitPlayMode();
        }
        [Test] public void SnapshotsMatchExactIdsAndKindsWithoutEstimatingMissingRows()
        {
            var basis=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"a\",\"quantity\":780},{\"kind\":\"CURRENCY\",\"id\":\"b\",\"quantity\":5}]");
            var final=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"b\",\"quantity\":20},{\"kind\":\"CURRENCY\",\"id\":\"a\",\"quantity\":3120},{\"kind\":\"ITEM\",\"id\":\"a\",\"quantity\":99}]");
            var model=new RouletteRewardCountup();model.CapturePreview(basis);basis[0]["quantity"]=9999;model.Begin(final);final[1]["quantity"]=9999;
            Assert.That(model.Text(0,0),Is.EqualTo("5"));Assert.That(model.Text(1,0),Is.EqualTo("780"));Assert.That(model.Text(1,.5),Is.EqualTo("1,950"));
            Assert.That(model.Text(1,1),Is.EqualTo("3,120"));Assert.That(model.Text(2,.99),Is.EqualTo("—"));Assert.That(model.Text(2,1),Is.EqualTo("99"));
            model.Clear();model.Begin(final);Assert.That(model.Text(0,0),Is.EqualTo("—"));Assert.That(model.Text(0,1),Is.EqualTo("20"));
        }
        [Test] public void DuplicateOrExcludedSourceIsNeutralAndLongEndpointIsExact()
        {
            var model=new RouletteRewardCountup();
            var basis=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"a\",\"quantity\":1,\"source\":\"RUN_CLEAR\"}]");
            var final=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"a\",\"quantity\":99,\"source\":\"FIRST_CLEAR\"}]");
            model.CapturePreview(basis);model.Begin(final);Assert.That(model.Text(0,.5),Is.EqualTo("—"));Assert.That(model.Text(0,1),Is.EqualTo("99"));
            final[0]["source"]="RUN_CLEAR";final.Add(final[0].DeepClone());model.Begin(final);Assert.That(model.Text(0,0),Is.EqualTo("—"));Assert.That(model.Text(1,0),Is.EqualTo("—"));
            final.RemoveAt(1);basis.Add(basis[0].DeepClone());model.CapturePreview(basis);model.Begin(final);Assert.That(model.Text(0,0),Is.EqualTo("—"));
            basis.RemoveAt(1);basis[0]["quantity"]=0;final[0]["quantity"]=long.MaxValue;model.CapturePreview(basis);model.Begin(final);
            Assert.That(model.Text(0,.5),Is.EqualTo("4,611,686,018,427,387,903"));Assert.That(model.Text(0,1),Is.EqualTo("9,223,372,036,854,775,807"));
            basis[0]["kind"]=null;model.CapturePreview(basis);model.Begin(final);Assert.That(model.Text(0,0),Is.EqualTo("—"));
        }

        [UnityTest] public IEnumerator UnbindClearsOldDataAndReopenDoesNotRestart()
        {
            yield return new EnterPlayMode();var root=new GameObject("CancelRevealQA",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var popup=SharedRoulettePopup.Create(root.transform,font,"QA");int calls=0;popup.Bind(()=>calls++,null,null,null);
                var rows=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"cur_credits\",\"quantity\":780}]");
                var final=(JArray)rows.DeepClone();final[0]["quantity"]=3120;var receipt=new JObject{["multiplier"]=4,["rewardClaimId"]="old-owner",["rewardLines"]=final};
                popup.Render(null,rows,"","",true,false,false,false,false);yield return null;popup.Render(receipt,rows,"","확인",true,false,true,false,false);yield return new WaitForSecondsRealtime(.1f);
                popup.Unbind();Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="780"||t.text=="3,120"),Is.False);
                var button=root.GetComponentsInChildren<Button>().Single(b=>b.name=="QAMain");button.onClick.Invoke();Assert.That(calls,Is.Zero);
                popup.Bind(()=>calls++,null,null,null);popup.Bind(()=>calls++,null,null,null);
                rows[0]["quantity"]=11;final[0]["quantity"]=44;receipt["rewardClaimId"]="new-owner";
                popup.Render(null,rows,"","",true,false,false,false,false);popup.Render(receipt,rows,"","확인",true,false,true,false,false);
                root.SetActive(false);root.SetActive(true);popup.Bind(()=>calls++,null,null,null);popup.Render(receipt,rows,"","확인",true,false,true,false,false);yield return null;
                Assert.That(root.GetComponentInChildren<SharedRouletteVisual>().IsAnimating,Is.False);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RewardQuantity").text,Is.EqualTo("44"));
                button.onClick.Invoke();Assert.That(calls,Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(root);}yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator SpinKeepsBaseCardsThenCountsAndUnlocks()
        {
            yield return new EnterPlayMode();
            var root=new GameObject("RewardRevealQA",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var popup=SharedRoulettePopup.Create(root.transform,font,"QA");popup.Bind(()=>{},null,null,null);
                var bases=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"cur_credits\",\"quantity\":780}]");
                var final=JArray.Parse("[{\"kind\":\"CURRENCY\",\"id\":\"cur_credits\",\"quantity\":3120}]");
                var receipt=new JObject{["multiplier"]=4,["rewardClaimId"]="reveal-test",["rewardLines"]=final};
                popup.Render(null,bases,"","돌리기",true,false,false,false,false);yield return null;
                popup.Render(receipt,bases,"","확인",true,false,true,false,false);
                yield return new WaitForSecondsRealtime(.15f);
                var quantity=root.GetComponentsInChildren<TMP_Text>().SingleOrDefault(t=>t.name=="RewardQuantity");
                Assert.That(quantity,Is.Not.Null,"Base reward cards stay visible while spinning");
                Assert.That(quantity.text,Is.EqualTo("780"),"Never show committed total before count-up");
                var center=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CommittedMultiplier");
                Assert.That(center.text,Is.Not.EqualTo("×4"));
                var button=root.GetComponentsInChildren<Button>().Single(b=>b.name=="QAMain");Assert.That(button.interactable,Is.False);
                yield return new WaitForSecondsRealtime(6.5f);
                Assert.That(center.text,Is.EqualTo("×4"));Assert.That(button.interactable,Is.False,"Count-up also gates confirmation");
                Assert.That(quantity.text,Is.Not.EqualTo("780"));Assert.That(quantity.text,Is.Not.EqualTo("3,120"));
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(quantity.text,Is.EqualTo("3,120"));Assert.That(button.interactable,Is.True);
                Assert.That(bases[0]["quantity"].Value<long>(),Is.EqualTo(780));Assert.That(final[0]["quantity"].Value<long>(),Is.EqualTo(3120));
            }
            finally{Object.DestroyImmediate(root);}
            yield return new ExitPlayMode();
        }
    }
}
