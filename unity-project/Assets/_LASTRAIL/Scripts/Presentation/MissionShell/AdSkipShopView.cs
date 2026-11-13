using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Phase 1 has no Store adapter. Only server entitlement recheck is an executable command.
    public sealed class AdSkipShopView : MonoBehaviour
    {
        TMP_Text status,purchaseLabel,title;
        Button refreshButton,purchaseButton,restoreButton;
        bool newSalesAllowed=true;
        Action refresh;
        bool bound;
        public static AdSkipShopView Create(Transform parent,TMP_FontAsset font)
        {
            var root=AuthFormVisuals.Rect("AdSkipShopView",parent);AuthFormVisuals.Place(root,34,550,873,670);
            var view=root.gameObject.AddComponent<AdSkipShopView>();view.Build(font);return view;
        }
        public void Bind(Action refresh){this.refresh=refresh;bound=true;}
        public void Unbind(){bound=false;refresh=null;}
        void OnDisable()=>Unbind();
        void OnDestroy()=>Unbind();
        void Build(TMP_FontAsset font)
        {
            IndependentMetalSurface.Create((RectTransform)transform,false,Color.white);
            TMP_Text Text(string name,string value,float y,float height,float size)
            {var text=AuthFormVisuals.Text(transform,name,value,font,size,false,TextAlignmentOptions.Left);AuthFormVisuals.Place(text.rectTransform,92,y,689,height);return text;}
            title=Text("AdSkipName","광고 스킵 이용권",76,60,40);
            Text("AdSkipTerm","영구 · 1회 구매",142,48,28);
            Text("AdSkipBenefits","운행 보급 · 작전 완료 룰렛\n광고 시청 없이 기존 룰렛 이용",208,94,29);
            Text("AdSkipRerollScope","증강 추가 리롤: 1차 미지원",306,48,26);
            status=Text("AdSkipStatus","보유 여부 확인 필요",364,66,25);
            Button Button(string name,string label,float x,float y,float width,Action action)
            {
                var button=AuthFormVisuals.MetalButton(transform,name,label,font,false,()=>{if(bound&&isActiveAndEnabled)action?.Invoke();});
                AuthFormVisuals.Place((RectTransform)button.transform,x,y,width,78);
                var caption=button.GetComponentInChildren<TMP_Text>();caption.fontSize=28;caption.rectTransform.offsetMin=new Vector2(25,10);caption.rectTransform.offsetMax=new Vector2(-25,-10);
                return button;
            }
            var purchase=purchaseButton=Button("AdSkipPurchase","구매 미설정",48,436,777,null);purchase.interactable=false;purchaseLabel=purchase.GetComponentInChildren<TMP_Text>();
            var restore=restoreButton=Button("AdSkipRestore","구매 복원 미설정",48,542,380,null);restore.interactable=false;
            refreshButton=Button("AdSkipRefresh","보유 여부 다시 확인",445,542,380,()=>{if(refreshButton.interactable)refresh?.Invoke();});
        }
        public void SetNewSalesAllowed(bool allowed){newSalesAllowed=allowed;}
        public void Render(AdSkipSnapshot snapshot,bool loading,string notice)
        {
            title.text=newSalesAllowed?"광고 스킵 이용권":"구매권 관리";
            foreach(string name in new[]{"AdSkipTerm","AdSkipBenefits","AdSkipRerollScope"})transform.Find(name).gameObject.SetActive(newSalesAllowed);
            purchaseButton.gameObject.SetActive(newSalesAllowed);
            ((RectTransform)transform).sizeDelta=new Vector2(873,newSalesAllowed?670:420);
            AuthFormVisuals.Place(status.rectTransform,92,newSalesAllowed?364:152,689,newSalesAllowed?66:96);
            AuthFormVisuals.Place((RectTransform)restoreButton.transform,48,newSalesAllowed?542:270,380,78);
            AuthFormVisuals.Place((RectTransform)refreshButton.transform,445,newSalesAllowed?542:270,380,78);
            purchaseLabel.text=snapshot?.Owned==true?"보유 중":snapshot?.CatalogConfigured==true?"가격 확인 필요 · 구매 연결 미설정":"구매 미설정";
            status.text=loading?"보유 여부 확인 중":!string.IsNullOrEmpty(notice)?notice:snapshot==null?"이용권 확인 필요":snapshot.Owned?"광고 스킵 이용권 보유 · 사용 가능 기능은 서버에서 확인합니다.":snapshot.Status=="REVOKED"?"이용권 상태가 변경되었습니다.":"현재 보유한 이용권이 없습니다.";
            refreshButton.interactable=!loading;
        }
    }
}
