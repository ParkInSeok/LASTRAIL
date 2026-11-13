using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.AugmentSelection
{
    // Display and input only. The parent View owns the candidate binding.
    internal sealed class AugmentSelectionDetailView:MonoBehaviour
    {
        TMP_Text title,body;
        ScrollRect scroll;
        Button examplesButton,closeButton,selectButton;
        AugmentModalInputScope inputScope;
        string description,examples;
        bool examplesExpanded;
        bool readOnly;
        Action close,select;
        public static AugmentSelectionDetailView Create(Transform parent,TMP_FontAsset font,Action close,Action select)
        {
            var root=AuthFormVisuals.Rect("AugmentDetail",parent);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=Vector2.zero;root.offsetMax=Vector2.zero;
            root.gameObject.AddComponent<Image>().color=new Color(.06f,.09f,.13f,1);
            var view=root.gameObject.AddComponent<AugmentSelectionDetailView>();view.close=close;view.select=select;view.readOnly=select==null;
            view.title=AuthFormVisuals.Text(root,"DetailTitle","",font,32);view.title.rectTransform.anchorMin=new Vector2(0,1);view.title.rectTransform.anchorMax=Vector2.one;view.title.rectTransform.offsetMin=new Vector2(24,-76);view.title.rectTransform.offsetMax=new Vector2(-200,-12);
            Button Button(string name,string label,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,Action action)
            {
                var r=AuthFormVisuals.Rect(name,root);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;
                var image=r.gameObject.AddComponent<Image>();image.color=new Color(.13f,.18f,.23f,1);var b=r.gameObject.AddComponent<RevisionBoundButton>();b.targetGraphic=image;
                var t=AuthFormVisuals.Text(r,name+"Label",label,font,28,false,TextAlignmentOptions.Center);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=Vector2.zero;t.rectTransform.offsetMax=Vector2.zero;
                b.onClick.AddListener(()=>{if(view.isActiveAndEnabled&&b.IsInteractable())action();});return b;
            }
            view.closeButton=Button("DetailClose","닫기",Vector2.one,Vector2.one,new Vector2(-180,-76),new Vector2(-24,-12),()=>view.close?.Invoke());
            if(!view.readOnly)view.selectButton=Button("DetailSelect","선택",Vector2.zero,new Vector2(1,0),new Vector2(24,16),new Vector2(-24,80),()=>view.select?.Invoke());
            view.examplesButton=Button("DetailExamples","수치 예시 보기",Vector2.zero,new Vector2(1,0),new Vector2(24,view.readOnly?16:88),new Vector2(-24,view.readOnly?72:144),()=>view.ToggleExamples());
            var viewport=AuthFormVisuals.Rect("DetailViewport",root);viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(24,96);viewport.offsetMax=new Vector2(-24,-92);
            viewport.gameObject.AddComponent<Image>().color=new Color(.08f,.12f,.17f,1);viewport.gameObject.AddComponent<RectMask2D>();
            view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.horizontal=false;view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.viewport=viewport;
            view.body=AuthFormVisuals.Text(viewport,"DetailBody","",font,27,false,TextAlignmentOptions.TopLeft);
            var content=view.body.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;content.anchoredPosition=Vector2.zero;
            var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;view.scroll.content=content;
            view.Close();return view;
        }
        public void Show(string name,string description,string examples=null){this.description=description;this.examples=examples;examplesExpanded=false;title.text=name;body.text=description;examplesButton.gameObject.SetActive(!string.IsNullOrEmpty(examples));examplesButton.GetComponentInChildren<TMP_Text>().text="수치 예시 보기";scroll.viewport.offsetMin=new Vector2(24,(string.IsNullOrEmpty(examples)?96:160)-(readOnly?72:0));gameObject.SetActive(true);inputScope?.Dispose();inputScope=new AugmentModalInputScope(transform,transform.parent,closeButton.gameObject);Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=1;}
        void ToggleExamples(){if(string.IsNullOrEmpty(examples))return;examplesExpanded=!examplesExpanded;body.text=description+(examplesExpanded?"\n\n"+examples:"");examplesButton.GetComponentInChildren<TMP_Text>().text=examplesExpanded?"수치 예시 접기":"수치 예시 보기";Canvas.ForceUpdateCanvases();}
        public void SetSelectionAvailable(bool available){if(selectButton)selectButton.interactable=available;}
        public void RefreshCandidate(string name,string description,string examples){
            // Keep the existing modal/focus ownership; replace only its content.
            this.description="후보가 변경되었습니다.\n\n"+description;this.examples=examples;examplesExpanded=false;title.text=name;body.text=this.description;
            examplesButton.gameObject.SetActive(!string.IsNullOrEmpty(examples));examplesButton.GetComponentInChildren<TMP_Text>().text="수치 예시 보기";
            scroll.viewport.offsetMin=new Vector2(24,(string.IsNullOrEmpty(examples)?96:160)-(readOnly?72:0));Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=1;
        }
        public void Close(){inputScope?.Dispose();inputScope=null;scroll?.StopMovement();description=null;examples=null;examplesExpanded=false;if(body!=null)body.text="";if(title!=null)title.text="";gameObject.SetActive(false);}
        void OnDisable(){inputScope?.Dispose();inputScope=null;}
        void OnDestroy(){inputScope?.Dispose();inputScope=null;close=null;select=null;}
    }
}
