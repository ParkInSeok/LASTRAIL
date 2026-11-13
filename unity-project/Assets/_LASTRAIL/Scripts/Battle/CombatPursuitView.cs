using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 sealed class Scenery {public RectTransform Rect;public float X,Y,Speed,Period;public bool Roadside;}
 readonly List<Scenery> pursuitScenery=new List<Scenery>();CombatPursuitRoad pursuitRoad;
 CombatWheelContact contactShadow,contactDust;
 bool PursuitActive=>pursuitRoad!=null&&pursuitRoad.gameObject.activeSelf;
#if UNITY_EDITOR
 public JObject CapturePursuitProjection(){
  var props=new JArray();foreach(var p in pursuitScenery)props.Add(new JObject{["id"]=p.Rect.name,["x"]=p.Rect.anchoredPosition.x,["y"]=-p.Rect.anchoredPosition.y,["speed"]=p.Speed,["baseX"]=p.X,["period"]=p.Period});
  var contacts=new JArray();foreach(var actor in (JArray)driver.HudState["actors"]){if(!units.TryGetValue((string)actor["id"],out var u)||!u.Vehicle||!u.Root.gameObject.activeSelf)continue;contacts.Add(new JObject{["id"]=(string)actor["id"],["slot"]=(int)actor["slot"],["x"]=u.Root.anchoredPosition.x+85*u.Root.localScale.x,["y"]=u.ProjectedGround,["scale"]=u.Root.localScale.x});}
  return new JObject{["active"]=PursuitActive,["props"]=props,["contacts"]=contacts};
 }
#endif
 void AddPursuitWorld(){
  if(assets.PursuitRoad==null||assets.PursuitLayout==null)return;
  var layout=JObject.Parse(assets.PursuitLayout.text);Transform firstMarker=null;
  // Each alpha-cutout repeats only after leaving the clipped view. Source atlas is never mirrored.
  foreach(var layer in (JArray)layout["decorations"]){
   bool near=(string)layer["kind"]=="roadside";var texture=near?assets.PursuitMarkers:assets.PursuitRidges;if(texture==null)continue;
   var uv=(JArray)layer["uv"];float width=(float)layer["width"],height=(float)layer["height"],period=(float)layer["period"];
   for(int copy=-1;copy<=1;copy++){
    var image=Raw("Pursuit_"+(string)layer["id"]+"_"+copy,exteriorViewport,texture,0,0,width,height);image.uvRect=new Rect((float)uv[0],(float)uv[1],(float)uv[2],(float)uv[3]);image.material=assets.PursuitCutout;
    if(near&&assets.ExteriorBezel!=null)image.color=new Color(.64f,.76f,.88f);
    if(near&&firstMarker==null)firstMarker=image.transform;
    pursuitScenery.Add(new Scenery{Rect=image.rectTransform,X=(float)layer["x"]+period*copy,Y=(float)layer["y"],Speed=(float)layer["speed"],Period=period,Roadside=near});
   }
  }
  pursuitRoad=Rect("PursuitRoad",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatPursuitRoad>();pursuitRoad.Texture=assets.PursuitRoad;pursuitRoad.raycastTarget=false;
  if(firstMarker!=null)pursuitRoad.transform.SetSiblingIndex(firstMarker.GetSiblingIndex());
  var roadUv=(JArray)layout["roadUv"];if(roadUv!=null)pursuitRoad.TextureRegion=new Rect((float)roadUv[0],(float)roadUv[1],(float)roadUv[2],(float)roadUv[3]);
 }
 void RefreshPursuit(float seconds){
  if(pursuitRoad!=null)pursuitRoad.SetTime(seconds);
  foreach(var piece in pursuitScenery){float x=piece.X-Mathf.Repeat(seconds*piece.Speed,piece.Period);float y=piece.Roadside?CombatPursuitLayout.RoadY(x,seconds,1)+piece.Y:piece.Y;piece.Rect.anchoredPosition=new Vector2(x,-y);}
 }
 void RefreshPursuitVisibility(bool visible){if(pursuitRoad!=null)pursuitRoad.gameObject.SetActive(visible);foreach(var piece in pursuitScenery)piece.Rect.gameObject.SetActive(visible);}
 void AddContactShadow(){contactShadow=Rect("WheelContactShadow",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatWheelContact>();contactShadow.raycastTarget=false;}
 void AddContactDust(){contactDust=Rect("WheelContactDust",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatWheelContact>();contactDust.Dust=true;contactDust.raycastTarget=false;}
 void RefreshContacts(float seconds,float groundSpeed){
  var contacts=new List<Vector2>();foreach(var disc in wheelDiscs)if(disc.gameObject.activeSelf){var r=disc.rectTransform;contacts.Add(exteriorViewport.InverseTransformPoint(r.TransformPoint(new Vector2(r.rect.center.x,r.rect.yMin))));}
  var points=contacts.ToArray();contactShadow.SetContacts(seconds,groundSpeed,points);contactDust.SetContacts(seconds,groundSpeed,points);
 }
}
}
