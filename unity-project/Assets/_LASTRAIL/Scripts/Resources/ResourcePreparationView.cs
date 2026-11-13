using System;
using LastRail.Auth;
using LastRail.Presentation.Startup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Resources
{
    public sealed class ResourcePreparationView : MonoBehaviour
    {
        TMP_Text title, eyebrow, heading, status, amount, percent, network, remaining;
        Image fill;
        RectTransform meter,progressContent;
        Button pause;
        bool hasBytes;
        StartupMotionBackdrop motion;
        long baseline, sourceTotal=-1;
        public void BeginAuthenticatedPlan(long total,long remainingBytes)
        {sourceTotal=total; baseline=Math.Max(0,total-Math.Max(0,remainingBytes));}
        public static ResourcePreparationView Create(Transform parent,TMP_FontAsset font,Action pauseAction)
        {
            var root=AuthFormVisuals.Rect("ResourcePreparation",parent);
            var view=root.gameObject.AddComponent<ResourcePreparationView>();
            root.gameObject.AddComponent<Image>().color=new Color(.03f,.04f,.05f);
            view.motion=StartupMotionBackdrop.Attach(root,true);
            var shade=AuthFormVisuals.Rect("Readability",root).gameObject.AddComponent<EntryBackdropShade>(); shade.raycastTarget=false;
            view.title=AuthFormVisuals.Text(root,"ResourceTitle","LAST RAIL",font,100,true,TextAlignmentOptions.Center);
            view.title.gameObject.AddComponent<LoginTitlePresentation>();
            // Consent hides only progress/input; the borrowed backdrop and title remain visible.
            root=AuthFormVisuals.Rect("ResourceProgressContent",root);view.progressContent=root;
            view.eyebrow=AuthFormVisuals.Text(root,"Eyebrow","PREPARING YOUR JOURNEY",font,26,true);
            view.eyebrow.color=new Color(.5f,.84f,.88f);
            view.heading=AuthFormVisuals.Text(root,"Heading","여정을 준비하고 있습니다",font,58,true);
            view.status=AuthFormVisuals.Text(root,"Status","",font,36);
            view.amount=AuthFormVisuals.Text(root,"Amount","",font,34);
            view.percent=AuthFormVisuals.Text(root,"Percent","",font,36,true,TextAlignmentOptions.MidlineRight); view.percent.color=view.eyebrow.color;
            view.meter=AuthFormVisuals.Rect("ProgressTrack",root);
            view.meter.gameObject.AddComponent<Image>().color=new Color(.17f,.29f,.31f);
            var inner=AuthFormVisuals.Rect("TrackInterior",view.meter); inner.offsetMin=new Vector2(3,3); inner.offsetMax=new Vector2(-3,-3); inner.gameObject.AddComponent<Image>().color=new Color(.02f,.06f,.07f);
            var bar=AuthFormVisuals.Rect("MeasuredProgress",inner); view.fill=bar.gameObject.AddComponent<Image>(); view.fill.color=new Color(.24f,.83f,.92f); bar.anchorMax=new Vector2(0,1);
            view.network=AuthFormVisuals.Text(root,"Network","",font,28);
            view.remaining=AuthFormVisuals.Text(root,"Remaining","",font,28,false,TextAlignmentOptions.MidlineRight);
            view.pause=AuthFormVisuals.MetalButton(root,"PauseDownload","일시 정지",font,false,pauseAction);
            view.Layout(); return view;
        }
        void LateUpdate() => Layout();
        public void SetConsentVisible(bool visible)=>progressContent.gameObject.SetActive(!visible);
        void Layout()
        {
            var area=((RectTransform)transform).rect;
            float width=Mathf.Max(320,area.width*.55f-72), x=area.width-width-48;
            float y=Mathf.Max(28,(area.height-650)/2);
            Place(title.rectTransform,48,area.height*.38f,area.width*.40f,125);
            Place(eyebrow.rectTransform,x,y,width,50);
            Place(heading.rectTransform,x,y+60,width,100);
            heading.enableAutoSizing=true; heading.fontSizeMin=34; heading.fontSizeMax=58;
            Place(status.rectTransform,x,y+175,width,100);
            Place(amount.rectTransform,x,y+290,width*.72f,65); Place(percent.rectTransform,x+width*.72f,y+290,width*.28f,65);
            Place(meter,x,y+370,width,28);
            Place(network.rectTransform,x,y+415,width*.5f,80); Place(remaining.rectTransform,x+width*.5f,y+415,width*.5f,80);
            Place((RectTransform)pause.transform,x,y+530,width,100);
        }
        void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(x+w/2,-y-h/2);
            rect.sizeDelta=new Vector2(w,h);
        }
        public void ShowEntryFailure()
        {status.text="로비에 연결하지 못했습니다. 다시 시도해 주세요."; pause.gameObject.SetActive(false);}

        public void Render(ContentStage stage,long received,long total,DownloadAccessPolicy access)
        {
            if(stage==ContentStage.Initializing || stage==ContentStage.Catalog || stage==ContentStage.Size) baseline=0;
            if(sourceTotal!=total) {sourceTotal=total; baseline=0;}
            received=Math.Max(0,received-baseline); total=Math.Max(0,total-baseline);
            motion.SetResourceLoading(stage!=ContentStage.Ready && stage!=ContentStage.Failed && !access.Paused && access.Network!=ContentNetwork.Offline
                && (stage!=ContentStage.Downloading || access.Allowed));
            hasBytes=total>0; received=Math.Max(0,Math.Min(received,Math.Max(0,total)));
            status.text=stage==ContentStage.Failed?"리소스를 준비하지 못했습니다. 다시 시도해 주세요."
                :stage==ContentStage.Catalog?"리소스 목록 확인 중"
                :stage==ContentStage.Size?"필요한 다운로드 용량 확인 중"
                :stage==ContentStage.Loading?"다운로드 확인 완료 · 리소스 불러오는 중"
                :stage==ContentStage.Ready?"로비 연결 중"
                :access.Paused?"다운로드 일시 정지"
                :access.Network==ContentNetwork.Offline?"네트워크 연결 대기 중"
                :stage==ContentStage.Downloading && !access.Allowed?"다운로드 동의 또는 연결 대기 중"
                :stage==ContentStage.Downloading?"필수 리소스 다운로드 중":"리소스 준비 중";
            var measured=hasBytes && (stage==ContentStage.Downloading || stage==ContentStage.AwaitingAuthentication || stage==ContentStage.Loading || stage==ContentStage.Ready);
            amount.text=measured?(received/1048576d).ToString("0.0")+" / "+(total/1048576d).ToString("0.0")+" MiB":"필요한 리소스를 확인하고 있습니다";
            percent.text=measured?Math.Floor(received*100d/total).ToString("0")+"%":"";
            remaining.text=measured?"남은 용량 "+((total-received)/1048576d).ToString("0.0")+" MiB":"";
            fill.rectTransform.anchorMax=new Vector2(measured?(float)((double)received/total):0,1);
            fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            network.text=access.Network==ContentNetwork.Wifi?"Wi-Fi 연결됨":access.Network==ContentNetwork.Offline?"연결 끊김":access.Network==ContentNetwork.OtherOnline?"모바일 데이터 연결":"네트워크 연결됨";
            pause.gameObject.SetActive(stage==ContentStage.Downloading && (access.Allowed || access.Paused));
            pause.GetComponentInChildren<TMP_Text>().text=access.Paused?"다운로드 재개":"일시 정지";
        }
    }
}
