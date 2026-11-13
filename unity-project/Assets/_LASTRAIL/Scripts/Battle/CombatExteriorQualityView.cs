using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 CombatWheelContact consistDust;
 CombatLocomotiveExhaust locomotiveExhaust;
 void ApplyExteriorQuality(){
  var backing=Box("ExteriorChassis",root,0,66,720,273,new Color(.006f,.009f,.011f));backing.transform.SetSiblingIndex(exteriorViewport.GetSiblingIndex());
  var aperture=exteriorViewport.gameObject.AddComponent<CombatExteriorAperture>();aperture.raycastTarget=false;
  exteriorViewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
  if(assets.ExteriorBezel!=null){
   var frame=root.GetComponentsInChildren<Image>().First(i=>i.name=="MonitorBezel"&&Mathf.Abs(i.rectTransform.anchoredPosition.y+66)<.1f);
   var t=assets.ExteriorBezel;float sx=t.width/2035f,sy=t.height/773f;
   frame.sprite=OwnPresentationSprite(Sprite.Create(t,new Rect(50*sx,70*sy,1934*sx,635*sy),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(62*sx,62*sy,62*sx,62*sy)));frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=3.5f*sx;
  }
 }
 void AddConsistAtmosphere(){
  locomotiveExhaust=Rect("NormalExhaust",consist[0],0,0,consist[0].sizeDelta.x,consist[0].sizeDelta.y).gameObject.AddComponent<CombatLocomotiveExhaust>();locomotiveExhaust.raycastTarget=false;
  consistDust=Rect("ConsistWheelDust",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatWheelContact>();consistDust.Dust=true;consistDust.raycastTarget=false;
  contactDust.gameObject.SetActive(false);contactShadow.gameObject.SetActive(false);
 }
 void RefreshConsistAtmosphere(JObject state,float travelTime){
  var rail=(JObject)state["railJourney"];bool moving=(string)rail["phase"]!="Selecting"&&(string)state["outcome"]=="Running";
  float throttle=(int)rail["throttleBps"]/10000f;
  locomotiveExhaust.SetState((float)state["motionSeconds"],moving?throttle:.18f);
  var points=consist.SelectMany(c=>new[]{.22f,.76f}.Select(x=>(Vector2)exteriorViewport.InverseTransformPoint(c.TransformPoint(new Vector2(c.rect.xMin+c.rect.width*x,c.rect.yMin+2))))).ToArray();
  consistDust.SetContacts(travelTime,moving?CombatTravelGround.Speed*throttle:0,points);
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatExteriorAperture:MaskableGraphic,ICanvasRaycastFilter {
 public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera){if(!isActiveAndEnabled)return true;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out var p))return false;var r=rectTransform.rect;float left=r.xMin+17,right=r.xMax-17,bottom=r.yMin+15,top=r.yMax-15;if(p.x<left||p.x>right||p.y<bottom||p.y>top)return false;return Mathf.Min(p.x-left,right-p.x)+Mathf.Min(p.y-bottom,top-p.y)>=7;}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();var r=rectTransform.rect;float l=r.xMin+17,b=r.yMin+15,h=r.yMax-15,w=r.xMax-17,c=7;var p=new[]{new Vector2(l+c,b),new Vector2(w-c,b),new Vector2(w,b+c),new Vector2(w,h-c),new Vector2(w-c,h),new Vector2(l+c,h),new Vector2(l,h-c),new Vector2(l,b+c)};m.AddVert(new Vector2((l+w)/2,(b+h)/2),Color.white,Vector2.zero);foreach(var v in p)m.AddVert(v,Color.white,Vector2.zero);for(int i=0;i<8;i++)m.AddTriangle(0,i+1,(i+1)%8+1);}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatLocomotiveExhaust:MaskableGraphic {
 float clock,power;
 public void SetState(float seconds,float effort){clock=seconds;power=Mathf.Clamp(effort,0,1.3f);SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();var r=rectTransform.rect;var origin=new Vector2(r.xMin+r.width*.41f,r.yMax-3);for(int p=0;p<9;p++){float age=Mathf.Repeat(clock*.48f+p/9f,1),a=Mathf.Sin(age*Mathf.PI)*(.24f+power*.22f);var point=origin+new Vector2(-age*(34+power*15),age*31);float radius=2+age*13;int first=m.currentVertCount;m.AddVert(point,new Color(.46f,.48f,.48f,a),Vector2.zero);for(int i=0;i<=16;i++){float angle=i*Mathf.PI*2/16;m.AddVert(point+new Vector2(Mathf.Cos(angle)*radius*1.4f,Mathf.Sin(angle)*radius),new Color(.55f,.57f,.56f,0),Vector2.zero);}for(int i=0;i<16;i++)m.AddTriangle(first,first+i+1,first+i+2);}}
}
}
