using System;
using System.Collections.Generic;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.AugmentSelection
{
    // Borrowed presentation assets. The catalog adapter owns their lifetime.
    public sealed class AugmentCardPresentation
    {
        public string Name { get; }
        public string Type { get; }
        public string Description { get; }
        public string Details { get; }
        public string Examples { get; }
        public Sprite Art { get; }
        public AugmentCardPresentation(string name,string type,string description,Sprite art)
            :this(name,type,description,art,description){}
        public AugmentCardPresentation(string name,string type,string description,Sprite art,string details)
            :this(name,type,description,art,details,null){}
        public AugmentCardPresentation(string name,string type,string description,Sprite art,string details,string examples)
        {Name=name;Type=type;Description=description;Art=art;Details=details;Examples=examples;}
    }
    // Isolated composition with neutral surfaces. Approved art/grade assets are not supplied.
    // BRouteAugmentScreen installs it with an explicit original grant/owner.
    public sealed class AugmentSelectionView:MonoBehaviour
    {
        sealed class Card
        {
            internal Button Details,Select,Free,Extra;
            internal AugmentCardPresentation Data;
            internal TMP_Text Type,Name,Description,FreeCount,ExtraCount;
            internal Image Art;
        }
        readonly Card[] cards=new Card[3];
        TMP_Text notice;
        Button retryButton;
        Action<int> select;
        Action<int,string> reroll;
        Action retry;
        bool bound;
        bool mutationBlocked;
        AugmentSelectionSnapshot current;
        AugmentSelectionDetailView detail;
        int detailSlot=-1;
        public event Action Unavailable;
        public static AugmentSelectionView Create(Transform parent,TMP_FontAsset font)
        {
            var root=AuthFormVisuals.Rect("AugmentSelectionView",parent);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(900,740);root.anchoredPosition=Vector2.zero;
            var view=root.gameObject.AddComponent<AugmentSelectionView>();view.Build(font);return view;
        }
        public void Bind(Action<int> select,Action<int,string> reroll,Action retry)
        {InvalidateGestures();CloseDetails();this.select=select;this.reroll=reroll;this.retry=retry;bound=true;}
        public void Unbind(){InvalidateGestures();CloseDetails();bound=false;select=null;reroll=null;retry=null;current=null;foreach(var card in cards)if(card!=null){card.Data=null;card.Art.sprite=null;}}
        void InvalidateGestures(){foreach(var button in GetComponentsInChildren<RevisionBoundButton>(true))button.InvalidateGesture();}
        void OnDisable(){Unbind();Unavailable?.Invoke();}
        void OnDestroy(){Unbind();Unavailable?.Invoke();Unavailable=null;}
        bool CanInvoke(Button button)=>bound&&isActiveAndEnabled&&button.gameObject.activeInHierarchy&&button.interactable;
        void CloseDetails(){detailSlot=-1;detail?.Close();}
        void OpenDetails(int slot){if(!CanInvoke(cards[slot].Details))return;detailSlot=slot;detail.Show(cards[slot].Data?.Name??current.Slots[slot].CandidateId,cards[slot].Data?.Details??"상세 정의 미연결",cards[slot].Data?.Examples);detail.SetSelectionAvailable(cards[slot].Select.interactable);}
        void SelectDetail(){int slot=detailSlot;if(slot<0||!CanInvoke(cards[slot].Select))return;CloseDetails();select?.Invoke(slot);}
        // Explicit display input from the grant owner, applied after Render. Never
        // infer empty slots from missing catalog data or turn this into grant completion.
        // The verified sparse-grant adapter remains a separate integration boundary.
        public void ShowUnavailableSlots(IReadOnlyList<int> unavailableSlots)
        {
            if(unavailableSlots==null)throw new ArgumentNullException(nameof(unavailableSlots));
            var seen=new HashSet<int>();
            foreach(int slot in unavailableSlots)if(slot<0||slot>=cards.Length||!seen.Add(slot))throw new ArgumentException("Invalid unavailable slot set");
            if(detailSlot>=0&&seen.Contains(detailSlot))CloseDetails();
            foreach(int slot in seen)
            {
                var card=cards[slot];card.Data=null;card.Art.sprite=null;card.Art.enabled=false;
                card.Type.text="";card.Name.text="선택 가능한 증강 없음";card.Description.text="";
                card.FreeCount.text="—";card.ExtraCount.text="—";
                card.Details.interactable=false;card.Select.interactable=false;card.Free.interactable=false;card.Extra.interactable=false;
            }
        }
        void Build(TMP_FontAsset font)
        {
            TMP_Text Text(Transform parent,string name,string value,float x,float y,float width,float height,float size)
            {
                var text=AuthFormVisuals.Text(parent,name,value,font,size,false,TextAlignmentOptions.Center);
                AuthFormVisuals.Place(text.rectTransform,x,y,width,height);text.raycastTarget=false;return text;
            }
            Button Button(Transform parent,string name,string label,float x,float y,float width,float height)
            {
                var rect=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(rect,x,y,width,height);
                var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.13f,.18f,.23f,.98f);
                var button=rect.gameObject.AddComponent<RevisionBoundButton>();button.targetGraphic=image;
                if(label!=null)Text(rect,name+"Label",label,8,4,width-16,height-8,24);
                return button;
            }
            Text(transform,"Title","증강 선택",0,0,900,64,38);
            for(int i=0;i<3;i++)
            {
                int slot=i;var root=AuthFormVisuals.Rect("Slot"+i,transform);AuthFormVisuals.Place(root,12+i*298,80,280,550);
                var card=cards[i]=new Card();card.Details=Button(root,"Details"+i,null,0,0,280,330);
                card.Type=Text(card.Details.transform,"Type"+i,"표시 데이터 없음",12,10,256,42,23);
                var art=AuthFormVisuals.Rect("Art"+i,card.Details.transform);AuthFormVisuals.Place(art,24,54,232,110);card.Art=art.gameObject.AddComponent<Image>();card.Art.preserveAspect=true;card.Art.raycastTarget=false;
                card.Name=Text(card.Details.transform,"Name"+i,"",16,174,248,60,29);
                card.Description=Text(card.Details.transform,"Description"+i,"",16,242,248,80,22);
                card.Select=Button(root,"Select"+i,"선택",0,342,280,60);
                card.Free=Button(root,"Free"+i,null,0,416,280,60);card.FreeCount=Text(card.Free.transform,"FreeCount"+i,"",8,5,264,50,25);
                card.Extra=Button(root,"Extra"+i,null,0,490,280,60);card.ExtraCount=Text(card.Extra.transform,"ExtraCount"+i,"",8,5,264,50,25);
                card.Select.onClick.AddListener(()=>{if(CanInvoke(card.Select))select?.Invoke(slot);});
                card.Details.onClick.AddListener(()=>OpenDetails(slot));
                card.Free.onClick.AddListener(()=>{if(CanInvoke(card.Free))reroll?.Invoke(slot,"FREE");});
                card.Extra.onClick.AddListener(()=>{if(CanInvoke(card.Extra))reroll?.Invoke(slot,"EXTRA");});
            }
            notice=Text(transform,"Notice","",0,642,660,74,23);
            retryButton=Button(transform,"Retry","결과 재확인",680,650,208,60);retryButton.onClick.AddListener(()=>{if(CanInvoke(retryButton))retry?.Invoke();});
            detail=AugmentSelectionDetailView.Create(transform,font,CloseDetails,SelectDetail);
            Render(null,false,false,"",null);
        }
        public void Render(AugmentSelectionSnapshot snapshot,bool busy,bool pending,string message,Func<string,string,AugmentCardPresentation> presentation,bool extraAvailable=false)
            =>RenderWithCapabilities(snapshot,busy,pending,message,presentation,extraAvailable,true);
        public void RenderWithCapabilities(AugmentSelectionSnapshot snapshot,bool busy,bool pending,string message,Func<string,string,AugmentCardPresentation> presentation,bool extraAvailable,bool rerollsAvailable)
        {
            bool bindingChanged=snapshot==null||current==null||snapshot.RunId!=current.RunId||snapshot.EventId!=current.EventId||snapshot.StageId!=current.StageId||snapshot.CatalogVersion!=current.CatalogVersion;
            bool candidateChanged=detailSlot>=0&&!bindingChanged&&detailSlot<snapshot.Slots.Count&&detailSlot<current.Slots.Count&&snapshot.Slots[detailSlot].CandidateId!=current.Slots[detailSlot].CandidateId;
            if(bindingChanged||snapshot.GrantRevision!=current.GrantRevision||snapshot.Confirmed!=current.Confirmed||mutationBlocked!=(busy||pending))InvalidateGestures();
            mutationBlocked=busy||pending;
            if(detailSlot>=0&&(bindingChanged||snapshot.Confirmed||detailSlot>=snapshot.Slots.Count))CloseDetails();
            current=snapshot;
            for(int i=0;i<3;i++)
            {
                var card=cards[i];var slot=snapshot!=null&&i<snapshot.Slots.Count?snapshot.Slots[i]:null;var data=slot==null?null:presentation?.Invoke(snapshot.CatalogVersion,slot.CandidateId);
                card.Data=data;
                card.Type.text=snapshot?.Confirmed==true&&slot!=null&&slot.CandidateId==snapshot.SelectedCandidateId?"선택 완료":data?.Type??"표시 데이터 없음";card.Name.text=data?.Name??slot?.CandidateId??"확인 필요";
                card.Description.text=data?.Description??"카탈로그·원화 미연결";card.Art.sprite=data?.Art;card.Art.enabled=data?.Art!=null;
                card.FreeCount.text="무료 "+(slot?.FreeRemaining.ToString()??"—")+"/3";
                card.ExtraCount.text="추가 "+(slot?.ExtraRemaining.ToString()??"—")+"/1";
                bool available=snapshot!=null&&slot!=null&&!snapshot.Confirmed&&!busy&&!pending;
                card.Details.interactable=snapshot!=null&&slot!=null&&!snapshot.Confirmed;card.Select.interactable=available;card.Free.interactable=rerollsAvailable&&available&&slot.FreeRemaining>0;
                card.Extra.gameObject.SetActive(extraAvailable&&rerollsAvailable);card.Extra.interactable=rerollsAvailable&&extraAvailable&&available&&slot.FreeRemaining==0&&slot.ExtraRemaining>0;
            }
            if(snapshot!=null)ShowUnavailableSlots(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(snapshot.Slots.Count,3-snapshot.Slots.Count)));
            if(detailSlot>=0){var card=cards[detailSlot];if(candidateChanged)detail.RefreshCandidate(card.Data?.Name??snapshot.Slots[detailSlot].CandidateId,card.Data?.Details??"상세 정의 미연결",card.Data?.Examples);detail.SetSelectionAvailable(card.Select.interactable);}
            AuthFormVisuals.Place(notice.rectTransform,0,extraAvailable?642:568,660,74);
            AuthFormVisuals.Place((RectTransform)retryButton.transform,680,extraAvailable?650:576,208,60);
            ((RectTransform)transform).sizeDelta=new Vector2(900,extraAvailable?740:666);
            notice.text=!string.IsNullOrEmpty(message)?message:snapshot?.Confirmed==true?"선택이 기록되었습니다.":"카드를 터치해 상세 보기 · 선택 버튼으로 확정";
            retryButton.gameObject.SetActive(pending);retryButton.interactable=!busy&&pending;
        }
    }
}
