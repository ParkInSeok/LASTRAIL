using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell {
 // Display and input intent only. The travel owner retains every accepted value.
 public sealed class OrdinaryTravelPanel:MonoBehaviour {
  Action inventoryAction;Button inventoryButton;
  public void BindInventory(Action action){inventoryAction=action;if(inventoryButton!=null)inventoryButton.gameObject.SetActive(action!=null);}

  TMP_FontAsset font;
  TMP_Text heading,status,nozzleLabel;
  RectTransform choices;
  OrdinaryTravelNozzle nozzle;
  Button pause,resume,retry;
  Action<string> choose;
  Action<int> throttle;
  Action pauseAction,resumeAction,retryAction,backAction,background;
  readonly List<GameObject> rows=new List<GameObject>();readonly List<string> rowEdges=new List<string>();
  int confirmed;
  ApprovedUiConnections art;RawImage mapBackground;TMP_Text currentLocation;
  Action<bool> visibility;
  bool focused,applicationPaused;
  bool Visible=>isActiveAndEnabled&&focused&&!applicationPaused;
  public void BindVisibility(Action<bool> callback){visibility=callback;visibility?.Invoke(Visible);}
  public static OrdinaryTravelPanel Create(Transform parent,TMP_FontAsset font){
   var root=new GameObject("OrdinaryTravel",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(parent,false);
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
   var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
   var view=root.AddComponent<OrdinaryTravelPanel>();view.font=font;view.Build();return view;
  }
  public void Bind(Action<string> choose,Action<int> throttle,Action pause,Action resume,Action retry,Action back,Action background){
   this.choose=choose;this.throttle=throttle;pauseAction=pause;resumeAction=resume;retryAction=retry;backAction=back;this.background=background;
  }
  public void Unbind(){BindInventory(null);choose=null;throttle=null;pauseAction=resumeAction=retryAction=backAction=background=null;visibility=null;}
  void Build(){
   art=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
   var shade=Rect("TravelBackground",transform,0,0,1280,720);shade.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.065f,.98f);
   if(art!=null&&art.MapBackground!=null){mapBackground=Rect("ApprovedMapBackground",transform,0,0,1280,720).gameObject.AddComponent<RawImage>();mapBackground.texture=art.MapBackground;float crop=720f/1280*art.MapBackground.width/art.MapBackground.height;mapBackground.uvRect=new Rect(0,1-crop,1,crop);mapBackground.color=new Color(.3f,.3f,.3f,1);mapBackground.raycastTarget=false;}
   heading=Text("DestinationHeading",transform,64,40,970,54,32);status=Text("TravelStatus",transform,64,116,1152,120,24);
   currentLocation=Text("CurrentLocation",transform,108,204,1108,40,24);
   if(art?.CurrentLocation!=null){var pin=Rect("CurrentLocationPin",currentLocation.transform,-44,0,36,36).gameObject.AddComponent<RawImage>();pin.texture=art.CurrentLocation;pin.raycastTarget=false;}
   currentLocation.textWrappingMode=TextWrappingModes.NoWrap;currentLocation.overflowMode=TextOverflowModes.Ellipsis;currentLocation.gameObject.SetActive(false);
   choices=Rect("DestinationChoices",transform,64,260,1152,260);
   Button("Back",transform,1060,40,156,54,"로비",()=>backAction?.Invoke());
   inventoryButton=Button("Equipment",transform,880,40,156,54,"장비",()=>inventoryAction?.Invoke());inventoryButton.gameObject.SetActive(false);
   var track=Rect("Nozzle",transform,90,420,1100,56);track.gameObject.AddComponent<Image>().color=new Color(.12f,.21f,.27f);
   nozzle=track.gameObject.AddComponent<OrdinaryTravelNozzle>();nozzle.wholeNumbers=true;
   var handle=Rect("NozzleHandle",track,0,0,40,56);var image=handle.gameObject.AddComponent<Image>();image.color=new Color(.2f,.8f,1);
   nozzle.handleRect=handle;nozzle.targetGraphic=image;nozzle.direction=Slider.Direction.LeftToRight;
   nozzle.onValueChanged.AddListener(v=>nozzleLabel.text="선택 예정 "+(v/100f).ToString("0.##")+"% · 확정 "+(confirmed/100f).ToString("0.##")+"%");
   nozzle.Released=v=>{if(v!=confirmed)throttle?.Invoke(v);};
   nozzleLabel=Text("NozzleValue",transform,90,350,1100,54,26);
   pause=Button("Pause",transform,90,560,240,64,"일시정지",()=>pauseAction?.Invoke());
   resume=Button("Resume",transform,360,560,240,64,"계속 이동",()=>resumeAction?.Invoke());
   retry=Button("Recover",transform,840,560,350,64,"원래 요청 확인",()=>retryAction?.Invoke());
  }
  public void ShowChoices(IEnumerable<(string edge,string label)> values,bool enabled,string message)=>ShowChoices(values.Select(v=>(v.edge,v.label,(string)null)),enabled,message);
  public void ShowChoices(IEnumerable<(string edge,string label,string kind)> values,bool enabled,string message,string currentNodeName=null){
   heading.text="다음 목적지";status.text=message;SetTravelVisible(false);ClearChoices();if(mapBackground!=null)mapBackground.gameObject.SetActive(true);int i=0;
   currentLocation.text=string.IsNullOrEmpty(currentNodeName)?string.Empty:"현재 위치: "+currentNodeName;currentLocation.gameObject.SetActive(!string.IsNullOrEmpty(currentNodeName));status.rectTransform.sizeDelta=new Vector2(1152,currentLocation.gameObject.activeSelf?80:120);
   foreach(var value in values){
    if(i==rows.Count){int cell=i;var created=Button("Destination"+i,choices,i*380,0,360,120,"",()=>{if(rowEdges[cell]!=null)choose?.Invoke(rowEdges[cell]);});
     var image=Rect("NodeKind",created.transform,18,24,72,72).gameObject.AddComponent<RawImage>();image.raycastTarget=false;
     if(art?.PanelFrame!=null)ApprovedUiConnections.Frame(created.transform,"ApprovedPanelFrame",art.PanelFrame,150f/17);
     rows.Add(created.gameObject);rowEdges.Add(null);
    }
    var b=rows[i].GetComponent<Button>();rowEdges[i]=value.edge;b.gameObject.SetActive(true);b.interactable=enabled;
    var icon=b.transform.Find("NodeKind").GetComponent<RawImage>();icon.texture=art?.MapIcon(value.kind);icon.gameObject.SetActive(icon.texture!=null);
    var label=b.GetComponentInChildren<TMP_Text>();label.text=value.label;label.rectTransform.anchoredPosition=new Vector2(icon.texture!=null?102:12,-12);label.rectTransform.sizeDelta=new Vector2(icon.texture!=null?246:336,96);i++;
   }
   while(rows.Count>i){int last=rows.Count-1;var retired=rows[last];rows.RemoveAt(last);rowEdges.RemoveAt(last);if(Application.isPlaying)Destroy(retired);else DestroyImmediate(retired);}   retry.gameObject.SetActive(!enabled);
  }
  public void ShowTravel(string name,string message,int confirmedBps,int minimum,int maximum,bool paused,bool busy){
   ClearChoices();SetTravelVisible(true);heading.text=name;status.text=message;confirmed=confirmedBps;
   nozzle.minValue=minimum;nozzle.maxValue=maximum;if(!nozzle.Dragging)nozzle.SetValueWithoutNotify(confirmed);
   nozzle.interactable=!busy&&!paused;nozzleLabel.text=(busy?"확인 중 · ":"")+"확정 속력 "+(confirmed/100f).ToString("0.##")+"%";
   pause.interactable=!paused;resume.interactable=paused&&!busy;retry.interactable=!busy;
  }
  public void SetStatus(string message){status.text=message;}
  public void ShowUnavailable(string message){ClearChoices();SetTravelVisible(false);heading.text="이동 준비";status.text=message;retry.gameObject.SetActive(true);retry.interactable=true;}
  public void ShowArrivalPaused(string message){ShowUnavailable(message);resume.gameObject.SetActive(true);resume.interactable=true;}
  void SetTravelVisible(bool visible){currentLocation.gameObject.SetActive(false);status.rectTransform.sizeDelta=new Vector2(1152,120);if(mapBackground!=null)mapBackground.gameObject.SetActive(false);nozzle.gameObject.SetActive(visible);nozzleLabel.gameObject.SetActive(visible);pause.gameObject.SetActive(visible);resume.gameObject.SetActive(visible);retry.gameObject.SetActive(visible);}
  void ClearChoices(){for(int i=0;i<rows.Count;i++){rows[i].SetActive(false);rowEdges[i]=null;rows[i].GetComponentInChildren<TMP_Text>(true).text="";var icon=rows[i].transform.Find("NodeKind").GetComponent<RawImage>();icon.texture=null;}}
  void Awake(){focused=Application.isFocused;}
  void OnApplicationFocus(bool focus){focused=focus;if(!focus)background?.Invoke();visibility?.Invoke(Visible);}
  void OnEnable(){visibility?.Invoke(Visible);}
  void OnDisable(){visibility?.Invoke(false);}
  void OnApplicationPause(bool paused){applicationPaused=paused;if(paused)background?.Invoke();visibility?.Invoke(Visible);}
  void OnDestroy(){Unbind();}
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  TMP_Text Text(string name,Transform parent,float x,float y,float w,float h,float size){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;t.richText=false;return t;}
  Button Button(string name,Transform parent,float x,float y,float w,float h,string label,Action action){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.09f,.23f,.32f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>{if(this!=null&&isActiveAndEnabled&&button!=null&&button.isActiveAndEnabled&&button.interactable)action();});var t=Text(name+"Label",r,12,12,w-24,h-24,24);t.text=label;t.alignment=TextAlignmentOptions.Center;return button;}
 }
 public sealed class OrdinaryTravelNozzle:Slider {
  public Action<int> Released;
  public bool Dragging{get;private set;}
  public override void OnPointerDown(PointerEventData data){if(!IsInteractable())return;Dragging=true;base.OnPointerDown(data);}
  public override void OnPointerUp(PointerEventData data){base.OnPointerUp(data);Dragging=false;if(IsInteractable())Released?.Invoke((int)value);}
  public override void OnMove(AxisEventData data){base.OnMove(data);if(IsInteractable())Released?.Invoke((int)value);}
 }
}
