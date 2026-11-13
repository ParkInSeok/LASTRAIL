using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle {
 public sealed partial class ApprovedCombatView {
  RectTransform ordinaryObstacleLayer,ordinaryObstacleBody;
  readonly List<Image> ordinaryObstacleEdges=new List<Image>();
  readonly List<Image> ordinaryObstacleDanger=new List<Image>();
  readonly Dictionary<string,Image> ordinaryObstacleShots=new Dictionary<string,Image>(StringComparer.Ordinal);
  CombatMeter ordinaryObstacleHp,ordinaryObstacleOverviewHp;
  RectTransform ordinaryObstacleOverview;
  TMP_Text ordinaryObstacleOverviewStatus,ordinaryObstacleOverviewHealth;
  Image ordinaryObstacleOverviewBox,ordinaryObstacleOverviewTrain,ordinaryObstacleOverviewZone;
  readonly Dictionary<string,Image> ordinaryObstacleOverviewShots=new Dictionary<string,Image>(StringComparer.Ordinal);

  static readonly int[,] ObstacleEdgePairs={{0,1},{0,2},{0,4},{1,3},{1,5},{2,3},{2,6},{3,7},{4,5},{4,6},{5,7},{6,7}};
  Vector2 ObstacleDisplayPoint(double x,double y,double z,double front){
   var uv=ordinaryRoof.ProjectRelativeToFront(x-front,y,z);var surface=ordinaryRoofSurface.rectTransform;
   var world=surface.TransformPoint(new Vector2(uv.x*surface.rect.width,-(1-uv.y)*surface.rect.height));
   return ordinaryObstacleLayer.InverseTransformPoint(world);
  }
  void EnsureOrdinaryObstacleDisplay(){
   if(ordinaryObstacleLayer!=null)return;
   ordinaryObstacleLayer=Rect("OrdinaryLocalJudgementGeometry",root,0,0,root.rect.width,root.rect.height);
   ordinaryObstacleLayer.SetSiblingIndex(actorLayer.GetSiblingIndex()+1);
   ordinaryObstacleBody=Rect("FallingBox",ordinaryObstacleLayer,0,0,0,0);
   for(int i=0;i<4;i++){var line=Box("LandingDanger"+i,ordinaryObstacleLayer,0,0,1,3,new Color(1,.12f,.08f,.8f));line.rectTransform.pivot=new Vector2(0,.5f);ordinaryObstacleDanger.Add(line);}
   for(int i=0;i<12;i++){
    var edge=Box("Edge"+i,ordinaryObstacleBody,0,0,1,2,new Color(1,.61f,.25f,.95f));edge.rectTransform.pivot=new Vector2(0,.5f);ordinaryObstacleEdges.Add(edge);
   }
   ordinaryObstacleHp=Meter("ObstacleHp",ordinaryObstacleBody,0,0,126,10,new Color(1,.55f,.23f));

  }
  void RefreshOrdinaryObstacle(JObject state){
   var observed=state["ordinaryObstacleExecution"] as JObject;
   RefreshOrdinaryObstacleOverview(state,observed);
   bool visible=ordinaryRoof!=null&&ordinaryRoofSurface!=null&&observed!=null&&(string)state["ordinaryObstacle"]?["phase"]=="OBSTACLE";
   if(!visible){
    if(ordinaryObstacleLayer!=null)ordinaryObstacleLayer.gameObject.SetActive(false);
    foreach(var shot in ordinaryObstacleShots.Values)if(shot!=null)DisposeVisual(shot.gameObject);ordinaryObstacleShots.Clear();return;
   }
   EnsureOrdinaryObstacleDisplay();ordinaryObstacleLayer.gameObject.SetActive(true);
   if(observed["centerMm"] is not JArray center||center.Count!=3||observed["sizeMm"] is not JArray size||size.Count!=3||observed["shots"] is not JArray shots)
    throw new InvalidOperationException("Original obstacle display geometry required.");
   double front=(double)state["ordinaryRail"]["frontArcMm"];int hp=(int)observed["hp"],maximum=(int)observed["maxHp"];
   if(hp<0||maximum<=0||hp>maximum||size.Any(v=>(double)v<=0))throw new InvalidOperationException("Original obstacle display state differs.");
   var ground=new Vector2[4];for(int i=0;i<4;i++){int corner=new[]{0,1,3,2}[i];ground[i]=ObstacleDisplayPoint((double)center[0]+((corner&1)==0?-.5:.5)*(double)size[0],0,((corner&2)==0?-.5:.5)*(double)size[2],front);}
   bool danger=hp>0&&(string)observed["phase"]!="BROKEN"&&ground.Max(p=>p.x)>=0&&ground.Min(p=>p.x)<=root.rect.width;
   for(int i=0;i<4;i++){var line=ordinaryObstacleDanger[i].rectTransform;line.gameObject.SetActive(danger);var delta=ground[(i+1)%4]-ground[i];line.anchoredPosition=ground[i];line.sizeDelta=new Vector2(delta.magnitude,3);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);}
   var corners=new Vector2[8];for(int i=0;i<8;i++)corners[i]=ObstacleDisplayPoint((double)center[0]+((i&1)==0?-.5:.5)*(double)size[0],(double)center[1]+((i&2)==0?-.5:.5)*(double)size[1],(double)center[2]+((i&4)==0?-.5:.5)*(double)size[2],front);
   bool inDisplay=corners.Max(p=>p.x)>=0&&corners.Min(p=>p.x)<=root.rect.width&&corners.Max(p=>p.y)>=-root.rect.height&&corners.Min(p=>p.y)<=0;
   ordinaryObstacleBody.gameObject.SetActive(hp>0&&(string)observed["phase"]!="BROKEN"&&inDisplay);
   if(hp>0&&(string)observed["phase"]!="BROKEN"&&inDisplay){
    for(int i=0;i<12;i++){
     var a=corners[ObstacleEdgePairs[i,0]];var delta=corners[ObstacleEdgePairs[i,1]]-a;var edge=ordinaryObstacleEdges[i].rectTransform;
     edge.anchoredPosition=a;edge.sizeDelta=new Vector2(delta.magnitude,2);edge.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
    }
    float mid=corners.Average(p=>p.x),top=corners.Max(p=>p.y);ordinaryObstacleHp.rectTransform.anchoredPosition=new Vector2(mid-63,top+16);ordinaryObstacleHp.SetHealth(hp,maximum,0);
   }
   var live=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject shot in shots){
    if((bool?)shot["flying"]!=true)continue;string id=(string)shot["id"];if(string.IsNullOrWhiteSpace(id)||!live.Add(id))throw new InvalidOperationException("Unique original obstacle projectile required.");
    if(!ordinaryObstacleShots.TryGetValue(id,out var marker)){marker=Box("Projectile_"+id,ordinaryObstacleLayer,0,0,7,3,new Color(1,.93f,.61f));marker.rectTransform.pivot=new Vector2(.5f,.5f);ordinaryObstacleShots.Add(id,marker);}
    var point=ObstacleDisplayPoint((double)shot["x"],(double)shot["y"],0,front);marker.rectTransform.anchoredPosition=point;
    marker.gameObject.SetActive(point.x>=0&&point.x<=root.rect.width&&point.y>=-root.rect.height&&point.y<=0);
   }
   foreach(string id in ordinaryObstacleShots.Keys.Where(id=>!live.Contains(id)).ToArray()){DisposeVisual(ordinaryObstacleShots[id].gameObject);ordinaryObstacleShots.Remove(id);}
  }
  // A labelled metric overview, not an edge-clamped replacement for the world camera.
  // Both axes use the same scale. All positions are borrowed from the current execution HUD.
  void RefreshOrdinaryObstacleOverview(JObject state,JObject observed){
   if(ordinaryRoof==null||ordinaryRoofSurface==null||observed==null||state["ordinaryRail"] is not JObject rail){if(ordinaryObstacleOverview!=null)ordinaryObstacleOverview.gameObject.SetActive(false);return;}
   if(ordinaryObstacleOverview==null){
    ordinaryObstacleOverview=Rect("LocalObstacleMetricOverview",root,500,85,600,145);
    ordinaryObstacleOverview.gameObject.AddComponent<RectMask2D>();
    Box("OverviewPlate",ordinaryObstacleOverview,0,0,600,145,new Color(.025f,.035f,.045f,.91f));
    Text("OverviewTitle",ordinaryObstacleOverview,"구간 보기 · 로컬 판정체",14,8,360,24,18).color=new Color(1,.77f,.42f);
    ordinaryObstacleOverviewStatus=Text("OverviewStatus",ordinaryObstacleOverview,"",14,34,575,24,17);
    ordinaryObstacleOverviewHealth=Text("OverviewHealth",ordinaryObstacleOverview,"",395,8,190,24,18);
    ordinaryObstacleOverviewHp=Meter("OverviewHp",ordinaryObstacleOverview,400,60,180,8,new Color(1,.55f,.23f));
    Box("OverviewRail",ordinaryObstacleOverview,20,118,560,2,new Color(.5f,.58f,.62f,.8f));
    ordinaryObstacleOverviewZone=Box("MeasuredLandingZone",ordinaryObstacleOverview,0,116,1,6,new Color(1,.12f,.08f));
    ordinaryObstacleOverviewTrain=Box("CurrentTrainSpan",ordinaryObstacleOverview,0,112,1,6,new Color(.3f,.92f,1));
    ordinaryObstacleOverviewBox=Box("MeasuredObstacle",ordinaryObstacleOverview,0,0,1,1,new Color(1,.62f,.2f));
    Text("OverviewLegend",ordinaryObstacleOverview,"청록: 열차 범위   빨강: 낙하 지점   주황: 장애물",14,123,575,18,13);
   }
   ordinaryObstacleOverview.gameObject.SetActive(true);
   var center=(JArray)observed["centerMm"];var size=(JArray)observed["sizeMm"];var shots=(JArray)observed["shots"];
   double target=(double)center[0],origin=target-350000;const double scale=560d/410000;
   float X(double mm)=>(float)(20+(mm-origin)*scale);
   float Y(double mm)=>(float)(118-mm*scale);
   void Place(Image image,double x,double y,double width,double height){image.rectTransform.anchoredPosition=new Vector2(X(x),-Y(y));image.rectTransform.sizeDelta=new Vector2((float)(width*scale),(float)(height*scale));}
   int hp=(int)observed["hp"],maximum=(int)observed["maxHp"];double front=(double)rail["frontArcMm"],tail=(double)rail["tailArcMm"];
   string phase=(string)observed["phase"],sequence=(string)state["ordinaryObstacle"]?["phase"];
   string progress=sequence=="COMBAT"?"후미 통과 · 원 전투 진행":sequence=="DEFEAT"?"열차 정지":hp==0?"파괴 완료 · 후미 통과 대기":phase=="BROKEN"?"충돌 처리 · 후미 통과 대기":phase=="FALLING"?"낙하 중":phase=="LANDED"?"지면 도착":"낙하 예고";
   ordinaryObstacleOverviewHealth.text=$"장애물 HP  {hp} / {maximum}";ordinaryObstacleOverviewHp.SetHealth(hp,maximum,0);
   string distance=target>=front?$"전방 {(target-front)/1000:0.0} m":$"후방 {(front-target)/1000:0.0} m";
   var uv=ordinaryRoof.ProjectRelativeToFront(target-front,(double)center[1],(double)center[2]);var surface=ordinaryRoofSurface.rectTransform;
   var mainPoint=root.InverseTransformPoint(surface.TransformPoint(new Vector2(uv.x*surface.rect.width,-(1-uv.y)*surface.rect.height)));
   bool offscreen=!root.rect.Contains(new Vector2(mainPoint.x,mainPoint.y));
   ordinaryObstacleOverviewStatus.text=progress+"  ·  "+distance+(hp>0&&offscreen?" · 화면 밖 대상":"");
   Place(ordinaryObstacleOverviewTrain,tail,3200,front-tail,3200);
   ordinaryObstacleOverviewTrain.gameObject.SetActive(X(front)>=20&&X(tail)<=580);
   Place(ordinaryObstacleOverviewZone,target-(double)size[0]/2,0,(double)size[0],0);ordinaryObstacleOverviewZone.rectTransform.sizeDelta=new Vector2((float)((double)size[0]*scale),6);
   ordinaryObstacleOverviewZone.gameObject.SetActive(sequence=="OBSTACLE"&&hp>0&&phase!="BROKEN");
   Place(ordinaryObstacleOverviewBox,target-(double)size[0]/2,(double)center[1]+(double)size[1]/2,(double)size[0],(double)size[1]);
   ordinaryObstacleOverviewBox.gameObject.SetActive(hp>0&&phase!="BROKEN"&&sequence=="OBSTACLE");
   var active=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject shot in shots){if((bool?)shot["flying"]!=true)continue;string id=(string)shot["id"];active.Add(id);
    if(!ordinaryObstacleOverviewShots.TryGetValue(id,out var marker)){marker=Box("OverviewShot_"+id,ordinaryObstacleOverview,0,0,4,4,new Color(1,1,.65f));marker.rectTransform.pivot=new Vector2(.5f,.5f);ordinaryObstacleOverviewShots.Add(id,marker);}
    float x=X((double)shot["x"]),y=Y((double)shot["y"]);marker.rectTransform.anchoredPosition=new Vector2(x,-y);marker.gameObject.SetActive(x>=20&&x<=580&&y>=70&&y<=120);
   }
   foreach(string id in ordinaryObstacleOverviewShots.Keys.Where(id=>!active.Contains(id)).ToArray()){DisposeVisual(ordinaryObstacleOverviewShots[id].gameObject);ordinaryObstacleOverviewShots.Remove(id);}
  }
 }
}
