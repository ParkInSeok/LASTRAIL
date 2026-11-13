#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using LastRail.Battle;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Diagnostics {
 public sealed class ObstacleTrialView : MonoBehaviour {
  readonly Dictionary<string,RectTransform> controls=new Dictionary<string,RectTransform>();
  readonly List<RectTransform> bullets=new List<RectTransform>();
  RectTransform root,rock,healthFill,warning,turret;
  TMP_Text status,offscreen;
  TMP_FontAsset font;
  ObstacleTrialController controller;
  const float PixelsPerMeter=10.5f,HeadX=650,GroundY=480;
  public RectTransform Control(string name)=>controls[name];
  RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
  }
  Image Box(string name,Transform parent,float x,float y,float w,float h,Color color){
   var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;
  }
  TMP_Text Text(string name,float x,float y,float w,float h,string value,float size=24){
   var t=Rect(name,root,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;return t;
  }
  void Button(string name,float x,string title,Action action){
   var b=Box(name,root,x,720,170,65,new Color(.12f,.2f,.23f));b.raycastTarget=true;
   var button=b.gameObject.AddComponent<Button>();button.targetGraphic=b;button.onClick.AddListener(()=>{action();Refresh();});
   Text(name+"Label",x+12,736,155,40,title,22);controls.Add(name,b.rectTransform);
  }
  RawImage Raw(string name,Texture texture,float x,float y,float w,float h){
   var r=Rect(name,root,x,y,w,h).gameObject.AddComponent<RawImage>();r.texture=texture;r.raycastTarget=false;return r;
  }
  public void Bind(ObstacleTrialController owner,CombatPresentationAssets art,OrdinaryRoofPresentationAssets roof){
   controller=owner;font=art.Font;
   var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
   var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   gameObject.AddComponent<GraphicRaycaster>();
   root=Rect("TrialViewport",transform,0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;root.gameObject.AddComponent<RectMask2D>();
   Raw("CurrentCanyon",art.DistantFar??art.ExteriorMap,0,0,1600,610);
   foreach(JObject row in (JArray)JObject.Parse(art.DistantTerrainLayout.text)["layers"]){
    var layer=Rect("CurrentTerrain_"+(string)row["id"],root,0,0,1600,650).gameObject.AddComponent<CombatCanyonLayer>();
    layer.Configure(art.DistantTerrain,row);
   }
   Box("Rail",root,0,GroundY,1600,5,new Color(.55f,.53f,.46f));
   warning=Box("DangerZone",root,0,GroundY-5,63,18,new Color(1,.1f,.1f,.7f)).rectTransform;
   for(int i=0;i<3;i++){
    var texture=roof.CarBodies[i];var b=roof.CarBounds[i];float width=12*PixelsPerMeter,height=width*b.height/b.width;
    var car=Raw("PhysicalCar"+i,texture,HeadX-(i*13+12)*PixelsPerMeter,GroundY-height,width,height);
    car.uvRect=i==0?new Rect(b.xMax/texture.width,b.y/texture.height,-b.width/texture.width,b.height/texture.height):new Rect(b.x/texture.width,b.y/texture.height,b.width/texture.width,b.height/texture.height);
   }
   turret=Box("MeasuredSixMeterSocket",root,HeadX-6*PixelsPerMeter-4,GroundY-4f*PixelsPerMeter-4,8,8,Color.cyan).rectTransform;
   rock=Box("MeasuredObstacleVolume",root,0,0,63,63,new Color(.37f,.28f,.20f)).rectTransform;
   var hp=Box("ObstacleHp",rock,0,-12,63,5,Color.black);healthFill=Box("Remaining",hp.transform,0,0,63,5,Color.green).rectTransform;
   Text("Title",30,25,1520,50,"장애물 수치 시험 · 현재 협곡 · 독립 600m 시험선로",30);
   offscreen=Text("OffscreenWarning",700,90,850,50,"");
   status=Text("Status",40,560,1500,140,"",25);
   Text("Scope",40,825,1500,50,"로컬 시험 · 갈색 상자는 판정 체적 표시 · 게임용 장애물 원화/서버 등록 별도",20);
   Button("Low",40,"저속 14m/s",()=>owner.Throttle(7000));
   Button("Cruise",230,"순항 20m/s",()=>owner.Throttle(10000));
   Button("High",420,"고속 26m/s",()=>owner.Throttle(13000));
   Button("Pause",610,"정지 / 재개",owner.TogglePause);
   Button("Rate",800,"1 / 1.5 / 2배",owner.CycleSpeed);
   Button("Reenter",990,"노드 재입장",owner.Reenter);
   Refresh();
  }
  float X(double world)=>HeadX+(float)(world-controller.Model.Front)/1000*PixelsPerMeter;
  public void Refresh(){
   var m=controller.Model;float x=X(ObstacleTrialModel.TargetX);
   rock.anchoredPosition=new Vector2(x-31.5f,-GroundY+(float)m.CenterY/1000*PixelsPerMeter+31.5f);
   rock.gameObject.SetActive(m.Hp>0&&m.Phase!="BROKEN");
   warning.anchoredPosition=new Vector2(X(ObstacleTrialModel.ZoneStart),-GroundY+5);warning.gameObject.SetActive(!m.Finished&&m.Hp>0&&m.Phase!="BROKEN");
   healthFill.sizeDelta=new Vector2(63*m.Hp/100f,5);
   offscreen.text=x>1560?"오른쪽 낙하 예고 → "+Mathf.Max(0,(ObstacleTrialModel.TargetX-m.Front)/1000)+"m":x<0?"← 통과한 낙하 구역":"";
   while(bullets.Count<m.Shots.Count)bullets.Add(Box("ActualProjectile"+bullets.Count,root,0,0,7,4,Color.yellow).rectTransform);
   for(int i=0;i<bullets.Count;i++){bool live=i<m.Shots.Count&&m.Shots[i].Flying;bullets[i].gameObject.SetActive(live);if(live)bullets[i].anchoredPosition=new Vector2(X(m.Shots[i].X),-GroundY+(float)m.Shots[i].Y/1000*PixelsPerMeter);}
   status.text=$"{m.CaseId}  |  {m.Milliseconds/1000f:F2}s  |  {m.Phase}\n실속도 {m.Speed/1000f:F2} / 목표 {m.TargetSpeed/1000f:F0}m/s  ·  열차 HP {m.TrainHp}  ·  적격 보호막 {m.Shield}\n선두 {m.Front/1000f:F2}m / 후미 {m.Tail/1000f:F2}m  ·  {(controller.Paused?"정지":controller.Rate+"배속")}";
  }
 }
}
#endif
