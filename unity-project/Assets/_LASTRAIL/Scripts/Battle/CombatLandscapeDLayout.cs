using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
 // Display coordinates only. Never changes the original world geometry, slots or commands.
 internal sealed class CombatLandscapeDLayout {
  public static readonly Vector2 DesignSize=new Vector2(1600,900);
  readonly RectTransform root,actors;
  RectTransform Find(string name)=>root.Find(name) as RectTransform;
  void Move(string name,float x,float y,float scale=1){var r=Find(name);if(r==null)return;r.anchoredPosition=new Vector2(x,-y);r.localScale=Vector3.one*scale;}
  void Size(string name,float w,float h){var r=Find(name);if(r!=null)r.sizeDelta=new Vector2(w,h);}
  void Hide(params string[] names){foreach(var name in names){var r=Find(name);if(r!=null)r.gameObject.SetActive(false);}}
  public CombatLandscapeDLayout(RectTransform root,Canvas canvas){
   this.root=root;root.sizeDelta=DesignSize;
   var scaler=canvas.GetComponent<CanvasScaler>();scaler.referenceResolution=DesignSize;scaler.matchWidthOrHeight=.5f;
   Size("Base",1600,900);Size("Header",1600,66);Size("HeaderUnderlight",1600,1);
   Move("ExteriorViewport",0,0,1600f/720);Size("ExteriorViewport",720,275);
   var viewport=Find("ExteriorViewport");
   if(viewport.TryGetComponent<Mask>(out var portraitMask))portraitMask.enabled=false;
   if(viewport.TryGetComponent<CombatExteriorAperture>(out var portraitBezel))portraitBezel.enabled=false;
   var cabin=Find("CabinContent");actors=cabin?.Find("BattleActors") as RectTransform;
   if(actors!=null){actors.SetParent(root,false);actors.SetSiblingIndex(viewport.GetSiblingIndex()+1);actors.anchoredPosition=Vector2.zero;actors.sizeDelta=DesignSize;actors.localScale=Vector3.one;}
   Hide("CabinContent","CommandConsole","SecondConsoleShell","TrainEdge","TrainEdgeBottom","HullIntegrityHousing","HullNameplate","HullNameplateStencil","TrainPictogram");
   foreach(Transform child in root)if(child.name=="MonitorBezel")child.gameObject.SetActive(false);
   Move("ExteriorChassis",0,0);Size("ExteriorChassis",1600,610);
   Move("TrainPanel",16,610);Size("TrainPanel",422,44);Move("TrainLabel",23,616);Size("TrainLabel",85,30);
   Move("TrainHp",112,627);Size("TrainHp",220,12);Move("TrainHpText",337,616);Size("TrainHpText",98,30);
   Move("Title",16,10);Move("Wave",260,8);Move("WaveProgress",260,44);Move("UltimateAuto",1205,11);Move("Speed",1350,11);Move("Pause",1480,11);
   Move("BranchHousing",16,660);Move("ThrottleHousing",303,660);Move("BranchTitle",28,665);Move("RouteDescription",29,741);Move("BranchLever",30,779,.75f);Move("LeverHint",28,850);
   Move("ThrottleTitle",309,665);Move("TrainThrottle",343,707);Move("ThrottleDescription",309,862);
   for(int i=0;i<7;i++)Move("ThrottleTick"+i,394,714+i*22);
   var cards=root.Cast<Transform>().Where(t=>t.name.StartsWith("OperatorCard_")).ToArray();
   for(int i=0;i<cards.Length;i++){Move(cards[i].name,480+136*i,656);Move("General_"+cards[i].name.Substring("OperatorCard_".Length),484+136*i,808);}
   for(int i=0;i<5;i++)Move("skills"+i,480+i*102,665);
   Move("ultimate0",1184,665);
   for(int i=0;i<3;i++)Move("Command"+i,1345,660+i*76);
   Move("Notice",16,86);Size("Notice",400,28);
   var safe=canvas.GetComponent<CombatSafeArea>()??canvas.gameObject.AddComponent<CombatSafeArea>();safe.Bind(root,canvas);
   ApplyOverlays();
  }
  public void PlaceActor(RectTransform actor,int slot,bool enemy,Vector2 transientOffset){
   if(actors==null)return;
   actor.anchoredPosition=new Vector2(enemy?1080+slot%3*110:140+slot*105,-245-(enemy?(slot/3)*70:0))+transientOffset;
   actor.localScale=Vector3.one*.7f;
  }
  public void ApplyOverlays(){Move("RouteMap",20,855,.52f);Move("OperatorInformation",480,446);Move("OperatorEquipmentDetail",480,140);Move("LeaderCutin",680,90);Move("SkipPresentation",982,189);}
 }
}
