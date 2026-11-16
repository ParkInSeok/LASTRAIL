using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastRail.Tests.EditMode
{
    public sealed class FormationActivationFlowTests
    {
        static readonly string[] Ids={"op_reina","op_kyle","op_eve","op_sera","op_nyx"};
        sealed class Journal:IAccountActionJournal
        {public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction action)=>true;public bool Remove(PendingAccountAction action)=>true;}
        sealed class Wire:IRunApiHttpTransport
        {
            public int Reads,Saves,Activations;
            public TaskCompletionSource<bool> SaveGate=new TaskCompletionSource<bool>();
            public bool Fail;
            JArray operators;
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method=="GET"){
                    Reads++;var json=JObject.Parse(FormationContractTests.S03Json());var original=(JObject)json["operators"][0];
                    json["operators"]=new JArray(Ids.Select(id=>{var row=(JObject)original.DeepClone();row["operatorId"]=id;return row;}));json["trainingCoreSlots"]=new JArray();
                    return RunApiHttpResponse.Success(200,json.ToString());
                }
                var input=JObject.Parse(request.BodyJson);bool save=request.EndpointPath.EndsWith("/save");var preset=request.EndpointPath.Split('/')[4];
                if(save){Saves++;operators=(JArray)input["operators"];await SaveGate.Task;if(Fail)return RunApiHttpResponse.Success(503,"{}");}else Activations++;
                var patch=new JObject{["kind"]="PATCH",["beforeMutationSequence"]=save?0:1,["mutationSequence"]=save?1:2,["coverage"]=new JArray(save?"FORMATION_PRESETS":"ACTIVE_FORMATION")};
                if(save)patch["preset"]=new JObject{["id"]=preset,["name"]=(string)input["name"],["revision"]=1,["operators"]=operators.DeepClone(),["definitionJson"]=new JObject{["schemaVersion"]=1,["operators"]=operators.DeepClone()}.ToString(),["updatedAt"]="2026-11-13T00:00:00.000Z"};
                else patch["formation"]=new JObject{["activePresetId"]=preset,["activePresetRevision"]=1,["revision"]=1,["operators"]=operators.DeepClone()};
                var reply=new JObject{["accepted"]=true,["receiptId"]=save?"saved":"activated",["kind"]=save?"SAVE":"ACTIVATE",["presetId"]=preset,["presetRevision"]=1,["refreshRequired"]=false,["contractVersion"]=FormationWrite.CompleteVersion,["owners"]=new JObject{["account"]=patch},["invalidates"]=save?new JArray():new JArray("ULTIMATE_SELECTION","DEPARTURE_PREVIEW")};
                if(!save){reply["formationRevision"]=1;reply["formationChanged"]=true;reply["operators"]=operators.DeepClone();}
                return RunApiHttpResponse.Success(200,reply.ToString());
            }
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)]
        public async Task SingleActivationSavesOnceAndStopsAfterLeavingOrUnknownSave(bool leave,bool fail)
        {
            var wire=new Wire{Fail=fail};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
            await state.EnsureAccountAsync();for(int i=0;i<5;i++)Assert.IsTrue(state.Formation.Select(i,Ids[i]));
            var root=new GameObject("Activation",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(state);pages.Open(LobbyPage.Formation);
            Action refresh=()=>pages.RefreshAccountView(state);state.Changed+=refresh;
            try{
                var button=root.GetComponentsInChildren<Button>().Single(b=>b.name=="ActivateFormation");
                var plan=root.GetComponentInChildren<FormationPlanView>();
                var roster=root.GetComponentsInChildren<Button>().Single(b=>b.name=="AssignOwned0");
                pages.RefreshAccountView(state);
                Assert.AreSame(plan,root.GetComponentInChildren<FormationPlanView>(),"Data refresh retains the formation view");
                Assert.AreSame(roster,root.GetComponentsInChildren<Button>().Single(b=>b.name=="AssignOwned0"));
                Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.name=="SaveFormation"||b.name=="OpenUltimateSelection"));
                button.onClick.Invoke();button.onClick.Invoke();Assert.AreEqual(1,wire.Saves);
                if(leave){pages.Close();pages.Open(LobbyPage.Formation);}
                wire.SaveGate.SetResult(true);
                for(int i=0;i<30&&(state.Busy||!state.Formation.CanCommit&&state.Pending==null);i++)await Task.Yield();
                Assert.AreEqual(1,wire.Saves);Assert.AreEqual(leave||fail?0:1,wire.Activations,state.ActionNotice);if(!fail)Assert.AreEqual(1,wire.Reads,"Success response must finish the refresh");
                if(fail)Assert.NotNull(state.Pending);else Assert.AreEqual(1,state.Account.Presets.Count);
                if(!leave){Assert.AreSame(button,root.GetComponentsInChildren<Button>().Single(b=>b.name=="ActivateFormation"));Assert.AreSame(plan,root.GetComponentInChildren<FormationPlanView>());}
                if(!leave&&!fail){Assert.AreEqual(state.Formation.PresetId,state.Account.ActivePresetId);Capture(root,"formation-activated");}
                if(!fail){var saved=root.GetComponentsInChildren<Button>().Single(b=>b.name=="SavedPreset0");saved.onClick.Invoke();CollectionAssert.AreEqual(Ids,state.Formation.Selection);}
                pages.Close();Assert.IsFalse(pages.IsOpen);
                if(!leave&&!fail){
                    var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                    var deck=LobbyCommandDeck.Attach(root,font,"UNITY QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(state);Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Capture(root,"lobby-active-five");
                    var portraits=root.GetComponentsInChildren<RawImage>().Where(p=>p.name.StartsWith("SquadPortrait")).ToArray();Assert.AreEqual(5,portraits.Length);foreach(var portrait in portraits)Assert.NotNull(portrait.texture);
                    deck.OpenPage(LobbyPage.Formation);Assert.IsFalse(portraits[0].gameObject.activeInHierarchy);deck.Pages.Close();Assert.IsTrue(portraits[0].gameObject.activeInHierarchy);
                }
            }
            finally{state.Changed-=refresh;UnityEngine.Object.DestroyImmediate(root);}
        }
        static void Capture(GameObject root,string name)
        {
            var canvas=root.GetComponent<Canvas>();if(canvas==null)canvas=root.AddComponent<Canvas>();var cameraObject=new GameObject("CaptureCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            var target=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{
                canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;camera.orthographic=true;camera.orthographicSize=450;camera.aspect=1600f/900;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.targetTexture=target;
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                var folder=System.IO.Path.GetFullPath("../QA/ResourceIntake-20261110/connection/formation-lobby-runtime-v02");System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
    }
}
