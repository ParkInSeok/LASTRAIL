using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Presentation snapshot only. No service, journal, save or departure action is accepted.
    public sealed class DepartureGuideData
    {
        public string Operation { get; }
        public IReadOnlyList<string> Operators { get; }
        public IReadOnlyList<string> Carriages { get; }
        public IReadOnlyList<string> Support { get; }
        public string Cost { get; }
        public DepartureGuideData(string operation,IEnumerable<string> operators,IEnumerable<string> carriages,IEnumerable<string> support,string cost)
        {
            Operation=operation;Operators=Array.AsReadOnly(operators.ToArray());Carriages=Array.AsReadOnly(carriages.ToArray());
            Support=Array.AsReadOnly(support.ToArray());Cost=cost;
        }
    }

    [ExecuteAlways]
    public sealed class DepartureGuideView : MonoBehaviour
    {
        static readonly string[] Titles={"편성 확인","열차 배치 확인","지원 선택 확인","출발 비용 확인"};
        static readonly string[] Instructions={"현재 편성한 대원입니다. 대원 수와 구성을 확인해 주세요.","저장한 객차 배치입니다.\n비어 있는 배치가 없는지 확인해 주세요.","저장한 지원 스킬 순서를 확인해 주세요. 선택 변경은 지원 선택 화면에서 할 수 있습니다.","확인된 비용과 보유 연료를 살펴보세요. 안내를 닫으면 출발 준비로 돌아갑니다."};
        readonly List<Image> sectionImages=new List<Image>();
        RectTransform panel,pointer,guidePopup,connector;
        readonly List<RectTransform> targetEdges=new List<RectTransform>();
        TMP_FontAsset font;
        Sprite frame;
        TMP_Text title,body,counter;
        Button previous,next;
        Func<bool> ownerValid;
        DepartureGuideFocusPresentation focused;
        public int Step { get; private set; }
        public RectTransform FocusTarget => focused?.CurrentTarget;
        public static DepartureGuideView OpenFocused(Transform parent,TMP_FontAsset font,Sprite frame,DepartureGuideData data,IReadOnlyList<RectTransform> targets,Func<bool> ownerValid)
        {
            if(targets==null||targets.Count!=4)throw new ArgumentException("Four preparation view targets required",nameof(targets));
            var view=CreateRoot(parent,font,frame,ownerValid);view.GetComponent<Image>().color=Color.clear;
            view.focused=new DepartureGuideFocusPresentation((RectTransform)view.transform,font,frame,data,targets,()=>view.MoveStep(-1),()=>view.MoveStep(1),()=>{if(view.ValidateOwner())view.Close();});
            view.RenderStep();view.ValidateOwner();return view;
        }
        // Standalone summary is retained for historical Editor QA only. Remove after those callers migrate.
        public static DepartureGuideView Open(Transform parent,TMP_FontAsset font,Sprite frame,DepartureGuideData data,Func<bool> ownerValid)
        {
            var view=CreateRoot(parent,font,frame,ownerValid);
            view.Build(data);view.RenderStep();view.ValidateOwner();return view;
        }
        static DepartureGuideView CreateRoot(Transform parent,TMP_FontAsset font,Sprite frame,Func<bool> ownerValid)
        {
            var root=new GameObject("DepartureGuide",typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            root.GetComponent<Image>().color=new Color(0,0,0,.86f);
            var view=root.AddComponent<DepartureGuideView>();view.font=font;view.frame=frame;view.ownerValid=ownerValid;
            return view;
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        Image Plate(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.025f,.044f,.058f);image.raycastTarget=false;
            if(frame!=null){var border=Rect("Frame",r,0,0,w,h).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.raycastTarget=false;}
            return image;
        }
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size)
        {
            var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=value;label.fontSize=size;
            label.enableAutoSizing=true;label.fontSizeMin=size*.7f;label.fontSizeMax=size;label.color=new Color(.9f,.94f,.95f);label.richText=false;label.raycastTarget=false;return label;
        }
        Button Control(string name,string value,float x,float width,Action action)
        {
            var image=Plate(name,guidePopup,x,223,width,65);image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(ValidateOwner())action();});
            Text("Label",image.transform,value,12,8,width-24,49,29).alignment=TextAlignmentOptions.Center;return b;
        }
        void Build(DepartureGuideData data)
        {
            panel=(RectTransform)Plate("GuideOverview",transform,0,0,941,1510).transform;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
            Text("Heading",panel,"출발 준비",52,25,600,65,42);
            counter=Text("GuideCounter",panel,"",735,52,150,50,28);
            var guide=Plate("GuideInstruction",panel,50,110,841,310);guidePopup=guide.rectTransform;
            title=Text("StepTitle",guide.transform,"",24,20,790,48,34);body=Text("StepBody",guide.transform,"",24,80,790,120,29);
            Text("Operation",panel,data.Operation,60,440,820,62,32);
            float[] positions={520,780,1015,1225};float[] heights={240,215,190,180};
            for(int i=0;i<4;i++)sectionImages.Add(Plate("GuideSection"+i,panel,60,positions[i],821,heights[i]));
            var formation=sectionImages[0].transform;
            Text("FormationCount",formation,"현재 편성 · "+data.Operators.Count+"명",22,15,770,43,28);
            if(data.Operators.Count==0)Text("EmptyFormation",formation,"편성한 대원이 없습니다.",22,85,760,75,27);
            float width=Mathf.Min(145,770f/Mathf.Max(1,data.Operators.Count));
            for(int i=0;i<data.Operators.Count;i++){
                var id=data.Operators[i];var texture=OperatorVisualCatalog.Portrait(id);
                var image=Rect("OperatorPortrait"+i,formation,22+i*(width+5),65,width-5,123).gameObject.AddComponent<RawImage>();
                image.texture=texture;image.uvRect=OperatorVisualCatalog.Crop(texture,width-5,123);image.color=texture==null?new Color(.10f,.15f,.18f):Color.white;image.raycastTarget=false;
                Text("OperatorName"+i,formation,OperatorVisualCatalog.KoreanLabel(id),22+i*(width+5),190,width-5,38,22);
            }
            Text("TrainHeading",sectionImages[1].transform,"저장한 객차 배치",22,15,760,40,28);
            Text("Carriages",sectionImages[1].transform,data.Carriages.Count==0?"열차 배치 확인 필요":string.Join("   |   ",data.Carriages),22,78,770,110,27);
            Text("SupportHeading",sectionImages[2].transform,"저장한 지원 선택",22,15,760,40,28);
            Text("SupportSelection",sectionImages[2].transform,data.Support.Count==0?"저장한 지원 선택 확인 필요":string.Join("   →   ",data.Support),22,75,770,94,27);
            Text("CostHeading",sectionImages[3].transform,"출발 비용",22,15,760,40,28);
            Text("Cost",sectionImages[3].transform,data.Cost,22,70,770,92,27);
            pointer=Rect("GuidePointer",panel,26,450,22,30);pointer.gameObject.AddComponent<GuidePointerGraphic>().raycastTarget=false;
            previous=Control("GuidePrevious","이전",24,245,()=>MoveStep(-1));
            next=Control("GuideNext","다음",289,265,()=>MoveStep(1));next.image.color=new Color(.025f,.32f,.58f);
            Control("GuideClose","닫기",574,243,Close);
            connector=Rect("GuideConnector",panel,35,420,3,100);connector.gameObject.AddComponent<Image>().color=new Color(.2f,.9f,1);connector.GetComponent<Image>().raycastTarget=false;
            for(int i=0;i<4;i++){var edge=Rect("TargetEdge"+i,panel,0,0,1,1);var image=edge.gameObject.AddComponent<Image>();image.color=new Color(.2f,.9f,1);image.raycastTarget=false;targetEdges.Add(edge);}Fit();
        }
        void RenderStep()
        {
            if(focused!=null){focused.Render(Step);return;}
            title.text=Titles[Step];body.text=Instructions[Step];counter.text=(Step+1)+" / 4";
            for(int i=0;i<sectionImages.Count;i++)sectionImages[i].color=i==Step?new Color(.035f,.18f,.23f):new Color(.025f,.044f,.058f);
            pointer.anchoredPosition=new Vector2(26,sectionImages[Step].rectTransform.anchoredPosition.y-25);
            var target=sectionImages[Step].rectTransform;float y=-target.anchoredPosition.y,w=target.sizeDelta.x,h=target.sizeDelta.y;
            for(int i=0;i<4;i++){var edge=targetEdges[i];edge.anchoredPosition=new Vector2(i==3?60+w-2:60,-(i==1?y+h-2:y));edge.sizeDelta=i<2?new Vector2(w,2):new Vector2(2,h);}
            connector.sizeDelta=new Vector2(3,y-420+25);
            previous.interactable=Step>0;previous.image.color=Step>0?new Color(.025f,.044f,.058f):new Color(.16f,.17f,.18f);
            previous.GetComponentInChildren<TMP_Text>().color=Step>0?Color.white:new Color(.55f,.58f,.61f);
            next.GetComponentInChildren<TMP_Text>().text=Step==3?"안내 마침":"다음";
        }
        void MoveStep(int direction){if(!ValidateOwner())return;int nextStep=Step+direction;if(nextStep>3){Close();return;}Step=Math.Max(0,nextStep);RenderStep();}
        public bool ValidateOwner(){if(ownerValid==null||!ownerValid()||(focused!=null&&!focused.Valid)){Close();return false;}return true;}
        void LateUpdate(){if((panel!=null||focused!=null)&&ValidateOwner()){if(focused!=null)focused.Layout();else Fit();}}
        void Fit(){var area=((RectTransform)transform).rect;panel.localScale=Vector3.one*Mathf.Min(area.width/941f,area.height/1510f);}
        public void Close(){gameObject.SetActive(false);if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
        void OnDisable(){ownerValid=null;}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GuidePointerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var c=new Color(.2f,.9f,1);
            vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.center.y),c,Vector2.zero);vh.AddTriangle(0,1,2);
        }
    }
}
