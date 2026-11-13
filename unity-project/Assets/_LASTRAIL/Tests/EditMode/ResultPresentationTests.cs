using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using LastRail.Tests.EditMode;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class ResultPresentationTests
{
    const string Folder="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
    sealed class Wire:IRunApiHttpTransport
    {
        public readonly TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();public bool Missing,Empty;public int Writes;public string Lifecycle;
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){if(r.Method!="GET"){Writes++;throw new InvalidOperationException("Unexpected mutation");}await Gate.Task;
            if(r.EndpointPath.EndsWith("/lifecycle"))return RunApiHttpResponse.Success(200,Lifecycle??File.ReadAllText(Folder+"result-lost-finish-restored.json"));
            if(r.EndpointPath.Contains("/reward-preview/")){if(Missing)return RunApiHttpResponse.Success(404,File.ReadAllText(Folder+"result-preview-missing.json"));var data=JObject.Parse(File.ReadAllText(Folder+"result-preview.json"));if(Empty)data["rewardLines"]=new JArray();return RunApiHttpResponse.Success(200,data.ToString());}
            return RunApiHttpResponse.Success(404,"{}");
        }
    }
    sealed class Journal:IAccountActionJournal{public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>throw new InvalidOperationException();public bool Remove(PendingAccountAction a)=>throw new InvalidOperationException();}
    [TestCase("ABANDONED","ABANDONED","작전 포기")]
    [TestCase("COMPLETED","FAILURE","결과 확인 필요")]
    [TestCase("FAILED","VICTORY","결과 확인 필요")]
    public async Task NonvictoryReasonNeverUsesUnconfirmedVictoryPresentation(string status,string outcome,string title)
    {
        var data=JObject.Parse(File.ReadAllText(Folder+"result-lost-finish-restored.json"));data["runStatus"]=status;data["finish"]["outcome"]=outcome;
        var wire=new Wire{Lifecycle=data.ToString()};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        var root=new GameObject("NonvictoryTest",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
        try{var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenRunLifecycle("run-smoke-1");
            wire.Gate.SetResult(true);for(int i=0;i<200&&state.RunResultsLoading;i++)await Task.Delay(1);
            Assert.IsFalse(state.RunResultsLoading);Assert.IsNotNull(state.Lifecycle,state.LifecycleNotice);
            Assert.IsNull(root.GetComponentInChildren<RunVictoryView>());
            Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="DialogTitle").text,Is.EqualTo(title));
            Assert.That(new RunVictoryDisplayState(state,"run-smoke-1",new MissionShellMissionRowState[0]).Title,Is.EqualTo("결과 확인 필요"));
            Assert.That(wire.Writes,Is.Zero);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)]
    public async Task DelayedRecordedResultShowsLoadingThenVerifiedOutcomeOrRetry(bool missing,bool empty)
    {
        var wire=new Wire{Missing=missing,Empty=empty};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        var root=new GameObject("ResultPresentationTest",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        try{var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenRunLifecycle("run-smoke-1");
            Assert.IsTrue(state.RunResultsLoading);Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="DialogTitle").text,Is.EqualTo("결과 확인 중"));
            wire.Gate.SetResult(true);for(int i=0;i<200&&state.RunResultsLoading;i++)await Task.Delay(1);Assert.IsFalse(state.RunResultsLoading);Assert.IsNotNull(state.Lifecycle,state.LifecycleNotice);
            Assert.AreEqual("작전 실패",root.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="DialogTitle").text);
            foreach(string name in new[]{"ResultEnvironment","ResultTrain"}){var image=root.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);Assert.IsNotNull(image.texture);Assert.IsFalse(image.raycastTarget);}
            var record=root.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="ResultRecordText").text;
            Assert.That(record,Does.Not.Contain("수령해 주세요"),"Disabled or unavailable rewards must not invite a claim");
            if(missing)Assert.That(record,Does.Contain("불러오지 못했습니다"));
            if(empty)Assert.That(record,Does.Contain("보상 항목이 없습니다"));
            if(missing){Assert.IsNull(state.RunRewardPreview);Assert.That(state.RunResultNotice,Does.Contain("불러오지 못했습니다"));wire.Missing=false;root.GetComponentsInChildren<Button>().Single(x=>x.name=="RefreshRunLifecycle").onClick.Invoke();for(int i=0;i<200&&state.RunResultsLoading;i++)await Task.Delay(1);Assert.IsNotNull(state.RunRewardPreview);}
            else if(empty)Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="DialogBody").text,Does.Contain("보상 항목이 없습니다"));
            else Assert.AreEqual(2,state.RunRewardPreview.Lines.Count);
            Assert.AreEqual(0,wire.Writes);Assert.IsFalse(root.GetComponentsInChildren<Button>().Single(x=>x.name=="ClaimRunReward").interactable);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
