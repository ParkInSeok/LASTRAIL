using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle {
// Display-clock sampling of the approved web FX. This view never schedules or applies healing/damage.
public sealed class CombatSeraCrystalView : MonoBehaviour {
 public struct State {
  public Rect Actor;public Vector2 Center,Radius,Target;public double Phase;public float Gate,Energy,Core,Heal,Beam,Height;
  public bool Ultimate,Idle,Targeted;public float Depth(int i)=>(float)Math.Sin(Phase+i*Math.PI*2/3);
  public Vector2 Point(int i){double a=Phase+i*Math.PI*2/3;return Center+new Vector2(Radius.x*(float)Math.Cos(a),Radius.y*(float)Math.Sin(a));}
  public float Occlusion(int i)=>Idle?(Depth(i)<0?1:0):(1-Gate)*Smooth(((Ultimate?.12f:.15f)-Depth(i))/(Ultimate?.24f:.3f));
 }
 static float Smooth(double x){float u=Mathf.Clamp01((float)x);return u*u*(3-2*u);}
 static float Ease(double x){float u=Mathf.Clamp01((float)x);return u*u*u*(u*(u*6-15)+10);}
 static double Integral(double x)=>x<=0?0:x>=1?x-.5:x*x*x-.5*x*x*x*x;
 static double Spin(double age)=>1260*Integral((age-720)/1260)-900*Integral((age-2160)/900);
 public static State Sample(bool right,string motion,double age,double clock,Vector2? target){
  if(double.IsNaN(age)||double.IsInfinity(age)||double.IsNaN(clock)||double.IsInfinity(clock))throw new ArgumentOutOfRangeException(nameof(age));
  var s=new State{Actor=new Rect(0,0,1024,1536),Center=new Vector2(right?474:550,700),Radius=new Vector2(350,135),Phase=clock/6480*Math.PI*2-.3,Height=190,Idle=true};
  if(motion=="ultimate"){
   s.Idle=false;s.Ultimate=true;s.Actor=right?new Rect(100,40,408,612):new Rect(240,40,408,600);
   double pose=age<=360?age:age<1260?360:age-900;
   s.Energy=Smooth((pose-360)/720)*(1-Smooth((pose-1260)/720));s.Gate=Smooth(age/360)*(1-Smooth((age-2520)/540));
   Vector2 home=right?new Vector2(100+474*408f/1024,40+700*408f/1024):new Vector2(240+650f/3,40+844f/3);
   Vector2 cast=right?new Vector2(350+105*s.Energy,310-60*s.Energy):new Vector2(382-78*s.Energy,302-42*s.Energy);
   s.Center=Vector2.Lerp(home,cast,s.Gate);
   var homeRadius=right?new Vector2(350*408f/1024,135*408f/1024):new Vector2(350f/3,45);
   float radius=right?85+18*s.Energy:76+14*s.Energy;s.Radius=Vector2.Lerp(homeRadius,new Vector2(radius,radius),s.Gate);
   s.Phase=clock/4320*Math.PI*2-.3+Spin(age)*Math.PI*2/900;
   s.Core=Smooth((age-1440)/540)*(1-Smooth((age-2520)/540));
   float heal=Mathf.Clamp01((float)((age-1980)/900));s.Heal=heal>0&&heal<1?heal:0;s.Height=right?190*408f/1024:190f/3;
  }else if(motion=="active"){
   s.Idle=false;s.Actor=right?new Rect(120,12,246,369):new Rect(463,12,246,369);
   s.Center=new Vector2(s.Actor.x+(right?474:550)*246f/1024,12+700*369f/1536);s.Radius=new Vector2(350*246f/1024,135*369f/1536);s.Height=190*369f/1536;
   if(target.HasValue&&age>=540&&age<2160){
    s.Targeted=true;s.Target=target.Value;
    double orbit=age<900?0:age>=1440?1:Ease((age-900)/540);
    double arrivalPhase=(clock-(age-540))/6480*Math.PI*2-.3+Math.PI*2*orbit;
    float blend=age>=900&&age<1440?1:age<900?Ease((age-540)/360):1-Ease((age-1440)/720);
    double homePhase=s.Phase+(age>=1440?Math.PI*2:0);s.Phase=homePhase+(arrivalPhase-homePhase)*blend;
    s.Center=Vector2.Lerp(s.Center,target.Value,blend);s.Radius=Vector2.Lerp(s.Radius,new Vector2(68,28),blend);
    if(age<900||age>=1440){double u=age<900?(age-540)/360:(age-1440)/720;s.Center.y-=24*(float)Math.Pow(Math.Sin(Math.PI*u),2);}
    if(age>=900&&age<1440)s.Heal=(float)((age-900)/540);
   }
  }else if(motion=="basic"&&right){
   s.Idle=false;s.Actor=new Rect(100,20,300,450);s.Center=new Vector2(239,225);s.Radius=new Vector2(103,40);s.Height=190*450f/1536;
   if(target.HasValue){s.Targeted=true;s.Target=target.Value;s.Gate=Smooth((age-180)/540)*(1-Smooth((age-1440)/720));s.Center=Vector2.Lerp(s.Center,new Vector2(440,145),s.Gate);s.Radius=Vector2.Lerp(s.Radius,new Vector2(32,38),s.Gate);s.Beam=age>=900&&age<1220?Smooth((age-900)/20)*(1-Smooth((age-1100)/120)):0;}
  }
  // Left-basic emitter source is not approved here; its existing consumer owns the attack FX.
  return s;
 }
 Texture2D crystal,idleCrystal;Shader maskShader;readonly RawImage[] gems=new RawImage[3];readonly Material[] masks=new Material[3];
 SeraCrystalLines lines;RectTransform actor,target;RawImage actorImage,targetImage;State state;bool bound;
 public bool Visible{get;private set;}
 public void Bind(Texture2D castingCrystal,Texture2D idleGem,Shader occlusionShader){
  if(castingCrystal==null||idleGem==null||occlusionShader==null)throw new ArgumentException("Approved Sera crystal textures and mask shader are required.");
  if(bound)throw new InvalidOperationException("Sera FX already bound.");crystal=castingCrystal;idleCrystal=idleGem;maskShader=occlusionShader;
  lines=new GameObject("SeraOrbitAndHealing",typeof(RectTransform)).AddComponent<SeraCrystalLines>();lines.transform.SetParent(transform,false);lines.raycastTarget=false;
  for(int i=0;i<3;i++){var go=new GameObject("SeraCrystal_"+i,typeof(RectTransform));go.transform.SetParent(transform,false);gems[i]=go.AddComponent<RawImage>();gems[i].raycastTarget=false;masks[i]=new Material(maskShader);gems[i].material=masks[i];}
  bound=true;Hide();
 }
 public void Render(bool visible,bool facesRight,string motion,double elapsedMs,double idleMs,RectTransform actorArt,RectTransform targetArt){
  if(!visible||!isActiveAndEnabled||actorArt==null||!actorArt.gameObject.activeInHierarchy){Hide();return;}
  if(!bound)throw new InvalidOperationException("Bind approved Sera FX resources before rendering.");
  actor=actorArt;actorImage=actor.GetComponent<RawImage>();target=targetArt!=null&&targetArt.gameObject.activeInHierarchy?targetArt:null;targetImage=target==null?null:target.GetComponent<RawImage>();
  if(actorImage==null||actorImage.texture==null||!actorImage.enabled||actor.rect.width<=0||actor.rect.height<=0){Hide();return;}
  state=Sample(facesRight,motion,elapsedMs,idleMs,null);
  Vector2? targetPoint=null;
  if(targetImage!=null&&targetImage.texture!=null&&targetImage.enabled&&target.rect.width>0&&target.rect.height>0){
   // Source recipient anchor is (135,132) in its 270x267 preview art rectangle.
   var tr=target.rect;var local=new Vector3(tr.xMin+tr.width*.5f,tr.yMax-tr.height*(132f/267),0);
   Vector3 inActor=actor.InverseTransformPoint(target.TransformPoint(local));var ar=actor.rect;
   targetPoint=new Vector2(state.Actor.x+(inActor.x-ar.xMin)/ar.width*state.Actor.width,state.Actor.y+(ar.yMax-inActor.y)/ar.height*state.Actor.height);
  }
  state=Sample(facesRight,motion,elapsedMs,idleMs,targetPoint);Visible=true;lines.gameObject.SetActive(true);lines.Clear();
  if(state.Ultimate)DrawUltimate();else if(motion=="active"&&state.Targeted&&elapsedMs>=900&&elapsedMs<1440)DrawHeal((float)((elapsedMs-900)/540));
  for(int i=0;i<3;i++){
   var gem=gems[i];gem.gameObject.SetActive(true);gem.texture=state.Idle?idleCrystal:crystal;
   gem.uvRect=state.Idle?new Rect(240f/idleCrystal.width,1-1389f/idleCrystal.height,542f/idleCrystal.width,1300f/idleCrystal.height):new Rect(0,0,1,1);
   Vector2 p=state.Point(i);float h=state.Height*(1+.12f*(state.Ultimate||motion=="basic"?1-state.Gate:1)*state.Depth(i));float w=h*(state.Idle?542f/1300:.42f);
   var r=gem.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.localPosition=Point(p);r.sizeDelta=new Vector2(Vector2.Distance(Point(p),Point(p+Vector2.right*w)),Vector2.Distance(Point(p),Point(p+Vector2.up*h)));r.localRotation=Quaternion.identity;
   if(motion=="basic"&&facesRight&&state.Targeted){float a=Mathf.Atan2(state.Target.y-p.y,state.Target.x-p.x);r.localRotation=Quaternion.Euler(0,0,-(a+Mathf.PI/2)*Mathf.Rad2Deg*state.Gate);if(state.Beam>0){var start=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*h*.45f;Stroke(start,state.Target,3,new Color(.404f,.847f,.671f,state.Beam));Stroke(start,state.Target,1,new Color(.937f,1,.949f,state.Beam));}}
   SetMask(masks[i],"_Actor",actor,actorImage);SetMask(masks[i],"_Target",target,targetImage);masks[i].SetFloat("_Occlusion",state.Occlusion(i));
  }
  lines.Finish();
 }
 Vector2 Point(Vector2 p){var r=actor.rect;return ((RectTransform)transform).InverseTransformPoint(actor.TransformPoint(new Vector3(r.xMin+(p.x-state.Actor.x)/state.Actor.width*r.width,r.yMax-(p.y-state.Actor.y)/state.Actor.height*r.height,0)));}
 void Stroke(Vector2 a,Vector2 b,float width,Color color)=>lines.Line(Point(a),Point(b),Vector2.Distance(Point(a),Point(a+Vector2.up*width)),color);
 void Ellipse(Vector2 c,Vector2 radii,float width,Color color,float from=0,float to=Mathf.PI*2){int count=Mathf.Max(3,Mathf.CeilToInt((to-from)/(.09817477f)));var a=c+new Vector2(radii.x*Mathf.Cos(from),radii.y*Mathf.Sin(from));for(int i=1;i<=count;i++){float t=Mathf.Lerp(from,to,(float)i/count);var b=c+new Vector2(radii.x*Mathf.Cos(t),radii.y*Mathf.Sin(t));Stroke(a,b,width,color);a=b;}}
 void Glow(Vector2 center,float radius,Color color){var c=Point(center);lines.Glow(c,Vector2.Distance(c,Point(center+Vector2.right*radius)),color);}
 void DrawUltimate(){
  if(state.Core>0){var p=Point(state.Center);lines.CoreGlow(p,Vector2.Distance(p,Point(state.Center+Vector2.right*48)),state.Core);}
  if(state.Heal>0){float alpha=Mathf.Sin(Mathf.PI*state.Heal)*.6f,r=35+310*state.Heal;Ellipse(state.Center,new Vector2(r,r*.75f),3,new Color(.596f,1,.816f,alpha));for(int i=0;i<9;i++){var p=new Vector2(85+i*66,560-210*state.Heal+i%3*20);Stroke(p-Vector2.up*9,p+Vector2.up*9,4,new Color(.765f,1,.855f,alpha));Stroke(p-Vector2.right*9,p+Vector2.right*9,4,new Color(.765f,1,.855f,alpha));}}
  float strength=(.48f+.4f*state.Energy)*state.Gate;Ellipse(state.Center,state.Radius,5,new Color(.31f,.827f,.69f,strength*.25f));Ellipse(state.Center,state.Radius,1.1f,new Color(.694f,1,.878f,strength));Ellipse(state.Center,state.Radius+Vector2.one*4,.55f,new Color(.451f,.827f,.718f,strength*.6f));
  for(int i=0;i<3;i++){float a=(float)(state.Phase+i*Math.PI*2/3);Ellipse(state.Center,state.Radius,2,new Color(.847f,1,.941f,state.Gate),a-.12f,a+.12f);}
 }
 void DrawHeal(float u){float alpha=Mathf.Pow(Mathf.Sin(Mathf.PI*u),.7f);foreach(int side in new[]{-1,1}){Vector2 previous=default;for(int k=0;k<=22;k++){float v=k/22f;var p=state.Target+new Vector2(side*(39+8*Mathf.Sin(v*5+u*3)),51-100*v-14*u);if(k>0){Stroke(previous,p,4,new Color(.322f,.812f,.631f,alpha));Stroke(previous,p,1.4f,new Color(.929f,1,.949f,alpha));}previous=p;}Glow(state.Target+new Vector2(side*40,41-u*105),15,new Color(.902f,1,.894f,.9f*alpha));}}
 static void SetMask(Material m,string prefix,RectTransform rect,RawImage image){
  bool valid=rect!=null&&image!=null&&image.enabled&&image.texture!=null;m.SetFloat(prefix+"Enabled",valid?1:0);if(!valid)return;
  m.SetTexture(prefix+"Tex",image.texture);m.SetMatrix(prefix+"Inverse",rect.worldToLocalMatrix);var r=rect.rect;m.SetVector(prefix+"Rect",new Vector4(r.xMin,r.yMin,r.width,r.height));var uv=image.uvRect;m.SetVector(prefix+"Uv",new Vector4(uv.x,uv.y,uv.width,uv.height));m.SetFloat(prefix+"Alpha",image.color.a);
 }
 void Hide(){Visible=false;if(lines!=null)lines.gameObject.SetActive(false);foreach(var gem in gems)if(gem!=null)gem.gameObject.SetActive(false);actor=null;target=null;actorImage=null;targetImage=null;}
 void OnDisable()=>Hide();
 static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
 void OnDestroy(){Hide();foreach(var gem in gems)if(gem!=null)Release(gem.gameObject);if(lines!=null)Release(lines.gameObject);foreach(var m in masks)Release(m);}
}
sealed class SeraCrystalLines : MaskableGraphic {
 readonly List<UIVertex> vertices=new List<UIVertex>();readonly List<int> triangles=new List<int>();
 public void Clear(){vertices.Clear();triangles.Clear();}
 void Vertex(Vector2 p,Color color){var v=UIVertex.simpleVert;v.position=p;v.color=color;vertices.Add(v);}
 public void Line(Vector2 a,Vector2 b,float width,Color color){if(color.a<=0)return;var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=vertices.Count;Vertex(a-n,color);Vertex(a+n,color);Vertex(b+n,color);Vertex(b-n,color);triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k);triangles.Add(k+2);triangles.Add(k+3);}
 public void Glow(Vector2 p,float radius,Color color){int k=vertices.Count;Vertex(p,color);var fade=color;fade.a=0;for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;Vertex(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,fade);if(i>0){triangles.Add(k);triangles.Add(k+i);triangles.Add(k+i+1);}}}
 public void CoreGlow(Vector2 p,float radius,float strength){
  int k=vertices.Count;Vertex(p,new Color(242f/255,1,216f/255,strength*.95f));
  for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Vertex(p+direction*radius*.22f,new Color(143f/255,1,206f/255,strength*.7f));Vertex(p+direction*radius,new Color(82f/255,238f/255,190f/255,0));if(i>0){int a0=k+1+(i-1)*2,a1=k+1+i*2;triangles.Add(k);triangles.Add(a0);triangles.Add(a1);triangles.Add(a0);triangles.Add(a0+1);triangles.Add(a1+1);triangles.Add(a0);triangles.Add(a1+1);triangles.Add(a1);}}
 }
 public void Finish()=>SetVerticesDirty();
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();vh.AddUIVertexStream(vertices,triangles);}
}
}
