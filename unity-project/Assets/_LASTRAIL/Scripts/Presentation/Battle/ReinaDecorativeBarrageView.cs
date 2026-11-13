using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Battle {
 // Owns only pooled decorative sprites. Never holds a combat owner or an AudioSource.
 public sealed class ReinaDecorativeBarrageView:IDisposable {
  const string Resource="CombatPresentation/ReinaFinal/Decorative62/";
  sealed class Element {internal Texture2D[] Frames;internal Vector2 Pivot;internal float Extent;}
  sealed class Playback {
   internal string Id;internal ReinaDecorativeBarrageSchedule Schedule;internal Vector2 Target;
   internal double Age,MaximumStep,End;internal readonly HashSet<string> Shown=new HashSet<string>(),Landed=new HashSet<string>();
  }
  readonly RectTransform parent,layer;readonly Func<JObject,Vector2?> project;
  readonly Dictionary<string,Element> elements=new Dictionary<string,Element>();
  readonly List<RawImage> pool=new List<RawImage>();readonly HashSet<string> seen=new HashSet<string>();
  readonly Queue<Playback> pending=new Queue<Playback>();readonly JArray definitions;
  readonly double centerX;Playback current;double lastTime;bool sampled,disposed;int used;
  public int GeneratedCount {get;private set;} public int ShownCount {get;private set;} public int LandedCount {get;private set;}
  public int MaximumAirborne {get;private set;} public int LastSeed {get;private set;}
  public bool Active=>current!=null||pending.Count>0;
  public ReinaDecorativeBarrageView(RectTransform parent,Func<JObject,Vector2?> project){
   this.parent=parent??throw new ArgumentNullException(nameof(parent));this.project=project??throw new ArgumentNullException(nameof(project));
   var asset=LastRail.Resources.PreparedContent.Load<TextAsset>(Resource+"events");if(asset==null)throw new InvalidOperationException("Original decorative barrage metadata missing.");
   var data=JObject.Parse(asset.text);definitions=(JArray)data["events"];if(definitions?.Count!=62)throw new ArgumentException("62 decorative rounds required.");
   centerX=definitions.Average(x=>(double)x["anchor"][0]);
   foreach(var property in ((JObject)data["elements"]).Properties()){
    var e=property.Value;var frames=((JArray)e["files"]).Select(f=>LastRail.Resources.PreparedContent.Load<Texture2D>(Resource+System.IO.Path.GetFileNameWithoutExtension((string)f))).ToArray();
    if(frames.Length!=12||frames.Any(t=>t==null))throw new InvalidOperationException("Original 12-frame FX missing: "+property.Name);
    elements.Add(property.Name,new Element{Frames=frames,Pivot=new Vector2((float)e["pivot"][0]/frames[0].width,1-(float)e["pivot"][1]/frames[0].height),Extent=(float)e["extent"]});
   }
   var go=new GameObject("ReinaDecorative62",typeof(RectTransform));layer=(RectTransform)go.transform;layer.SetParent(parent,false);layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;layer.offsetMin=layer.offsetMax=Vector2.zero;
  }
  public void Refresh(JArray feedback,double time,bool reduced){
   if(disposed)throw new ObjectDisposedException(nameof(ReinaDecorativeBarrageView));
   layer.gameObject.SetActive(true);
   double elapsed=sampled?Math.Max(0,time-lastTime):0;lastTime=time;sampled=true;
   if(feedback!=null)foreach(JObject row in feedback){
    string id=(string)row["commandId"];var target=project(row);if(!target.HasValue)continue;
    if(current?.Id==id)current.Target=target.Value;foreach(var queued in pending)if(queued.Id==id)queued.Target=target.Value;
    if(!seen.Add(id)||(int)row["nextPulse"]!=0)continue;
    LastSeed=Guid.NewGuid().GetHashCode();var rng=new System.Random(LastSeed);var schedule=new ReinaDecorativeBarrageSchedule(definitions,rng.NextDouble);
    pending.Enqueue(new Playback{Id=id,Target=target.Value,Schedule=schedule,MaximumStep=schedule.Rounds.Min(r=>r.Fall)/2,End=schedule.Rounds.Max(r=>r.Impact+Math.Max(r.CoreLife,r.ShockLife))});
    GeneratedCount+=62;
   }
   if(current==null&&pending.Count>0){current=pending.Dequeue();elapsed=0;}
   used=0;
   if(current!=null){
    // A stalled render may lengthen decoration, never omit rounds or advance gameplay.
    current.Age+=Math.Min(elapsed,current.MaximumStep);int airborne=0;
    foreach(var shot in current.Schedule.Rounds){
     double age=current.Age-shot.Impact;var target=current.Target+new Vector2((float)(shot.X-centerX)*parent.rect.width,0);
     if(age>=0&&current.Landed.Add(shot.Id))LandedCount++;
     if(age<0&&current.Age>=shot.Start){
      airborne++;if(current.Shown.Add(shot.Id))ShownCount++;
      float q=(float)((current.Age-shot.Start)/shot.Fall),u=Mathf.Pow(q,1.12f),v=1-u;
      var entry=new Vector2(Mathf.Clamp(target.x+(float)(shot.EntryX-shot.X)*parent.rect.width,0,parent.rect.width),parent.rect.height*(1-(float)shot.EntryY));
      var control=new Vector2(target.x+(float)(shot.ControlX-shot.X)*parent.rect.width,Mathf.Lerp(entry.y,target.y,.55f));
      var position=v*v*entry+2*v*u*control+u*u*target;var tangent=2*v*(control-entry)+2*u*(target-control);
      float length=(Vector2.Distance(entry,control)+Vector2.Distance(control,target))*Mathf.Min(.30f,q);
      Stamp("falling_shell",q,position,length,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg,reduced);
     }
     if(age>=0&&age<shot.CoreLife)Stamp(shot.Kind=="major"?"major_core":"minor_core",(float)(age/shot.CoreLife),target,(float)(shot.Radius*shot.Scale)*parent.rect.width,0,reduced);
     if(age>=0&&age<shot.ShockLife)Stamp("shockwave",(float)(age/shot.ShockLife),target,(float)shot.Radius*parent.rect.width,0,reduced);
    }
    MaximumAirborne=Math.Max(MaximumAirborne,airborne);if(airborne>5)throw new InvalidOperationException("Decorative airborne cap violated.");
    if(current.Age>=current.End)current=null;
   }
   for(int i=used;i<pool.Count;i++)pool[i].gameObject.SetActive(false);
  }
  public void Hide(){layer.gameObject.SetActive(false);}
  void Stamp(string kind,float phase,Vector2 position,float size,float angle,bool reduced){
   if(used==pool.Count){var go=new GameObject("DecorativeFx",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(layer,false);var image=go.GetComponent<RawImage>();image.raycastTarget=false;image.rectTransform.anchorMin=image.rectTransform.anchorMax=Vector2.zero;pool.Add(image);}
   var item=pool[used++];var element=elements[kind];var texture=element.Frames[Mathf.Clamp((int)(phase*12),0,11)];
   item.texture=texture;item.rectTransform.pivot=element.Pivot;item.rectTransform.anchoredPosition=position;item.rectTransform.localRotation=Quaternion.Euler(0,0,angle);item.rectTransform.sizeDelta=new Vector2(texture.width,texture.height)*(size/element.Extent)*(reduced?.6f:1);item.gameObject.SetActive(true);
  }
  public void Cancel(string id){if(current?.Id==id)current=null;var keep=pending.Where(p=>p.Id!=id).ToArray();pending.Clear();foreach(var p in keep)pending.Enqueue(p);foreach(var image in pool)image.gameObject.SetActive(false);}
  public void Clear(){current=null;pending.Clear();sampled=false;foreach(var image in pool)image.gameObject.SetActive(false);}
  public void Dispose(){if(disposed)return;disposed=true;Clear();seen.Clear();elements.Clear();pool.Clear();if(Application.isPlaying)UnityEngine.Object.Destroy(layer.gameObject);else UnityEngine.Object.DestroyImmediate(layer.gameObject);}
 }
}
