using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace LastRail.Qa {
 // Editor-only fixture controller. It cannot START a run, call network APIs or grant rewards.
 public sealed class MapGrayboxFixtureController:MonoBehaviour {
  [SerializeField] TMP_FontAsset font;
  readonly Dictionary<Button,UnityAction> bindings=new Dictionary<Button,UnityAction>();
  readonly List<MapGrayboxFixtureGraphic> graphics=new List<MapGrayboxFixtureGraphic>();
  MapGrayboxFixtureGeometry geometry;MapGrayboxFixtureRun model;MapGrayboxFixtureRun.TrustedCheckpoint checkpoint;
  TMP_Text status;float elapsed;bool autoRun;int attempts,commands;string notice="Ready: TEST_ONLY default bypass";
  public MapGrayboxFixtureRun Model=>model;public int CommandCount=>commands;public int AttemptCount=>attempts;public int BindingCount=>bindings.Count;
  public void Initialize(TMP_FontAsset fixtureFont){font=fixtureFont;Unbind();for(int i=transform.childCount-1;i>=0;i--){var child=transform.GetChild(i);child.gameObject.SetActive(false);child.SetParent(null,false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}graphics.Clear();geometry=new MapGrayboxFixtureGeometry();model=new MapGrayboxFixtureRun(geometry);attempts=commands=0;elapsed=0;autoRun=false;checkpoint=null;Build();Bind();Refresh();}
  void OnEnable(){if(Application.isPlaying&&font!=null)Initialize(font);}
  void OnDisable(){autoRun=false;Unbind();}
  public void Bind(){Unbind();foreach(var button in GetComponentsInChildren<Button>(true)){string command=button.name;UnityAction callback=()=>Dispatch(command);bindings.Add(button,callback);button.onClick.AddListener(callback);}foreach(var graphic in graphics)graphic.Bind(geometry,model);}
  public void Unbind(){foreach(var pair in bindings)if(pair.Key!=null)pair.Key.onClick.RemoveListener(pair.Value);bindings.Clear();foreach(var graphic in graphics)if(graphic!=null)graphic.Unbind();}
  void Update(){if(!Application.isPlaying||!autoRun||model==null)return;elapsed+=Mathf.Min(Time.unscaledDeltaTime,.25f);while(elapsed>=.05f){elapsed-=.05f;Step();}Refresh();}
  public void Step(){if(model==null)return;attempts++;model.Step();}
  public void Advance(int count){if(count<0||count>10000)throw new ArgumentOutOfRangeException();for(int i=0;i<count;i++)Step();Refresh();}
  public void LoadScenario(int fallDelay=320,int hp=1000){model=new MapGrayboxFixtureRun(geometry,fallDelay,5,hp);checkpoint=null;attempts=commands=0;elapsed=0;autoRun=false;foreach(var graphic in graphics)graphic.Bind(geometry,model);Refresh();}
  void Dispatch(string command){if(model==null)return;commands++;string result="applied";switch(command){
   case "RunPause":autoRun=!autoRun;result=autoRun?"autostep enabled":"autostep stopped";break;
   case "Step":Step();break;case "Step40":Advance(40);break;
   case "Direct":result=model.Reserve(0).ToString();break;case "Bypass":result=model.Reserve(1).ToString();break;case "Cancel":result=model.Reserve(null).ToString();break;
   case "Low":model.Throttle(7000);break;case "Cruise":model.Throttle(10000);break;case "High":model.Throttle(13000);break;
   case "PauseModel":model.Pause(!model.Paused);break;case "WaveComplete":model.CompleteWave();break;case "DamageRock":result=model.DamageRock(100).ToString();break;
   case "Confirm":result=model.ConfirmAugment().ToString();break;case "Save":checkpoint=model.Checkpoint();break;case "Restore":if(checkpoint==null)result="no local checkpoint";else model.Restore(checkpoint);break;
   case "Reset":LoadScenario();break;default:throw new ArgumentException("Unknown fixture UI command");}
   notice=command+": "+result;Refresh();
  }
  public void Refresh(){if(model==null||status==null)return;JObject s=(JObject)model.Capture()["state"];status.text="Attempt "+attempts+"  |  Tick "+model.Tick+"  |  "+model.Distance/1000d+" m  |  route "+(model.Route==0?"DIRECT":"BYPASS")+"  |  locked "+s["locked"]+"\nHP "+s["hp"]+"  hits "+s["hits"]+"  rock "+s["rock"]+"  actual/target "+s["actual"]+" / "+s["target"]+"  recovery "+s["recovery"]+"\n"+s["phase"]+"  pending "+s["pending"]+"  tail joined "+s["joined"]+"  paused "+s["paused"]+"  |  "+notice;foreach(var graphic in graphics)graphic.SetVerticesDirty();}
  RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  TMP_Text Text(string name,string text,float x,float y,float w,float h,int size){var label=Rect(name,transform,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.text=text;label.color=new Color(.86f,.94f,1);label.raycastTarget=false;return label;}
  void Build(){
   var background=gameObject.GetComponent<Image>()??gameObject.AddComponent<Image>();background.color=new Color(.025f,.045f,.065f);background.raycastTarget=false;
   Text("FixtureTitle","MAP GRAYBOX  /  TEST ONLY  /  OBSERVED V1",40,20,1500,50,34);
   status=Text("ModelState","",40,80,1520,115,23);
   var stage=Rect("MechanicsViewport",transform,40,210,1520,395);var shade=stage.gameObject.AddComponent<Image>();shade.color=new Color(.045f,.07f,.095f);shade.raycastTarget=false;
   foreach(var layer in new[]{"hazard","paths","train"}){var graphic=Rect("Layer_"+layer,stage,0,0,1520,395).gameObject.AddComponent<MapGrayboxFixtureGraphic>();graphic.Layer=layer;graphic.Bind(geometry,model);graphics.Add(graphic);}
   Text("Legend","CYAN selected path   YELLOW warning 40m   MAGENTA lock 80m   AMBER rock zone   WHITE engine / GREEN cars",55,610,1490,36,21);
   for(int i=0;i<geometry.Ids.Length;i++){var id=geometry.Ids[i];float x=40+i%3*510,y=654+i/3*125;var graphic=Rect("Geometry_"+id,transform,x,y,490,110).gameObject.AddComponent<MapGrayboxFixtureGraphic>();graphic.Module=id;graphic.Bind(geometry,model);graphics.Add(graphic);Text("Label_"+id,id,x+10,y,470,25,18);}
   string[] ids={"RunPause","Step","Step40","Direct","Bypass","Cancel","Low","Cruise","High","PauseModel","WaveComplete","DamageRock","Confirm","Save","Restore","Reset"};
   string[] labels={"Run / Stop","Step 50ms","+40 steps","Direct","Bypass","Cancel","Low","Cruise","High","Pause model","Wave done","Break rock","Confirm hold","Save local","Restore local","Reset"};
   for(int i=0;i<ids.Length;i++){float x=40+i%8*191,y=914+i/8*52;var r=Rect(ids[i],transform,x,y,181,44);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.09f,.22f,.30f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var text=Text(ids[i]+"Label",labels[i],x+4,y+6,173,32,19);text.alignment=TextAlignmentOptions.Center;text.transform.SetParent(r,true);}
   Text("BoundaryNotice","Known onset441 boundary: observed hit=1; endpoint-start oracle=0. UNRESOLVED. No production damage/reward contract.",40,1030,1510,45,21);
  }
 }
}
