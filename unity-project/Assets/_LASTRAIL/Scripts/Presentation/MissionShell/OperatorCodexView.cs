using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class OperatorCodexView:MonoBehaviour,IOperatorCodexView
    {
        public event Action<int> SelectionRequested,TabRequested;
        public event Action BackRequested;
        public void RequestBack(){if(isActiveAndEnabled)BackRequested?.Invoke();}
        public OperatorCodexViewState CurrentState {get;private set;}
        OperatorCodexController controller;
        TMP_FontAsset font;Sprite frame;
        RawImage room,portrait;
        RectTransform detail,list;
        GameObject skills,information;
        TMP_Text name,portraitNotice,body,activeName,ultimateName,activeDescription,ultimateDescription,footer,returnCaption;
        readonly IndependentMetalSurface[] tabs=new IndependentMetalSurface[3];
        bool listBuilt;
        float viewportHeight=-1;

        public static OperatorCodexView Create(Transform parent,TMP_FontAsset font,Sprite borrowedFrame,Action close)
        {
            var root=AuthFormVisuals.Rect("OperatorCodex",parent);var view=root.gameObject.AddComponent<OperatorCodexView>();view.font=font;view.frame=borrowedFrame;view.Build();
            view.controller=new OperatorCodexController(OperatorCodexCatalog.Load(),view,close);return view;
        }
        public void Unbind(){controller?.Dispose();}
        void OnDisable(){Unbind();}
        void OnDestroy(){Unbind();}
        void Build()
        {
            room=Art("CodexRoom",transform,0,0,941,1672);room.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatArchive/room");
            Plate("CodexHeader",transform,0,0,412,98,.9f);Text("CodexTitle",transform,"대원 도감",143,12,254,73,64).alignment=TextAlignmentOptions.Center;
            var headerBack=Control("BackOperatorCodex",transform,"",18,8,105,80,RequestBack);Chevron("HeaderBackArrow",headerBack.transform,37,19,31,43);
            Plate("CodexBrandPlate",transform,654,8,273,103,.9f);Text("CodexBrand",transform,"LAST RAIL",671,29,239,60,36).alignment=TextAlignmentOptions.Center;
            detail=Rect("CodexDetail",transform,0,0,941,1672);
            var viewport=Rect("PortraitViewport",detail,27,141,887,510);viewport.gameObject.AddComponent<RectMask2D>();
            portrait=Art("CodexPortrait",viewport,24,0,840,637);
            portraitNotice=Text("PortraitUnavailable",viewport,"초상 연결을 확인하고 있습니다.",60,120,770,180,28);portraitNotice.alignment=TextAlignmentOptions.Center;
            name=Text("OperatorName",detail,"",50,598,500,57,36);
            Plate("CodexTabBar",detail,0,665,941,126,.9f);
            string[] labels={"기본 정보","스킬","이야기"};var tabRects=new[]{new Rect(20,683,301,86),new Rect(331,681,284,89),new Rect(621,683,300,86)};
            for(int i=0;i<3;i++){int index=i;var bounds=tabRects[i];var tab=Control("CodexTab"+i,detail,labels[i],bounds.x,bounds.y,bounds.width,bounds.height,()=>TabRequested?.Invoke(index));tab.transform.Find("Frame").gameObject.SetActive(false);tab.GetComponent<Image>().color=Color.clear;
                var skin=IndependentMetalSurface.Create((RectTransform)tab.transform,true,Color.white);skin.Frame.BorderScale=.55f;skin.Frame.color=new Color(.46f,.49f,.51f);skin.Backplate.Inset=new Vector2(26,15);foreach(RectTransform light in skin.Lights){light.anchoredPosition*=.55f;light.sizeDelta*=.55f;}tabs[i]=skin;tab.targetGraphic=skin.Backplate;
                var label=tab.GetComponentInChildren<TMP_Text>();label.rectTransform.anchoredPosition=new Vector2(84,-3);label.rectTransform.sizeDelta=new Vector2(bounds.width-106,bounds.height-6);
                var glyph=Rect("CodexTabIcon"+i,tab.transform,35,18,42,50).gameObject.AddComponent<CodexTabGlyph>();glyph.Index=i;glyph.color=new Color(.9f,.87f,.78f);glyph.raycastTarget=false;
            }
            var skillRoot=Rect("CodexSkills",detail,0,0,941,1672);skills=skillRoot.gameObject;
            MaterialPlate("ActiveSkillPlate",skillRoot,27,792,887,280);MaterialPlate("UltimateSkillPlate",skillRoot,27,1102,887,280);
            Symbol("ActiveShieldSymbol",skillRoot,242,844,220,200,true);Symbol("UltimateShieldSymbol",skillRoot,242,1154,220,200,false);
            Plate("ActiveTagPlate",skillRoot,580,813,170,54);Plate("UltimateTagPlate",skillRoot,580,1123,170,54);
            Text("ActiveTag",skillRoot,"액티브",585,815,160,50,28).alignment=TextAlignmentOptions.Center;activeName=Text("ActiveSkillName",skillRoot,"",580,881,310,64,43);activeDescription=Text("ActiveSkillDescription",skillRoot,"",580,967,310,74,29);
            Text("UltimateTag",skillRoot,"궁극기",585,1125,160,50,28).alignment=TextAlignmentOptions.Center;ultimateName=Text("UltimateSkillName",skillRoot,"",580,1191,310,64,43);ultimateDescription=Text("UltimateSkillDescription",skillRoot,"",580,1277,310,74,29);
            foreach(var y in new[]{954f,1264f}){var line=Rect("SkillDivider"+(int)y,skillRoot,580,y,310,2).gameObject.AddComponent<Image>();line.color=new Color(.46f,.49f,.48f);line.raycastTarget=false;}
            activeName.fontSizeMin=26;ultimateName.fontSizeMin=26;
            var info=Rect("CodexInformation",detail,27,792,887,590);information=info.gameObject;Plate("InformationPlate",info,0,0,887,590);body=Text("CodexInformationText",info,"",44,45,799,480,35);body.alignment=TextAlignmentOptions.TopLeft;
            list=Rect("CodexList",transform,27,170,887,1290);
            footer=Text("CodexFooter",transform,"정확한 효과와 수치는 현재 스킬 정보에서 확인합니다.",38,1430,865,83,26);
            var back=Control("CodexListReturn",transform,"목록으로",170,1520,601,130,RequestBack);returnCaption=back.GetComponentInChildren<TMP_Text>();returnCaption.fontSize=43;returnCaption.fontSizeMin=34;returnCaption.fontSizeMax=43;
            back.transform.Find("Frame").gameObject.SetActive(false);back.GetComponent<Image>().color=Color.clear;
            var returnSurface=IndependentMetalSurface.Create((RectTransform)back.transform,true,new Color(.13f,.68f,.87f));returnSurface.EmphasizePrimary(1.25f);returnSurface.Frame.color=new Color(.66f,.69f,.70f);back.targetGraphic=returnSurface.Backplate;
            Chevron("ReturnArrow",back.transform,163,45,24,40);
        }
        public void Render(OperatorCodexViewState state)
        {
            CurrentState=state;
            if(!listBuilt)
            {
                listBuilt=true;for(int i=0;i<state.Entries.Count;i++){int index=i;Control("CodexOperator"+i,list,state.Entries[i].Name,0,i*82,887,74,()=>SelectionRequested?.Invoke(index));}
                if(state.Entries.Count==0)Text("CatalogUnavailable",list,"대원 정보를 불러오지 못했습니다.",35,100,817,200,32);
            }
            bool showList=state.IsList||state.Selected==null;detail.gameObject.SetActive(!showList);list.gameObject.SetActive(showList);returnCaption.text=showList?"로비로":"목록으로";
            footer.text=state.Tab==OperatorCodexTab.Skills&&!showList?"정확한 효과와 수치는 현재 스킬 정보에서 확인합니다.":"확인된 정보만 표시됩니다.";
            if(state.Selected==null)return;
            name.text=state.Selected.Name;
            // Screen-specific display role; never replace the global identity catalog.
            // True-alpha baseline upper-body UV crop remains a visual candidate.
            portrait.texture=state.Selected.Id=="op_kyle"?LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/kyle-alpha-baseline-v05"):null;
            portrait.uvRect=new Rect(630f/1536,(1024-10-318.5f)/1024,420f/1536,318.5f/1024);
            portrait.enabled=portrait.texture!=null;portraitNotice.gameObject.SetActive(portrait.texture==null);
            for(int i=0;i<tabs.Length;i++){bool selected=(int)state.Tab==i;if(selected)tabs[i].EmphasizePrimary(1.25f);else{tabs[i].Backplate.material=null;tabs[i].Backplate.color=new Color(.55f,.57f,.58f);}tabs[i].Lights.gameObject.SetActive(selected);}
            skills.SetActive(state.Tab==OperatorCodexTab.Skills);information.SetActive(state.Tab!=OperatorCodexTab.Skills);
            activeName.text=state.Selected.Active;ultimateName.text=state.Selected.Ultimate;activeDescription.text=state.Selected.ActiveDescription;ultimateDescription.text=state.Selected.UltimateDescription;body.text=state.Body;
            foreach(var symbol in skills.GetComponentsInChildren<CodexShieldSymbol>(true))symbol.gameObject.SetActive(state.Selected.Id=="op_kyle");
        }
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout(), detailLandscape=new LandscapeSectionLayout(), skillLandscape=new LandscapeSectionLayout(), listLandscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,(name,r)=>
            {
                if(name=="CodexRoom")return new Rect(0,0,1600,900);
                if(name=="CodexBrandPlate")return new Rect(1300,8,273,103);
                if(name=="CodexBrand")return new Rect(1317,29,239,60);
                if(name=="CodexDetail")return new Rect(0,0,1600,900);
                if(name=="CodexList")return new Rect(35,145,1530,610);
                if(name=="CodexFooter")return new Rect(35,790,865,83);
                if(name=="CodexListReturn")return new Rect(1050,790,500,90);
                return r;
            });
            detail.localScale=list.localScale=Vector3.one;
            detailLandscape.Apply(detail,(name,r)=>
            {
                if(name=="PortraitViewport")return new Rect(30,150,710,408);
                if(name=="OperatorName")return new Rect(50,600,500,57);
                if(name=="CodexSkills")return new Rect(0,0,1600,900);
                if(name=="CodexInformation")return new Rect(800,255,710,472);
                return LandscapeSectionLayout.Section(r,665,780,125,.8f);
            });
            ((RectTransform)skills.transform).localScale=Vector3.one;
            skillLandscape.Apply(skills.transform,(name,r)=>LandscapeSectionLayout.Section(r,792,780,255,.8f));
            listLandscape.Apply(list,(name,r)=>
            {
                if(name.StartsWith("CodexOperator")&&int.TryParse(name.Substring(13),out var i))return new Rect(i%2*770,i/2*82,740,74);
                return r;
            });
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=AuthFormVisuals.Rect(name,parent);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        RawImage Art(string name,Transform parent,float x,float y,float w,float h){var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        void Border(Transform parent,float pixelsPerUnit=1){var b=AuthFormVisuals.Rect("Frame",parent).gameObject.AddComponent<Image>();b.sprite=frame;b.type=Image.Type.Sliced;b.fillCenter=false;b.pixelsPerUnitMultiplier=pixelsPerUnit;b.color=new Color(1,.79f,.51f);b.raycastTarget=false;}
        void Plate(string name,Transform parent,float x,float y,float w,float h,float pixelsPerUnit=1){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.045f,.052f,.057f,.97f);image.raycastTarget=false;Border(r,pixelsPerUnit);}
        void MaterialPlate(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/popup-backplate")??throw new InvalidOperationException("Codex skill plate material missing");image.uvRect=new Rect(.2f,.2f,.6f,.6f);image.color=new Color(.60f,.66f,.70f,1);image.raycastTarget=false;Border(r,.9f);}
        void Chevron(string name,Transform parent,float x,float y,float width,float height){var glyph=Rect(name,parent,x,y,width,height).gameObject.AddComponent<CodexBackChevron>();glyph.color=new Color(.96f,.93f,.84f);glyph.raycastTarget=false;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.8f;t.fontSizeMax=size;t.text=value;t.richText=false;t.raycastTarget=false;t.color=new Color(.96f,.93f,.84f);t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.055f,.063f,.067f,.98f);Border(r);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(isActiveAndEnabled&&b.interactable)action?.Invoke();});Text("Label",r,label,6,3,w-12,h-6,31).alignment=TextAlignmentOptions.Center;return b;}
        void Symbol(string name,Transform parent,float x,float y,float w,float h,bool rays){var symbol=Rect(name,parent,x,y,w,h).gameObject.AddComponent<CodexShieldSymbol>();symbol.Configure(LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01")??throw new InvalidOperationException("Codex shield material missing"),rays);}
    }
}

