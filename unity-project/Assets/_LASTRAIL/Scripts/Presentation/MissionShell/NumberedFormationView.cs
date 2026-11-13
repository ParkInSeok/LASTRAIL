using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
 // Only display enumeration reverses. Input always carries the original formation slot.
 public sealed class NumberedFormationView:MonoBehaviour
 {
  readonly Button[,] buttons=new Button[3,5];readonly TMP_Text[,] labels=new TMP_Text[3,5];readonly RawImage[,] portraits=new RawImage[3,5];
  Action<int> selected;
  public static Texture2D Portrait(string id)=>id==null?null:LastRail.Resources.PreparedContent.Load<Texture2D>("NumberedFormation/"+id)??OperatorVisualCatalog.Portrait(id);
  public static string Label(string id)=>id=="op_nyx"?"닉스":OperatorVisualCatalog.KoreanLabel(id);
  public static NumberedFormationView Create(Transform parent,TMP_FontAsset font,bool showCards=true)
  {
   var root=new GameObject("NumberedFormation",typeof(RectTransform));root.transform.SetParent(parent,false);var view=root.AddComponent<NumberedFormationView>();view.Build(font,showCards);return view;
  }
  static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
  {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static TMP_Text Text(Transform parent,TMP_FontAsset font,string name,string value,float x,float y,float w,float h,float size)
  {var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.75f;t.fontSizeMax=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.richText=false;return t;}
  void Build(TMP_FontAsset font,bool showCards)
  {
   ((RectTransform)transform).sizeDelta=new Vector2(900,showCards?430:250);float offset=showCards?0:180;
   var background=gameObject.AddComponent<Image>();background.color=new Color(.025f,.045f,.065f,.95f);background.raycastTarget=false;
   if(showCards)Text(transform,font,"PositionHelp","전투 위치 · 요원 선택 후 번호를 눌러 배치/교환",0,0,900,38,25);
   for(int row=0;row<3;row++){
    if(row==0&&!showCards)continue;
    if(row>0)Text(transform,font,"Direction"+row,row==1?"적 ◀  왼쪽 교전 · 후미":"오른쪽 교전 · 선두  ▶ 적",row==1?0:460,192-offset,440,36,23);
    for(int slot=0;slot<5;slot++){
     int boundSlot=slot;float x=row==0?slot*180:((row==1?slot:4-slot)*84+(row==1?6:466));float y=row==0?45:(slot%2==0?286:237)-offset;float w=row==0?168:80,h=row==0?135:120;
     var r=Rect(transform,(row==0?"FormationSlot":row==1?"LeftPosition":"RightPosition")+slot,x,y,w,h);var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.08f,.16f,.21f,.96f);
     var b=r.gameObject.AddComponent<Button>();b.targetGraphic=bg;buttons[row,slot]=b;b.onClick.AddListener(()=>{if(this!=null&&isActiveAndEnabled&&b.interactable)selected?.Invoke(boundSlot);});
     var image=Rect(r,"Portrait",5,5,w-10,h-43).gameObject.AddComponent<RawImage>();image.raycastTarget=false;portraits[row,slot]=image;
     labels[row,slot]=Text(r,font,"PositionLabel","",2,h-39,w-4,37,row==0?24:19);
    }
   }
   Text(transform,font,"PreviewScope","전신 정지원화·번호 미리보기 / 객차번호 아님 / 이동·접지 표현 아님",0,408-offset,900,22,17);
  }
  public void Bind(Action<int> action){selected=action;}
  public void Unbind(){selected=null;}
  void OnDisable(){Unbind();}void OnDestroy(){Unbind();}
  public void Render(IReadOnlyList<string> ids,int activeSlot=-1,bool editable=false)
  {
   if(ids==null||ids.Count!=5)throw new ArgumentException("Five logical positions required",nameof(ids));
   for(int row=0;row<3;row++)for(int slot=0;slot<5;slot++){
    if(buttons[row,slot]==null)continue;
    var id=ids[slot];var image=portraits[row,slot];var texture=Portrait(id);image.texture=texture;image.enabled=texture!=null;image.uvRect=new Rect(0,0,1,1);
    if(texture!=null){var parent=(RectTransform)image.transform.parent;float availableWidth=parent.rect.width-10,availableHeight=parent.rect.height-43,scale=Mathf.Min(availableWidth/texture.width,availableHeight/texture.height);image.rectTransform.sizeDelta=new Vector2(texture.width*scale,texture.height*scale);image.rectTransform.anchoredPosition=new Vector2((parent.rect.width-texture.width*scale)*.5f,-5);}
    labels[row,slot].text=(slot+1)+" "+Label(id);buttons[row,slot].interactable=editable&&selected!=null;buttons[row,slot].targetGraphic.color=slot==activeSlot?new Color(.42f,.31f,.1f):new Color(.08f,.16f,.21f,.96f);
   }
  }
 }
}
