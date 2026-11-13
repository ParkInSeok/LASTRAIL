using System;
using System.Collections.Generic;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace LastRail.Presentation.AugmentSelection
{
    // Host places the optional entry in its existing result auxiliary area and
    // binds the original node snapshot. No reward/network/effect execution here.
    public sealed class AugmentNodeResultView:MonoBehaviour
    {
        AugmentNodeResult snapshot;
        Button entry,closeButton;
        AugmentModalInputScope inputScope;
        RectTransform modal,content;
        AugmentSelectionDetailView detail;
        TMP_FontAsset font;
        readonly List<(Button Button,UnityAction Handler)> rowHandlers=new List<(Button,UnityAction)>();
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
        Button Button(string name,Transform parent,string text,Action action,bool transient=false)
        {
            var r=AuthFormVisuals.Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.16f,.20f,1);
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;UnityAction handler=()=>{if(isActiveAndEnabled&&button.IsInteractable())action();};button.onClick.AddListener(handler);if(transient)rowHandlers.Add((button,handler));
            var label=AuthFormVisuals.Text(r,"Label",text,font,26,false,TextAlignmentOptions.Center);Stretch(label.rectTransform);return button;
        }
        public static AugmentNodeResultView Create(Transform parent,TMP_FontAsset font)
        {
            var r=AuthFormVisuals.Rect("AugmentNodeResult",parent);Stretch(r);var v=r.gameObject.AddComponent<AugmentNodeResultView>();v.font=font;
            v.entry=v.Button("AugmentEffectsEntry",r,"증강 효과",()=>v.Open());AuthFormVisuals.Place((RectTransform)v.entry.transform,24,24,240,60);
            v.modal=AuthFormVisuals.Rect("AugmentResultModal",r);Stretch(v.modal);v.modal.gameObject.AddComponent<Image>().color=new Color(.04f,.07f,.1f,1);
            var close=v.closeButton=v.Button("AugmentResultClose",v.modal,"닫기",()=>v.Close());var cr=(RectTransform)close.transform;cr.anchorMin=cr.anchorMax=Vector2.one;cr.offsetMin=new Vector2(-180,-76);cr.offsetMax=new Vector2(-24,-12);
            var title=AuthFormVisuals.Text(v.modal,"Title","이번 전투 증강 효과",font,30);title.rectTransform.anchorMin=new Vector2(0,1);title.rectTransform.anchorMax=Vector2.one;title.rectTransform.offsetMin=new Vector2(24,-76);title.rectTransform.offsetMax=new Vector2(-200,-12);
            var viewport=AuthFormVisuals.Rect("Viewport",v.modal);Stretch(viewport);viewport.offsetMin=new Vector2(24,24);viewport.offsetMax=new Vector2(-24,-92);viewport.gameObject.AddComponent<Image>().color=new Color(.06f,.1f,.14f,1);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            v.content=AuthFormVisuals.Rect("Content",viewport);v.content.anchorMin=new Vector2(0,1);v.content.anchorMax=Vector2.one;v.content.pivot=new Vector2(.5f,1);v.content.sizeDelta=Vector2.zero;
            var layout=v.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=true;layout.childForceExpandHeight=false;v.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=v.content;
            v.detail=AugmentSelectionDetailView.Create(v.modal,font,()=>v.detail.Close(),null);v.Unbind();return v;
        }
        public void Bind(AugmentNodeResult result)
        {
            Unbind();snapshot=result;entry.gameObject.SetActive(snapshot?.Entries.Count>0);
            if(snapshot==null)return;
            for(int i=0;i<snapshot.Entries.Count;i++){
                var row=snapshot.Entries[i];var button=Button("AugmentResultCard"+i,content,"",()=>{if(snapshot!=null)detail.Show(row.Card.Name,row.Details,row.Card.Examples);},true);
                button.GetComponentInChildren<TMP_Text>().text="";var le=button.gameObject.AddComponent<LayoutElement>();le.preferredHeight=180;
                var name=AuthFormVisuals.Text(button.transform,"CardName",row.Card.Name,font,28);AuthFormVisuals.Place(name.rectTransform,16,12,620,46);
                var summary=AuthFormVisuals.Text(button.transform,"Summary",row.Summary,font,25);summary.rectTransform.anchorMin=Vector2.zero;summary.rectTransform.anchorMax=Vector2.one;summary.rectTransform.offsetMin=new Vector2(16,12);summary.rectTransform.offsetMax=new Vector2(-16,-64);
            }
        }
        void Open(){if(snapshot?.Entries.Count>0){modal.gameObject.SetActive(true);inputScope?.Dispose();inputScope=new AugmentModalInputScope(modal,transform.parent,closeButton.gameObject);}}
        void Close(){detail.Close();inputScope?.Dispose();inputScope=null;modal.gameObject.SetActive(false);}
        public void Unbind(){snapshot=null;foreach(var owned in rowHandlers)if(owned.Button!=null)owned.Button.onClick.RemoveListener(owned.Handler);rowHandlers.Clear();if(entry!=null)entry.gameObject.SetActive(false);if(detail!=null)detail.Close();inputScope?.Dispose();inputScope=null;if(modal!=null)modal.gameObject.SetActive(false);if(content!=null)for(int i=content.childCount-1;i>=0;i--){var child=content.GetChild(i);child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}}
        void OnDisable()=>Unbind();
    }
}
