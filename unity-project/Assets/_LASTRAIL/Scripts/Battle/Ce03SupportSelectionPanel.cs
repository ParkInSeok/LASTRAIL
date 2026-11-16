using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Explicitly injected local-host screen. Does not select defaults or start combat.
public sealed class Ce03SupportSelectionPanel:MonoBehaviour,ICe03SupportSelectionView {
 readonly List<Button> choices=new List<Button>();
 public event Action<string> ToggleRequested;
 public event Action<int> RemoveRequested,MoveLeftRequested;
 public event Action SaveRequested,BackRequested;
 Ce03SupportSelectionController controller;Ce03SupportSelectionState state;
 CombatPresentationAssets art;RectTransform root;TMP_Text notice,slots,details;Button save;
 readonly List<Button> removeButtons=new List<Button>(),moveButtons=new List<Button>();readonly List<TMP_Text> slotLabels=new List<TMP_Text>();
 readonly List<RawImage> slotIcons=new List<RawImage>();readonly List<CombatReadyFrame> slotFrames=new List<CombatReadyFrame>();readonly List<TMP_Text> choiceBadges=new List<TMP_Text>();RawImage detailIcon;
 readonly List<GameObject> choiceChecks=new List<GameObject>();
 RawImage SkillIcon(string name,Transform parent,float x,float y,float size,string id){var r=Rect(name,x,y,size,size);r.SetParent(parent,false);var image=r.gameObject.AddComponent<RawImage>();image.raycastTarget=false;var child=Rect(name+"Glyph",0,0,size,size);child.SetParent(r,false);child.gameObject.AddComponent<SupportRoleGlyph>().raycastTarget=false;SetSkillIcon(image,id);return image;}
 void SetSkillIcon(RawImage image,string id){image.gameObject.SetActive(id!=null);if(id==null)return;string resource=id=="support_fire"?"support_fire-v01":id=="barrier"?"support_barrier-v01":id=="emergency_repair"?"support_repair-v01":null;image.texture=resource==null?null:LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/"+resource);image.color=image.texture==null?Color.clear:Color.white;var glyph=image.GetComponentInChildren<SupportRoleGlyph>(true);glyph.gameObject.SetActive(image.texture==null);glyph.Role=id;glyph.SetAllDirty();}
 public static Ce03SupportSelectionPanel Open(Transform parent,Ce03LocalClient client,Action back){
  var go=new GameObject("SupportSelection",typeof(RectTransform));go.transform.SetParent(parent,false);var screen=(RectTransform)go.transform;screen.anchorMin=Vector2.zero;screen.anchorMax=Vector2.one;screen.offsetMin=screen.offsetMax=Vector2.zero;var view=go.AddComponent<Ce03SupportSelectionPanel>();view.Build(client,back);return view;
 }
 RectTransform Rect(string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(root,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 TMP_Text Text(string name,string value,float x,float y,float w,float h,int size){var t=Rect(name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=CombatEditorFonts.Resolve(art.Font);t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.75f;t.fontSizeMax=size;t.text=value;t.richText=false;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;return t;}
 Button Button(string name,string label,float x,float y,float w,float h,Action action){var r=Rect(name,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=art.Secondary;image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(isActiveAndEnabled&&b.interactable)action();});var t=Text(name+"Label",label,x+8,y+8,w-16,h-16,21);t.transform.SetParent(r,true);return b;}
 void Decoration(string name,string resource,float x,float y,float w,float h,bool crop){var image=Rect(name,x,y,w,h).gameObject.AddComponent<RawImage>();var texture=LastRail.Resources.PreparedContent.Load<Texture2D>(resource);image.texture=texture;image.raycastTarget=false;if(crop)image.uvRect=LastRail.Presentation.MissionShell.OperatorVisualCatalog.Crop(texture,w,h);}
 void Plate(string name,float x,float y,float w,float h){var image=Rect(name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=art.Secondary;image.type=Image.Type.Sliced;image.color=new Color(.18f,.25f,.29f);image.raycastTarget=false;}
 void Build(Ce03LocalClient value,Action back){
  if(value==null)throw new ArgumentNullException(nameof(value));art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");if(art?.Font==null||art.Secondary==null)throw new InvalidOperationException("Presentation assets unavailable.");
  root=new GameObject("SupportContent",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(transform,false);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;root.sizeDelta=new Vector2(720,1510);
  var background=gameObject.AddComponent<Image>();background.color=new Color(.025f,.04f,.045f,1);
  Decoration("SupportRoom","NoncombatSupport/support-room",0,0,720,330,true);
  Decoration("SupportHost","NoncombatSupport/support-host",424,52,235,282,false);
  Plate("TitlePlate",24,24,400,80);Text("Title","지휘관 스킬 선택",40,37,370,53,32);
  Plate("GuidePlate",30,125,365,150);Text("Guide","스킬 3개를 선택하세요.\n저장한 순서대로 배치됩니다.",50,142,325,114,25);
  slots=Text("Selected","",35,333,650,38,23);
  for(int i=0;i<3;i++){int index=i;float x=35+i*222;Plate("SelectedSlot"+i,x,380,207,115);slotIcons.Add(SkillIcon("SlotIcon"+i,root,x+22,399,40,null));slotLabels.Add(Text("SlotName"+i,"",x+74,389,120,52,20));removeButtons.Add(Button("RemoveSlot"+i,"×",x+151,445,42,38,()=>RemoveRequested?.Invoke(index)));moveButtons.Add(Button("MoveLeft"+i,"←",x+94,445,46,38,()=>MoveLeftRequested?.Invoke(index)));
   foreach(var control in new[]{removeButtons[i],moveButtons[i]}){control.image.sprite=null;control.image.color=new Color(.08f,.23f,.30f);control.GetComponentInChildren<TMP_Text>().fontSize=26;control.GetComponentInChildren<TMP_Text>().fontSizeMax=26;}
   var frameRect=Rect("SlotOutline"+i,x+4,384,199,107);var outline=frameRect.gameObject.AddComponent<CombatReadyFrame>();outline.raycastTarget=false;slotFrames.Add(outline);
  }
  Plate("DetailsPlate",35,1060,650,195);detailIcon=SkillIcon("DetailIcon",root,58,1100,90,null);details=Text("Details","스킬을 선택해 상태를 확인하세요.",165,1080,490,150,25);
  notice=Text("Notice","불러오는 중",35,1275,650,80,22);
  save=Button("Save","저장",365,1390,320,78,()=>SaveRequested?.Invoke());Button("Back","돌아가기",35,1390,300,78,()=>BackRequested?.Invoke());
  Fit();
  controller=new Ce03SupportSelectionController(value,this,()=>{back?.Invoke();if(this==null)return;gameObject.SetActive(false);if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);});
  Activate();
 }
 async void Activate(){if(controller!=null)await controller.ActivateAsync();}
 static string Name(string id)=>id=="support_fire"?"지원 사격":id=="barrier"?"방어막":id=="emergency_repair"?"긴급 수리":id=="ce03.role.suppressive_barrage"?"제압 포격":id=="ce03.role.medical_support"?"의료 지원":id=="ce03.role.smoke"?"연막 전개":id=="ce03.role.weakness_scan"?"약점 스캔":id=="ce03.role.firepower_boost"?"화력 증폭":id=="ce03.role.electronic_disruption"?"전자기 교란":id=="ce03.role.emergency_protection"?"긴급 방호":throw new FormatException("Unknown catalog role.");
 public void Render(Ce03SupportSelectionState value){state=value;var entries=state.Entries;
  for(int i=0;i<entries.Count;i++)
  {
   var e=entries[i];string id=e.Id;bool available=e.Available;
   if(i==choices.Count){int cell=i;var created=Button("Choice"+i,"",35+i%2*335,520+i/2*105,315,88,()=>{if(state!=null&&cell<state.Entries.Count)ToggleRequested?.Invoke(state.Entries[cell].Id);});choices.Add(created);
   SkillIcon("ChoiceIcon"+i,created.transform,38,24,40,id);
   var checkRect=Rect("ChoiceCheck"+i,277,24,20,20);checkRect.SetParent(created.transform,false);var check=checkRect.gameObject.AddComponent<LastRail.Presentation.MissionShell.RewardResourceSymbol>();check.Kind="CHECK";check.raycastTarget=false;choiceChecks.Add(check.gameObject);
   var badge=Text("ChoiceBadge"+i,"",276,50,22,22,18);badge.transform.SetParent(created.transform,false);choiceBadges.Add(badge);
   var createdStatus=Text("ChoiceState"+i,"",102,54,162,24,19);createdStatus.transform.SetParent(created.transform,false);createdStatus.color=new Color(.72f,.78f,.79f);
   }
   var b=choices[i];b.GetComponentInChildren<TMP_Text>().text=Name(id);
   // Reserve a72-wide icon lane and an independent right-hand selection-order lane.
   var caption=b.GetComponentInChildren<TMP_Text>().rectTransform;caption.anchoredPosition=new Vector2(102,available?-32:-22);caption.sizeDelta=new Vector2(162,24);
   b.transform.Find("ChoiceState"+i).GetComponent<TMP_Text>().text=available?"":"준비 중";
   SetSkillIcon(b.transform.Find("ChoiceIcon"+i).GetComponent<RawImage>(),id);  }
  notice.text=state.Notice;
  var selected=state.Selected;var focused=state.Focused;
  slots.text="선택한 지원 · "+selected.Count+" / 3";save.interactable=state.CanSave;save.GetComponentInChildren<TMP_Text>().text=state.Pending?"저장 결과 재확인":"저장";
  save.image.color=save.interactable?new Color(.12f,.62f,1):new Color(.38f,.43f,.47f);save.GetComponentInChildren<TMP_Text>().color=save.interactable?Color.white:new Color(.7f,.73f,.75f);
  for(int i=0;i<3;i++){slotLabels[i].text=(i+1)+" · "+(i<selected.Count?Name(selected[i]):"미선택");SetSkillIcon(slotIcons[i],i<selected.Count?selected[i]:null);slotFrames[i].SetLight(i<selected.Count?1:0,1);removeButtons[i].interactable=state.CanEdit&&i<selected.Count;moveButtons[i].interactable=state.CanEdit&&i>0&&i<selected.Count;}
  for(int i=0;i<choices.Count;i++){var b=choices[i];b.interactable=state.CanEdit;int order=IndexOf(selected,entries[i].Id);b.image.color=order>=0?new Color(.3f,.8f,1):Color.white;choiceBadges[i].text=order>=0?(order+1).ToString():"";choiceChecks[i].SetActive(order>=0);}
  if(focused!=null&&entries!=null){SetSkillIcon(detailIcon,focused);var entry=entries.Single(e=>e.Id==focused);details.text=Name(focused)+"\n\n"+(entry.Available?"현재 출격에 사용할 수 있습니다.":"준비 중인 지원입니다. 저장은 가능하지만 현재 출격에는 사용할 수 없습니다.");}
 }
 static int IndexOf(IReadOnlyList<string> values,string id){for(int i=0;i<values.Count;i++)if(values[i]==id)return i;return -1;}
 void Fit(){if(root==null||!(root.parent is RectTransform parent))return;var area=parent.rect;if(area.width>0&&area.height>0)root.localScale=Vector3.one*Mathf.Min(area.width/720f,area.height/1510f);}
 void OnRectTransformDimensionsChange(){Fit();}
 void OnDisable(){controller?.Deactivate();}
 void OnEnable(){Activate();}
 void OnDestroy(){controller?.Dispose();controller=null;}
}
}
