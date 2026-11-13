using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        RunVictoryView victoryView;
        float resultViewportHeight=-1;
        readonly Dictionary<GameObject,bool> resultOriginalVisibility=new Dictionary<GameObject,bool>();
        void CloseVictoryPresentation(bool restore)
        {
            CloseCombatAdView();
            if(victoryView!=null){victoryView.Unbind();var old=victoryView.gameObject;victoryView=null;old.SetActive(false);if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);}
            if(restore)foreach(var pair in resultOriginalVisibility)if(pair.Key!=null)pair.Key.SetActive(pair.Value);
            resultOriginalVisibility.Clear();
        }
        void BuildResultPresentation()
        {
            resultViewportHeight=-1;
            dialog.sizeDelta=new Vector2(941,1672);
            dialog.parent.GetComponent<Image>().color=new Color(.015f,.018f,.02f,1);
            var background=ResultArt("ResultEnvironment","NoncombatResult/environment",0,0,941,1672,true);background.transform.SetAsFirstSibling();
            var train=ResultArt("ResultTrain","NoncombatResult/train",20,220,900,450,false);train.transform.SetSiblingIndex(1);train.gameObject.SetActive(false);
            var title=dialog.Find("DialogTitle").GetComponent<TMP_Text>();Place(title.rectTransform,60,50,821,110);title.fontSize=64;title.fontSizeMax=64;title.alignment=TextAlignmentOptions.Center;
            Label("ResultOperation",dialog,"",60,165,821,65,29,TextAlignmentOptions.Center);
            var ornaments=Rect("ResultTitleOrnaments",dialog);Place(ornaments,0,42,941,3);
            for(int i=0;i<2;i++){var stroke=Rect("Stroke"+i,ornaments);Place(stroke,350.5f+i*140,0,100,3);var image=stroke.gameObject.AddComponent<Image>();image.raycastTarget=false;}
            Panel("ResultStats",dialog,35,650,871,130);Label("ResultStatsText",dialog,"도달 웨이브\n기록 확인 필요",60,670,385,90,27,TextAlignmentOptions.Center);
            Label("ResultTimeText",dialog,"전투 시간\n기록 확인 필요",496,670,385,90,27,TextAlignmentOptions.Center);ResultDivider("ResultMetricDivider",470,670,2,90);
            Panel("ResultParticipants",dialog,35,800,871,130);Label("ResultParticipantsText",dialog,"참여 대원 · 해당 전투의 편성 기록 확인 필요",60,822,821,85,28,TextAlignmentOptions.Center);
            Panel("ResultRewardPanel",dialog,35,950,871,300);
            foreach(var name in new[]{"ResultStats","ResultParticipants","ResultRewardPanel"})
            {
                var panel=(RectTransform)dialog.Find(name);panel.GetComponent<Image>().color=Color.clear;
                // Borrow the filtered material; the panel owns only its child graphic. Keep its existing frame.
                var interior=Rect("Interior",panel);Stretch(interior);interior.offsetMin=new Vector2(12,12);interior.offsetMax=new Vector2(-12,-12);interior.SetAsFirstSibling();
                var surface=interior.gameObject.AddComponent<RawImage>();surface.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01")??throw new System.InvalidOperationException("Result steel material missing");surface.color=new Color(.18f,.22f,.24f);surface.raycastTarget=false;
            }
            Label("ResultRecordHeading",dialog,"전투 기록",100,964,770,35,25,TextAlignmentOptions.MidlineLeft);
            Label("ResultRecordText",dialog,"",65,1008,811,68,23,TextAlignmentOptions.TopLeft);
            ResultDivider("ResultRecordDivider",65,1083,811,2);
            Label("ResultRewardHeading",dialog,"보상 정보",100,1094,770,35,25,TextAlignmentOptions.MidlineLeft);
            for(int i=0;i<3;i++)ResultDivider("ResultRecordIcon"+i,67,974+i*6,20,2);
            var rewardIcon=Rect("ResultRewardIcon",dialog);Place(rewardIcon,67,1100,20,20);var icon=rewardIcon.gameObject.AddComponent<Image>();icon.color=new Color(.15f,.65f,.78f);icon.raycastTarget=false;
            var body=dialog.Find("DialogBody").GetComponent<TMP_Text>();Place(body.rectTransform,65,1137,811,98);body.fontSize=23;body.fontSizeMax=23;body.transform.SetAsLastSibling();
            Place((RectTransform)dialog.Find("RecoverRunFinish"),35,1280,425,70);Place((RectTransform)dialog.Find("ClaimRunReward"),481,1280,425,70);
            Place((RectTransform)dialog.Find("RefreshRunLifecycle"),35,1370,871,70);
            var formation=Button("ResultFormation",dialog,35,1460,425,85,"편성 점검",()=>OpenPage(LobbyPage.Formation));formation.image.color=new Color(.025f,.28f,.48f);
            Place((RectTransform)dialog.Find("CloseLobbyDialog"),481,1460,425,85);
            Label("ResultReturnNotice",dialog,"재출격은 출발 준비 화면에서 진행합니다.",60,1570,821,50,23,TextAlignmentOptions.Center);
            foreach(var button in dialog.GetComponentsInChildren<Button>()){var caption=button.GetComponentInChildren<TMP_Text>();caption.rectTransform.anchorMin=Vector2.zero;caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.offsetMin=new Vector2(20,10);caption.rectTransform.offsetMax=new Vector2(-20,-10);caption.alignment=TextAlignmentOptions.Center;caption.fontSize=29;caption.fontSizeMax=29;var colors=button.colors;colors.disabledColor=new Color(.55f,.6f,.65f,1);button.colors=colors;}
            UpdateResultPresentation();
        }
        void ResultDivider(string name,float x,float y,float w,float h){var rect=Rect(name,dialog);Place(rect,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.3f,.55f,.61f);image.raycastTarget=false;}
        LandscapeSectionLayout resultLandscape;
        void SetResultViewportHeight(float height)
        {
            if(dialog.Find("ResultEnvironment")==null)return;
            if(resultLandscape==null)resultLandscape=new LandscapeSectionLayout();
            dialog.sizeDelta=new Vector2(1600,900);
            resultLandscape.Apply(dialog,(name,r)=>
            {
                if(name=="ResultEnvironment")return new Rect(0,0,1600,900);
                if(name=="ResultTrain")return new Rect(20,270,720,360);
                if(name=="DialogTitle")return new Rect(35,50,720,110);
                if(name=="ResultOperation")return new Rect(35,165,720,65);
                if(name=="ResultTitleOrnaments")return new Rect(0,42,750,3);
                if(name=="ResultReturnNotice")return new Rect(35,760,720,50);
                return LandscapeSectionLayout.Section(r,650,810,60,.8f);
            });
        }
        void ApplyResultControlVisuals()
        {
            if(victoryView!=null)return;
            foreach(var button in dialog.GetComponentsInChildren<Button>()){
                var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
                button.image.color=!button.interactable?new Color(.12f,.15f,.17f,1):button.name=="ResultFormation"?new Color(.025f,.28f,.48f,1):new Color(.035f,.044f,.049f,1);
                button.GetComponentInChildren<TMP_Text>().color=button.interactable?new Color(.94f,.95f,.96f,1):new Color(.53f,.58f,.62f,1);
            }
        }
        RawImage ResultArt(string name,string resource,float x,float y,float w,float h,bool crop)
        {
            var rect=Rect(name,dialog);Place(rect,x,y,w,h);var image=rect.gameObject.AddComponent<RawImage>();var texture=LastRail.Resources.PreparedContent.Load<Texture2D>(resource);image.texture=texture;image.raycastTarget=false;
            if(crop)image.uvRect=OperatorVisualCatalog.Crop(texture,w,h);return image;
        }
        void UpdateResultPresentation()
        {
            if(dialog==null||dialog.Find("ResultTrain")==null)return;
            var state=accountState;var data=state?.Lifecycle;bool loading=state?.RunResultsLoading==true;bool matching=data?.RunId==lifecycleRun;
            if(victoryView!=null&&!loading&&matching&&data.ConfirmedOutcome!="VICTORY")CloseVictoryPresentation(true);
            if(victoryView!=null||!loading&&matching&&data.ConfirmedOutcome=="VICTORY")
            {
                if(victoryView==null)
                {
                    foreach(Transform child in dialog){resultOriginalVisibility[child.gameObject]=child.gameObject.activeSelf;child.gameObject.SetActive(false);}
                    victoryView=RunVictoryView.Create(dialog,font,frameSkin);
                    victoryView.Bind(ClaimCurrentRunReward,RefreshCurrentRunResults,ReturnFromRunLifecycle,RecoverCurrentRunFinish);
                    victoryView.BindAd(OpenCombatAdView);
                    victoryView.BindOperations(OpenResultOperations);
                    EvaluateLayout();
                }
                victoryView.Render(new RunVictoryDisplayState(state,lifecycleRun,missions));RenderCombatAdView();return;
            }
            bool failed=!loading&&matching&&data.ConfirmedOutcome=="FAILURE";
            var title=dialog.Find("DialogTitle").GetComponent<TMP_Text>();title.text=loading?"결과 확인 중":matching?data.ResultTitle:"결과 확인 필요";title.color=failed?new Color(1,.48f,.4f):new Color(.91f,.9f,.84f);
            foreach(var stroke in dialog.Find("ResultTitleOrnaments").GetComponentsInChildren<Image>())stroke.color=failed?new Color(.65f,.31f,.27f):new Color(.48f,.49f,.46f);
            dialog.Find("ResultTrain").gameObject.SetActive(failed);
            var operation=matching?(string)data.StartResponse?["operationId"]:null;
            var label=missions?.FirstOrDefault(x=>x.MissionId==operation)?.DisplayLabel;
            dialog.Find("ResultOperation").GetComponent<TMP_Text>().text=loading?"전투 기록을 확인하고 있습니다.":operation==null?"기록 확인 필요":(label??"작전 기록")+" · 위험 "+(string)data.StartResponse?["risk"];
            dialog.Find("ResultFormation").GetComponent<Button>().interactable=state?.AccountStatus==AccountStateReadStatus.Ready;
            var history=new RunResultHistoryDisplayState(state,lifecycleRun);
            dialog.Find("ResultParticipantsText").GetComponent<TMP_Text>().text=history.Heading+" · "+(history.HasFormation?string.Join(" · ",history.Names):history.FormationNotice);
        }
    }
}
