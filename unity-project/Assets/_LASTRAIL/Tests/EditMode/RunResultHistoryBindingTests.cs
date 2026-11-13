using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class RunResultHistoryBindingTests
    {
        const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
        sealed class Wire:IRunApiHttpTransport
        {
            public bool Victory,Missing,Invalid;public int Writes;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
            {
                if(r.Method!="GET"){Writes++;throw new InvalidOperationException("Read-only history test");}
                if(r.EndpointPath.EndsWith("/lifecycle"))
                {
                    var data=JObject.Parse(File.ReadAllText(Source+"result-lost-finish-restored.json"));
                    if(Victory){data["runStatus"]="COMPLETED";data["finish"]["outcome"]="VICTORY";}
                    // Explicit contract-shaped local fixture, not an assertion about this historical run.
                    if(!Missing)data["startResponse"]["admission"]=new JObject{["version"]="departure-v2",["formation"]=new JObject{["schemaVersion"]=1,["operators"]=new JArray(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}.Select((id,slot)=>new JObject{["slot"]=slot,["operatorId"]=Invalid?"op_kyle":id}).Reverse())}};
                    return Task.FromResult(RunApiHttpResponse.Success(200,data.ToString()));
                }
                if(r.EndpointPath.Contains("/reward-preview/"))return Task.FromResult(RunApiHttpResponse.Success(200,File.ReadAllText(Source+"result-preview.json")));
                return Task.FromResult(RunApiHttpResponse.Success(404,"{}"));
            }
        }
        [TestCase(false)][TestCase(true)] public async Task OriginalStartFormationIsDisplayedInSlotOrderAndClearedWhenMissing(bool victory)
        {
            var wire=new Wire{Victory=victory};var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            var root=new GameObject("HistoryTest",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(owner);deck.OpenRunLifecycle("run-smoke-1");
                string labels=string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));
                foreach(var name in new[]{"카일","레이나","이브","세라","미라"})Assert.That(labels,Does.Contain(name));
                Assert.That(owner.Lifecycle.StartFormation.OperatorIds,Is.EqualTo(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}));
                Assert.That(new RunResultHistoryDisplayState(owner,"another_run").HasFormation,Is.False,"A different run cannot borrow this roster");
                Assert.That(labels,Does.Contain("출발 편성"));Assert.That(labels,Does.Contain("기록 확인 필요"));
                wire.Missing=true;await owner.ReadRunResultsAsync("run-smoke-1");
                labels=string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));Assert.That(labels,Does.Not.Contain("카일"),"Old roster must not survive a missing history read");
                wire.Missing=false;wire.Invalid=true;await owner.ReadRunResultsAsync("run-smoke-1");Assert.That(owner.Lifecycle,Is.Not.Null,"Malformed optional formation must not erase valid reward lifecycle");
                labels=string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));Assert.That(labels,Does.Not.Contain("카일"));Assert.That(wire.Writes,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [TestCase("version",RunStartFormationStatus.Unsupported)]
        [TestCase("schema",RunStartFormationStatus.Unsupported)]
        [TestCase("slot",RunStartFormationStatus.Malformed)]
        [TestCase("id",RunStartFormationStatus.Malformed)]
        public void InvalidOptionalFormationHasExplicitUnavailableStatus(string change,RunStartFormationStatus expected)
        {
            var start=JObject.Parse("{\"admission\":{\"version\":\"departure-v2\",\"formation\":{\"schemaVersion\":1,\"operators\":[{\"slot\":0,\"operatorId\":\"op_kyle\"},{\"slot\":1,\"operatorId\":\"op_reina\"},{\"slot\":2,\"operatorId\":\"op_eve\"},{\"slot\":3,\"operatorId\":\"op_sera\"},{\"slot\":4,\"operatorId\":\"op_mira\"}]}}}");
            var a=start["admission"];var formation=a["formation"];
            if(change=="version")a["version"]="departure-future";
            if(change=="schema")formation["schemaVersion"]=2;
            if(change=="slot")formation["operators"][0]["slot"]=-1;
            if(change=="id")formation["operators"][0]["operatorId"]="invalid id";
            var result=RunStartFormationSnapshot.Read(start);Assert.That(result.Status,Is.EqualTo(expected));Assert.That(result.OperatorIds,Is.Empty);
        }
    }
}
