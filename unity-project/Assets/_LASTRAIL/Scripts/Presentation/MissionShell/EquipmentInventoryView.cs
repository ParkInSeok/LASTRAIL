using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class EquipmentInventoryView:MonoBehaviour {
  EquipmentInventoryController owner;Action closed,refresh;Button refreshButton;TMP_FontAsset font;ApprovedUiConnections art;
  RectTransform rows,detail;TMP_Text summary;ScrollRect scroll;bool bound;
  sealed class Row { public RectTransform Root;public Button Input;public TMP_Text Label;public string Id; }
  readonly System.Collections.Generic.List<Row> rowViews=new System.Collections.Generic.List<Row>();
  TMP_Text detailLabel;
  EquipmentIconResources iconResources;EquipmentIconResources.Lease icons;bool loadingIcons;long iconGeneration;
  public void BindRefresh(Action action){refresh=action;refreshButton.gameObject.SetActive(action!=null);}
  public void ShowNotice(string text){if(summary!=null)summary.text=text;}
  void ReleaseIcons(){iconGeneration++;loadingIcons=false;foreach(var image in GetComponentsInChildren<RawImage>(true))if(image.name.StartsWith("EquipmentIcon:"))image.texture=null;icons?.Dispose();icons=null;}
  async void EnsureIcons(){if(iconResources==null||icons!=null||loadingIcons||owner?.Snapshot==null||!owner.Snapshot.Items.Any(i=>EquipmentIconResources.TryRegion(i.IconKey,out _)))return;loadingIcons=true;long stamp=iconGeneration;try{var lease=await iconResources.AcquireAsync();if(this==null||stamp!=iconGeneration||!bound||!isActiveAndEnabled){lease.Dispose();return;}icons=lease;ApplyIcons();}catch(Exception e){if(stamp==iconGeneration)Debug.LogWarning("Equipment icon load failed: "+e.GetType().Name);}finally{if(stamp==iconGeneration)loadingIcons=false;}}
  void ApplyIcons(){foreach(var image in GetComponentsInChildren<RawImage>(true))if(image.name.StartsWith("EquipmentIcon:")&&EquipmentIconResources.TryRegion(image.name.Substring(14),out var uv)){image.texture=icons?.Texture;image.uvRect=uv;image.color=icons==null?Color.clear:Color.white;}}
  void Icon(Transform parent,string key){
   var image=parent.GetComponentsInChildren<RawImage>(true).FirstOrDefault(i=>i.transform.parent==parent&&i.name.StartsWith("EquipmentIcon:"));
   bool valid=EquipmentIconResources.TryRegion(key,out var uv);
   if(!valid){if(image!=null){image.texture=null;image.name="EquipmentIcon:";image.gameObject.SetActive(false);}return;}
   if(image==null){var r=Box("EquipmentIcon:"+key,parent,Vector2.up,Vector2.up,0);r.pivot=Vector2.up;r.anchoredPosition=new Vector2(4,-4);r.sizeDelta=new Vector2(76,76);image=r.gameObject.AddComponent<RawImage>();image.raycastTarget=false;}
   image.name="EquipmentIcon:"+key;image.gameObject.SetActive(true);image.uvRect=uv;image.texture=icons?.Texture;image.color=icons==null?Color.clear:Color.white;
  }
  readonly ScreenBackKey backKey=new ScreenBackKey();
  public static EquipmentInventoryView Open(Transform parent,TMP_FontAsset font,EquipmentInventoryController owner,Action closed,EquipmentIconResources iconResources=null){
   if(parent==null||owner==null)throw new ArgumentNullException();
   var root=new GameObject("EquipmentInventory",typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);Stretch((RectTransform)root.transform);
   root.GetComponent<Image>().color=new Color(.025f,.035f,.045f,.99f);
   var view=root.AddComponent<EquipmentInventoryView>();view.font=LastRail.Battle.CombatEditorFonts.Resolve(font);view.closed=closed;view.iconResources=iconResources;view.Build();view.Bind(owner);return view;
  }
  static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
  static RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,float inset=12){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.one*inset;r.offsetMax=Vector2.one*-inset;return r;}
  TMP_Text Label(string name,Transform parent,string value,int size){var r=Box(name,parent,Vector2.zero,Vector2.one,8);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.88f,.92f,.94f);t.richText=false;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=16;t.fontSizeMax=size;return t;}
  Button Button(string name,Transform parent,string label,Action action){var r=Box(name,parent,Vector2.zero,Vector2.one,0);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.15f,.19f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(bound&&isActiveAndEnabled)action();});Label("Label",r,label,26).alignment=TextAlignmentOptions.MidlineLeft;return b;}
  void Build(){
   art=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
   Label("Title",Box("Header",transform,new Vector2(0,.88f),Vector2.one),"장비 보관함",38);
   refreshButton=Button("Refresh",Box("RefreshArea",transform,new Vector2(.78f,.88f),Vector2.one),"새로고침",()=>refresh?.Invoke());refreshButton.gameObject.SetActive(false);
   summary=Label("Summary",Box("Status",transform,new Vector2(0,.77f),new Vector2(1,.88f)),"",24);
   var list=Box("List",transform,new Vector2(0,.13f),new Vector2(.49f,.77f));var mask=list.gameObject.AddComponent<Image>();mask.color=new Color(.02f,.04f,.05f);list.gameObject.AddComponent<RectMask2D>();
   scroll=list.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=list;
   rows=Box("Rows",list,Vector2.up,Vector2.one,0);rows.pivot=Vector2.up;rows.sizeDelta=Vector2.zero;scroll.content=rows;
   detail=Box("Detail",transform,new Vector2(.49f,.13f),new Vector2(1,.77f));
   if(art?.PanelTexture!=null){var background=detail.gameObject.AddComponent<RawImage>();background.texture=art.PanelTexture;background.color=new Color(.1f,.14f,.17f);background.raycastTarget=false;}
   detailLabel=Label("SelectedItem",detail,"",28);
   Button("Back",Box("Footer",transform,Vector2.zero,new Vector2(1,.13f)),"뒤로",RequestBack);
  }
  public void Bind(EquipmentInventoryController controller){if(controller==null)throw new ArgumentNullException(nameof(controller));Unbind();owner=controller;if(isActiveAndEnabled){bound=true;owner.Changed+=Render;Render();}}
  void Detach(){if(owner!=null)owner.Changed-=Render;bound=false;ReleaseIcons();}
  public void Unbind(){Detach();owner=null;}
  void OnEnable(){backKey.Synchronize();if(owner!=null&&!bound){bound=true;owner.Changed+=Render;Render();}}
  void OnDisable()=>Detach();
  void OnDestroy(){Unbind();owner=null;closed=null;refresh=null;}
  void OnGUI(){if(bound)backKey.Handle(Event.current,RequestBack,ScreenBackKey.Editing);}
  public void RequestBack(){if(!bound)return;if(owner.SelectedId!=null){owner.Back();return;}var callback=closed;closed=null;gameObject.SetActive(false);if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);callback?.Invoke();}
  void Render(){
   if(!bound||rows==null)return;float position=scroll.verticalNormalizedPosition;var snapshot=owner.Snapshot;
   if(snapshot==null){foreach(var row in rowViews){row.Id=null;row.Root.gameObject.SetActive(false);}ReleaseIcons();summary.text="장비 정보를 아직 받지 못했습니다.";rows.sizeDelta=Vector2.zero;detailLabel.text="장비 정보가 도착하면 표시됩니다.";detailLabel.rectTransform.offsetMax=new Vector2(-8,-8);Icon(detail,null);return;}
   summary.text="보관 "+snapshot.Used+" / "+snapshot.Capacity+"  ·  전체 장비 "+snapshot.Items.Count;
   rows.sizeDelta=new Vector2(0,snapshot.Items.Count*92);
   while(rowViews.Count>snapshot.Items.Count){var retired=rowViews[rowViews.Count-1];rowViews.RemoveAt(rowViews.Count-1);retired.Id=null;retired.Root.gameObject.SetActive(false);if(Application.isPlaying)Destroy(retired.Root.gameObject);else DestroyImmediate(retired.Root.gameObject);}
   for(int index=0;index<snapshot.Items.Count;index++){
    if(index==rowViews.Count){var row=new Row{Root=Box("Row"+index,rows,Vector2.up,Vector2.one,0)};row.Root.pivot=Vector2.up;row.Root.anchoredPosition=new Vector2(0,-index*92);row.Root.sizeDelta=new Vector2(0,84);
     row.Input=Button("Select",row.Root,"",()=>{if(row.Id!=null&&row.Root!=null&&row.Root.gameObject.activeInHierarchy)owner.Select(row.Id);});row.Label=row.Input.GetComponentInChildren<TMP_Text>();rowViews.Add(row);
     if(art?.ItemFrame!=null)ApprovedUiConnections.Frame(row.Root,"ApprovedItemFrame",art.ItemFrame,70f/3);
    }
    var item=snapshot.Items[index];var cell=rowViews[index];cell.Id=item.Id;cell.Root.gameObject.SetActive(true);
    cell.Label.text=(item.Name??"장비 "+(index+1))+"  "+new string('★',item.Star)+"\n"+(item.Location=="STORED"?"보관 중":"장착 중")+(item.Locked?" · 잠금":"");
    Icon(cell.Root,item.IconKey);cell.Label.rectTransform.offsetMin=new Vector2(EquipmentIconResources.TryRegion(item.IconKey,out _)?88:8,8);
    cell.Input.GetComponent<Image>().color=item.Id==owner.SelectedId?new Color(.15f,.3f,.35f):new Color(.08f,.15f,.19f);
   }
   var selected=owner.Selected;
   detailLabel.text=selected==null?(snapshot.Items.Count==0?"보유 장비가 없습니다.":"목록에서 장비를 선택하세요."):(selected.Name??"장비 상세")+"\n\n"+new string('★',selected.Star)+"\n\n"+(selected.Location=="STORED"?"보관 중":(selected.WearerKind=="operator"?"대원":"포탑")+" 장착 · 슬롯 "+(selected.Slot+1))+"\n"+(selected.Locked?"잠금 상태":"잠금 해제 상태")+"\n세트 "+selected.SetTag.Substring(3)+"\n\n"+(selected.Effects??"이름·효과 상세는 아직 제공되지 않았습니다.");
   Icon(detail,selected?.IconKey);detailLabel.rectTransform.offsetMax=new Vector2(-8,-8);
   if(selected!=null&&EquipmentIconResources.TryRegion(selected.IconKey,out _)){var icon=(RectTransform)detail.Find("EquipmentIcon:"+selected.IconKey);icon.anchorMin=icon.anchorMax=icon.pivot=Vector2.one;icon.anchoredPosition=new Vector2(-8,-8);detailLabel.rectTransform.offsetMax=new Vector2(-96,-8);}
   ApplyIcons();EnsureIcons();
   scroll.verticalNormalizedPosition=position;
  }
 }
}
