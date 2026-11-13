using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.Battle {
 // Caller owns the camera/Canvas projection. Sizes are reference UI pixels, not world PPU.
 public sealed class FiveAgentVisualView:MonoBehaviour {
  [SerializeField]FiveAgentVisualAssets assets;[SerializeField]FourAgentV03Assets v03Assets;readonly List<Graphic> images=new List<Graphic>();public event Action BindingEnded;public int VisibleLayers{get;private set;}
  public void Configure(FiveAgentVisualAssets assets){this.assets=assets??throw new ArgumentNullException(nameof(assets));v03Assets=null;}
  public void ConfigureV03(FourAgentV03Assets catalog){v03Assets=catalog??throw new ArgumentNullException(nameof(catalog));assets=null;}
  public void Present(JObject frame){if(!isActiveAndEnabled)return;if(assets==null&&v03Assets==null||frame==null||(string)frame["version"]!="unity.five-agent-reference-frame.v1.20261017")throw new ArgumentException("Configured visual frame required.");int index=0;foreach(var row in FourAgentVisualDepth.Order((JArray)frame["layers"],(int)frame["direction"])){string kind=(string)row["kind"];Type type=kind=="image"?typeof(RawImage):kind=="kyleFlash"?typeof(KyleMuzzleFlashGraphic):typeof(FiveAgentVisualPrimitive);Graphic graphic;
    if(index>=images.Count||images[index].GetType()!=type){var go=new GameObject("VisualLayer",typeof(RectTransform),typeof(CanvasRenderer),type);go.transform.SetParent(transform,false);graphic=(Graphic)go.GetComponent(type);if(index<images.Count){var old=images[index];old.gameObject.SetActive(false);if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject);images[index]=graphic;}else images.Add(graphic);}else graphic=images[index];
    graphic.gameObject.SetActive(true);graphic.name=(string)row["role"]+"_slot"+(int)row["slot"];graphic.transform.SetSiblingIndex(index);graphic.raycastTarget=false;var r=graphic.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2((float)row["x"],-(float)row["y"]);r.sizeDelta=new Vector2((float)row["w"],(float)row["h"]);r.localRotation=Quaternion.Euler(0,0,-(float)row["angle"]);var color=row["color"];graphic.color=new Color((float)color[0],(float)color[1],(float)color[2],(float)color[3]);
    if(graphic is RawImage image){image.texture=v03Assets!=null?v03Assets.Texture((string)row["path"]):assets.Texture((string)row["path"]);var uv=row["uv"];image.uvRect=uv==null?new Rect(0,0,1,1):new Rect((float)uv[0],(float)uv[1],(float)uv[2],(float)uv[3]);}else if(graphic is FiveAgentVisualPrimitive primitive)primitive.Configure((JObject)row);else if(graphic is KyleMuzzleFlashGraphic flash){flash.VisualScale=(float)row["w"]/26;flash.SetVerticesDirty();}index++;}
   for(int n=index;n<images.Count;n++)images[n].gameObject.SetActive(false);VisibleLayers=index;
  }
  public void Clear(){foreach(var image in images)if(image!=null)image.gameObject.SetActive(false);VisibleLayers=0;}
  void OnDisable(){BindingEnded?.Invoke();Clear();}void OnDestroy(){BindingEnded?.Invoke();BindingEnded=null;}
 }
 public sealed class FiveAgentVisualPrimitive:MaskableGraphic {
  string shape;JArray gradient;float innerX,innerY;
  public void Configure(JObject row){shape=(string)row["kind"];gradient=(JArray)row["gradient"]?.DeepClone();innerX=(float?)row["innerRatioX"]??0;innerY=(float?)row["innerRatioY"]??innerX;SetVerticesDirty();}
  void Fan(VertexHelper vh,IReadOnlyList<Vector2> boundary,Color tint){vh.AddVert(Vector2.zero,tint,Vector2.zero);for(int n=0;n<boundary.Count;n++)vh.AddVert(boundary[n],tint,Vector2.zero);for(int n=0;n<boundary.Count;n++)vh.AddTriangle(0,n+1,(n+1)%boundary.Count+1);}
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();var rect=rectTransform.rect;float rx=rect.width/2,ry=rect.height/2;const int steps=64;
   if(shape=="ring"){
    for(int n=0;n<=steps;n++){float a=n*Mathf.PI*2/steps;var axis=new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);vh.AddVert(axis,color,Vector2.zero);vh.AddVert(new Vector2(axis.x*innerX,axis.y*innerY),color,Vector2.zero);if(n>0){int i=n*2;vh.AddTriangle(i-2,i-1,i);vh.AddTriangle(i-1,i+1,i);}}return;
   }
   if(shape=="glow"){
    var stops=gradient??new JArray(new JArray(0,color.r,color.g,color.b,color.a),new JArray(1,color.r,color.g,color.b,0));
    for(int ring=0;ring<stops.Count;ring++){var stop=stops[ring];float radius=(float)stop[0];var tint=new Color((float)stop[1],(float)stop[2],(float)stop[3],(float)stop[4]);for(int n=0;n<=steps;n++){float a=n*Mathf.PI*2/steps;vh.AddVert(new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry)*radius,tint,Vector2.zero);if(ring>0&&n>0){int outer=ring*(steps+1)+n,inner=outer-(steps+1);vh.AddTriangle(inner-1,inner,outer-1);vh.AddTriangle(inner,outer,outer-1);}}}return;
   }
   var points=new List<Vector2>();
   if(shape=="disc")for(int n=0;n<steps;n++){float a=n*Mathf.PI*2/steps;points.Add(new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry));}
   else if(shape=="star")for(int n=0;n<12;n++){float a=n*Mathf.PI/6,r=n%2==0?1:innerX;points.Add(new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry)*r);}
   else if(shape=="roundedLine"){
    float half=Mathf.Max(0,rx-ry);
    for(int n=0;n<=steps/2;n++){float a=Mathf.PI/2+n*Mathf.PI/(steps/2);points.Add(new Vector2(-half+Mathf.Cos(a)*ry,Mathf.Sin(a)*ry));}
    for(int n=0;n<=steps/2;n++){float a=-Mathf.PI/2+n*Mathf.PI/(steps/2);points.Add(new Vector2(half+Mathf.Cos(a)*ry,Mathf.Sin(a)*ry));}
   }else points.AddRange(new[]{new Vector2(-rx,-ry),new Vector2(-rx,ry),new Vector2(rx,ry),new Vector2(rx,-ry)});
   Fan(vh,points,color);
  }
 }
}
