using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Resources;
namespace LastRail.Presentation.MissionShell
{
    // Borrows the page's textures/frame. Commands and account state stay with its controller.
    public sealed class TrainOverviewView : MonoBehaviour
    {
        TMP_FontAsset font; Sprite frame; TMP_Text level,cost,notice; Button upgrade;
        Action strengthen,refresh,manage;
        public static TrainOverviewView Create(Transform parent,TMP_FontAsset font,Sprite frame,Action strengthen,Action refresh,Action manage)
        {
            var root=Rect("TrainOverview",parent,0,0,1600,900);
            var view=root.gameObject.AddComponent<TrainOverviewView>();view.font=font;view.frame=frame;
            view.strengthen=strengthen;view.refresh=refresh;view.manage=manage;view.Build();return view;
        }
        void Build()
        {
            var train=Rect("TrainIllustration",transform,135,202,1055,440).gameObject.AddComponent<RawImage>();
            train.texture=PreparedContent.Load<Texture2D>("TrainFortress/train-overview");train.raycastTarget=false;
            train.rectTransform.sizeDelta=new Vector2(1055,1055f*train.texture.height/train.texture.width);
            var panel=Plate("TrainDetails",transform,1200,100,380,755);
            Text("Title",panel,"요새 기관차",20,20,340,55,35);
            Text("CurrentHeading",panel,"현재 성능",20,85,340,42,27);
            string[] labels={"열차 레벨","최대 HP","기본 공격력","방어력","기본 공격 주기"};
            for(int i=0;i<labels.Length;i++){
                Text("StatLabel"+i,panel,labels[i],24,143+i*40,215,37,23);
                var value=Text("StatValue"+i,panel,"—",242,143+i*40,110,37,24);value.color=new Color(.45f,.9f,.96f);if(i==0)level=value;
                var line=Rect("Divider"+i,panel,24,180+i*40,328,1).gameObject.AddComponent<Image>();line.color=new Color(.55f,.4f,.2f);line.raycastTarget=false;
            }
            Text("PreviewHeading",panel,"강화 미리보기",20,370,340,42,27);
            Text("PreviewUnavailable",panel,"강화 후 능력치 확인 필요",24,425,328,38,22);
            Text("CostHeading",panel,"필요 재료",20,480,340,42,27);
            cost=Text("TrainGrowthCost",panel,"—",24,532,328,72,23);
            upgrade=Button("UpgradeTrain",panel,"기관차 강화",22,628,336,80,()=>strengthen?.Invoke(),true);
            notice=Text("TrainGrowthNotice",transform,"",1210,862,365,30,17);
            Text("ConsistLabel",transform,"기관차 + 객차 4칸",170,690,990,40,29);
            var strip=Plate("TrainModuleStrip",transform,140,746,1040,110);
            string[] names={"후미 전투 객차","설비 3","설비 2","설비 1","요새 기관차"};
            for(int i=0;i<5;i++){
                bool locomotive=i==4;
                var cell=Button("TrainModule"+i,strip,names[i],i*208+3,5,202,100,()=>{if(!locomotive)manage?.Invoke();},locomotive);
                var label=cell.GetComponentInChildren<TMP_Text>();label.fontSize=23;label.rectTransform.anchoredPosition=new Vector2(10,-66);label.rectTransform.sizeDelta=new Vector2(182,29);
                var icon=Rect("ModuleIcon",cell.transform,25,8,152,54).gameObject.AddComponent<RawImage>();icon.texture=train.texture;icon.raycastTarget=false;
                var crops=new[]{new Rect(0,.54f,.18f,.43f),new Rect(.16f,.44f,.15f,.34f),new Rect(.27f,.33f,.15f,.4f),new Rect(.38f,.22f,.16f,.48f),new Rect(.44f,.03f,.55f,.83f)};icon.uvRect=crops[i];float aspect=train.texture.width*crops[i].width/(train.texture.height*crops[i].height);float width=Mathf.Min(152,54*aspect);icon.rectTransform.sizeDelta=new Vector2(width,width/aspect);icon.rectTransform.anchoredPosition=new Vector2((202-width)/2,-8);
            }
            Button("ManageTrainLoadout",transform,"차량 배치",148,110,180,55,()=>manage?.Invoke(),false);
            Button("RefreshTrain",transform,"새로고침",990,110,185,55,()=>refresh?.Invoke(),false);
        }
        public void Render(TrainScreenState state)
        {level.text=state.Level??"—";cost.text=state.Cost??"—";notice.text=state.Available?state.Notice:state.Unavailable;upgrade.interactable=state.CanUpgrade;upgrade.GetComponentInChildren<TMP_Text>().text=state.UpgradeLabel??"기관차 강화";}
        public void Unbind(){strengthen=null;refresh=null;manage=null;}
        void OnDisable()=>Unbind();
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        RectTransform Plate(string name,Transform parent,float x,float y,float w,float h)
        {var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.025f,.03f,.035f,1);image.raycastTarget=false;var edge=Rect("Frame",r,0,0,w,h).gameObject.AddComponent<Image>();edge.sprite=frame;edge.type=Image.Type.Sliced;edge.fillCenter=false;edge.raycastTarget=false;return r;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size)
        {var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.94f,.9f,.78f);t.alignment=TextAlignmentOptions.Midline;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=size*.8f;t.fontSizeMax=size;return t;}
        Button Button(string name,Transform parent,string label,float x,float y,float w,float h,Action action,bool blue)
        {var r=Plate(name,parent,x,y,w,h);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.targetGraphic.raycastTarget=true;if(blue){b.targetGraphic.color=new Color(.02f,.3f,.5f);LobbyArtwork.BlueInset(r).transform.SetAsFirstSibling();}Text("Label",r,label,12,5,w-24,h-10,30).alignment=TextAlignmentOptions.Center;b.onClick.AddListener(()=>{if(this!=null&&isActiveAndEnabled&&b.interactable)action();});return b;}
    }
}
