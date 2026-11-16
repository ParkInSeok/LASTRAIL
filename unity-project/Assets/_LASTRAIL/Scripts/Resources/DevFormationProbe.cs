#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Resources
{
    // Explicit DEV QA only; uses the real page callbacks and account owner, never synthetic responses.
    internal static class DevFormationProbe
    {
        [Serializable] sealed class Result
        {
            public string stage, failure, presetId;
            public bool cancelledOpen, independentArt, emptyPlus, placement, swap, saved, activated, reentered, released;
            public int savePosts, activatePosts, retainedTexturesAfterClose;
        }
        public static bool Passed {get; private set;}
        public static IEnumerator Run(LobbyDestinationPages page,AccountScreenRuntime state,string output)
        {
            Passed=false;var result=new Result();var steps=Steps(page,state,output,result);
            while(true){bool next;try{next=steps.MoveNext();}catch(Exception error){result.failure=error.Message;break;}if(!next)break;yield return steps.Current;}
            Passed=result.activated&&result.reentered&&result.released&&result.failure==null;
            File.WriteAllText(Path.Combine(output,"formation-report.json"),JsonUtility.ToJson(result,true));
        }
        static void Check(bool value,string code){if(!value)throw new InvalidOperationException(code);}
        static Button Button(LobbyDestinationPages page,string name)=>page.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        static void Click(LobbyDestinationPages page,string name){var button=Button(page,name);Check(button.interactable,"Disabled_"+name);button.onClick.Invoke();}
        static bool Released(){try{PreparedContent.Load<Texture2D>("FormationPlan/combat-roof-v02");return false;}catch(InvalidOperationException){return true;}}
        static IEnumerator Steps(LobbyDestinationPages page,AccountScreenRuntime state,string output,Result result)
        {
            Check(state.AccountStatus==AccountStateReadStatus.Ready&&state.Account.Operators.Count>=5,"RealOwnedRosterMissing");
            result.stage="cancel-open";page.Open(LobbyPage.Formation);page.Close();
            yield return new WaitForSecondsRealtime(2);
            Check(!page.IsOpen&&Released(),"CancelledPageReopenedOrRetained");result.cancelledOpen=true;
            page.Open(LobbyPage.Formation);var deadline=DateTime.UtcNow.AddSeconds(60);
            while(!page.IsOpen&&DateTime.UtcNow<deadline)yield return null;
            Check(page.IsOpen&&page.CurrentPage==LobbyPage.Formation,"FormationOpenFailed");
            Click(page,"NewPreset");yield return null;
            Check(page.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="EmptyPlus"&&t.gameObject.activeInHierarchy)==10,"TenPreviewPlusesMissing");result.emptyPlus=true;
            var art=page.GetComponentsInChildren<RawImage>();
            Check(art.Count(i=>i.name=="Frame"&&i.texture!=null)==10&&art.Any(i=>i.name=="CombatRoof0"&&i.texture!=null),"IndependentArtMissing");result.independentArt=true;
            var ownedIds=state.Account.Operators.Select(o=>o.Text("operatorId")).ToArray();var ids=new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"};Check(ownedIds.Length==5&&ids.All(ownedIds.Contains),"InitialFiveRosterMismatch");
            for(int i=0;i<5;i++){Click(page,"AssignOwned"+i);Click(page,"FormationSlot"+i);yield return null;}
            Check(state.Formation.Selection.SequenceEqual(ids),"PlacementMismatch");result.placement=true;
            Click(page,"FormationSlot0");Click(page,"FormationSlot1");yield return null;
            var swapped=(string[])ids.Clone();swapped[0]=ids[1];swapped[1]=ids[0];
            Check(state.Formation.Selection.SequenceEqual(swapped),"SwapMismatch");result.swap=true;Click(page,"FormationSlot0");Click(page,"FormationSlot1");yield return null;swapped=ids;Check(state.Formation.Selection.SequenceEqual(ids),"RestoreInitialFiveOrderMismatch");
            var name=page.GetComponentsInChildren<TMP_InputField>().Single(i=>i.name=="PresetName");name.text="DEV 편성 가로 검증";name.onEndEdit.Invoke(name.text);
            result.presetId=state.Formation.PresetId;
            yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"formation-placed.png"));yield return new WaitForSecondsRealtime(.5f);
            result.stage="save-close-reenter";var save=Button(page,"ActivateFormation");Check(save.interactable,"SaveDisabled");save.onClick.Invoke();save.onClick.Invoke();page.Close();
            deadline=DateTime.UtcNow.AddSeconds(60);while((state.Busy||state.Formation.Dirty||!state.Formation.CanActivate)&&DateTime.UtcNow<deadline)yield return null;
            Check(!state.Busy&&state.Pending==null&&state.Formation.CanActivate&&!state.Formation.Dirty,"SaveNotConfirmed");
            Check(!page.IsOpen&&Released(),"LateSaveReopenedPage");result.saved=true;
            page.Open(LobbyPage.Formation);deadline=DateTime.UtcNow.AddSeconds(60);while(!page.IsOpen&&DateTime.UtcNow<deadline)yield return null;
            Check(page.IsOpen&&state.Formation.Selection.SequenceEqual(swapped),"SaveReentryMismatch");
            result.stage="activate";var activate=Button(page,"ActivateFormation");Check(activate.interactable,"ActivateDisabled");activate.onClick.Invoke();activate.onClick.Invoke();
            deadline=DateTime.UtcNow.AddSeconds(60);while(state.Busy&&DateTime.UtcNow<deadline)yield return null;
            Check(!state.Busy&&state.Pending==null&&state.Account.ActivePresetId==result.presetId&&state.LastFormationReceipt?.Kind=="ACTIVATE","ActivationNotConfirmed");result.activated=true;
            yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"formation-active.png"));yield return new WaitForSecondsRealtime(.5f);
            var textures=new[]{PreparedContent.Load<Texture2D>("FormationPlan/combat-roof-v02")};
            page.Close();yield return new WaitForSecondsRealtime(1);Check(Released(),"CloseRetainedFormationHandle");
            result.retainedTexturesAfterClose=textures.Count(t=>t!=null);Check(result.retainedTexturesAfterClose==0,"ReleasedHandleStillRetainsNativeTexture");
            for(int cycle=0;cycle<3;cycle++){
                page.Open(LobbyPage.Formation);deadline=DateTime.UtcNow.AddSeconds(60);while(!page.IsOpen&&DateTime.UtcNow<deadline)yield return null;
                Check(page.IsOpen&&state.Formation.Selection.SequenceEqual(swapped)&&page.GetComponentsInChildren<RawImage>().Count(i=>i.name=="Portrait"&&i.texture!=null)==10,"ReentryLostPortraits");
                page.Close();yield return new WaitForSecondsRealtime(.2f);Check(Released(),"ReentryRetainedHandle");
            }
            result.reentered=true;result.released=true;
            var log=File.ReadAllLines(Path.Combine(output,"http-status.log"));
            result.savePosts=log.Count(line=>line=="POST /v1/formation/presets/"+result.presetId+"/save 200");
            result.activatePosts=log.Count(line=>line=="POST /v1/formation/presets/"+result.presetId+"/activate 200");
            Check(result.savePosts==1&&result.activatePosts==1,"DuplicateOrMissingCommand");result.stage="complete";
        }
    }
}
#endif
