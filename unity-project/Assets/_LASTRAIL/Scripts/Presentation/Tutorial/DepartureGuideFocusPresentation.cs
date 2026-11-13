using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Borrows existing preparation rectangles. Owns only child graphics; no account, quote or input commands.
    public sealed class DepartureGuideFocusPresentation
    {
        static readonly string[] Titles={"대원 편성","열차 편성","지원 스킬","출격 비용"};
        readonly RectTransform root,callout,junctionRing,junctionDot;
        readonly RectTransform[] targets,shades,edges,lines;
        readonly TMP_Text title,body,counter,nextLabel,previousLabel;
        readonly Button previous;
        readonly string[] instructions;
        int step;
        public RectTransform CurrentTarget=>targets[step];
        public bool Valid=>root!=null&&targets.All(t=>t!=null&&t.gameObject.activeInHierarchy);

        public DepartureGuideFocusPresentation(RectTransform root,TMP_FontAsset font,Sprite frame,DepartureGuideData data,IReadOnlyList<RectTransform> targets,Action back,Action next,Action close)
        {
            this.root=root;this.targets=targets.ToArray();
            instructions=new[]{data.Operators.Count==0?"대원 편성 확인이 필요합니다.":"함께 출격할 대원 "+data.Operators.Count+"명을 확인해 주세요.",data.Carriages.Count==0?"열차 배치 확인이 필요합니다.":"저장한 객차 "+data.Carriages.Count+"개의 배치를 확인해 주세요.",data.Support.Count==0?"저장한 지원 선택 확인이 필요합니다.":"저장한 지원 "+data.Support.Count+"개의 순서를 확인해 주세요.",data.Cost};
            shades=Enumerable.Range(0,4).Select(i=>Graphic("GuideShade"+i,new Color(0,0,0,.55f))).ToArray();
            edges=Enumerable.Range(0,4).Select(i=>Graphic("GuideFocusEdge"+i,new Color(.12f,.86f,1))).ToArray();
            lines=Enumerable.Range(0,2).Select(i=>Graphic("GuideConnection"+i,new Color(.12f,.86f,1))).ToArray();
            junctionRing=Junction("GuideJunctionRing",.35f);junctionDot=Junction("GuideJunctionDot",0);
            callout=Rect("GuideCallout",root,0,0,560,285);var plate=callout.gameObject.AddComponent<Image>();plate.color=Color.clear;plate.raycastTarget=false;
            var interior=Rect("Interior",callout,10,10,540,265).gameObject.AddComponent<RawImage>();interior.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01")??throw new InvalidOperationException("Guide steel material missing");interior.color=new Color(.18f,.22f,.24f);interior.raycastTarget=false;
            if(frame!=null){var border=Rect("GuideCalloutFrame",callout,0,0,560,285).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.raycastTarget=false;}
            title=Text("GuideTitle","",30,25,500,50,38);body=Text("GuideBody","",35,92,490,67,26);counter=Text("GuideCounter","",233,158,94,36,25);
            foreach(float x in new[]{35f,343f}){var rule=Rect("GuideCounterRule",callout,x,177,182,1).gameObject.AddComponent<Image>();rule.color=new Color(.36f,.44f,.45f);rule.raycastTarget=false;}
            previous=Control("GuidePrevious","이전",30,145,back,false);previousLabel=previous.GetComponentInChildren<TMP_Text>();
            var nextButton=Control("GuideNext","다음",202,156,next,true);nextLabel=nextButton.GetComponentInChildren<TMP_Text>();Control("GuideClose","안내 닫기",385,145,close,false);
            TMP_Text Text(string name,string value,float x,float y,float w,float h,float size){var text=Rect(name,callout,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.richText=false;text.raycastTarget=false;return text;}
            Button Control(string name,string label,float x,float width,Action action,bool primary){var rect=Rect(name,callout,x,211,width,55);var image=rect.gameObject.AddComponent<Image>();image.color=primary?new Color(.04f,.35f,.65f):new Color(.12f,.16f,.17f);if(frame!=null){var border=Rect("ButtonFrame",rect,0,0,width,55).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.raycastTarget=false;}var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());var caption=Text(name+"Label",label,0,0,width,55,28);caption.transform.SetParent(rect,false);return button;}
        }
        RectTransform Graphic(string name,Color color){var rect=Rect(name,root,0,0,1,1);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return rect;}
        RectTransform Junction(string name,float inner){var rect=Rect(name,root,0,0,1,1);var graphic=rect.gameObject.AddComponent<RouletteSurfaceGraphic>();graphic.Radius=.5f;graphic.InnerRadius=inner;graphic.EdgeBrightness=1;graphic.color=new Color(.12f,.86f,1);graphic.raycastTarget=false;return rect;}
        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);Place(rect,x,y,width,height);return rect;}
        static void Place(RectTransform rect,float x,float y,float width,float height){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(Mathf.Max(0,width),Mathf.Max(0,height));}
        public void Render(int value){step=value;title.text=Titles[step];body.text=instructions[step];counter.text=(step+1)+" / 4";previous.interactable=step>0;previousLabel.color=step>0?Color.white:new Color(.55f,.6f,.61f);nextLabel.text=step==3?"안내 마침":"다음";Layout();}
        public void Layout()
        {
            if(!Valid)return;var area=root.rect;float width=area.width,height=area.height;if(width<=0||height<=0)return;
            var corners=new Vector3[4];CurrentTarget.GetWorldCorners(corners);var points=corners.Select(root.InverseTransformPoint).ToArray();
            float left=Mathf.Clamp(points.Min(p=>p.x)-area.xMin,0,width),right=Mathf.Clamp(points.Max(p=>p.x)-area.xMin,0,width);
            float top=Mathf.Clamp(area.yMax-points.Max(p=>p.y),0,height),bottom=Mathf.Clamp(area.yMax-points.Min(p=>p.y),0,height);
            Place(shades[0],0,0,width,top);Place(shades[1],0,bottom,width,height-bottom);Place(shades[2],0,top,left,bottom-top);Place(shades[3],right,top,width-right,bottom-top);
            float scale=Mathf.Min(width/941f,height/1672f),stroke=Mathf.Max(2,3*scale);
            Place(edges[0],left,top,right-left,stroke);Place(edges[1],left,bottom-stroke,right-left,stroke);Place(edges[2],left,top,stroke,bottom-top);Place(edges[3],right-stroke,top,stroke,bottom-top);
            float popupX=Mathf.Max(20*scale,width-640*scale),popupY=Mathf.Max(115*scale,top-445*scale);
            popupY=Mathf.Min(popupY,Mathf.Max(0,height-305*scale));Place(callout,popupX,popupY,560,285);callout.localScale=Vector3.one*scale;
            var start=new Vector2(popupX+60*scale,popupY+285*scale);var end=new Vector2(Mathf.Clamp(left+(right-left)/3,left,right),top);var bend=new Vector2(start.x,Mathf.Max(start.y,end.y-65*scale));
            Line(lines[0],start,bend,stroke);Line(lines[1],bend,end,stroke);
            // These children live in root coordinates; apply the logical-to-root scale exactly once.
            Place(junctionRing,end.x-10*scale,end.y-10*scale,20*scale,20*scale);
            Place(junctionDot,end.x-3*scale,end.y-3*scale,6*scale,6*scale);
        }
        static void Line(RectTransform line,Vector2 start,Vector2 end,float stroke){var delta=end-start;line.anchorMin=line.anchorMax=new Vector2(0,1);line.pivot=new Vector2(.5f,.5f);line.anchoredPosition=new Vector2((start.x+end.x)/2,-(start.y+end.y)/2);line.sizeDelta=new Vector2(delta.magnitude,stroke);line.localEulerAngles=new Vector3(0,0,Mathf.Atan2(-delta.y,delta.x)*Mathf.Rad2Deg);}
    }
}
