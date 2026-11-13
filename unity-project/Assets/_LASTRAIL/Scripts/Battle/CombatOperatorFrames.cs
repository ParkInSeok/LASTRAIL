using System;
using System.Collections.Generic;
using UnityEngine;
namespace LastRail.Battle {
 // Source textures are borrowed; only display registration and exposure timing belong here.
 [CreateAssetMenu(menuName="LAST RAIL/Combat Operator Frames")]
 public sealed class CombatOperatorFrames:ScriptableObject {
  public Entry[] Entries=Array.Empty<Entry>();
  public Entry[] DirectionalEntries=Array.Empty<Entry>();
  public Entry Find(string id){foreach(var entry in Entries)if(entry.OperatorId==id)return entry;return null;}
  public Entry Find(string id,bool facesRight){foreach(var entry in DirectionalEntries)if(entry.OperatorId==id&&entry.FacesRight==facesRight)return entry;return Find(id);}
  public void Validate(){
   if(Entries==null)throw new InvalidOperationException("Missing operator frame registry");
   var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(var entry in Entries){if(entry==null||string.IsNullOrWhiteSpace(entry.OperatorId)||!ids.Add(entry.OperatorId))throw new InvalidOperationException("Invalid or duplicate operator frame identity");entry.Validate();}
   if(DirectionalEntries==null)throw new InvalidOperationException("Missing directional frame table");
   ids.Clear();
   foreach(var entry in DirectionalEntries){if(entry==null||Find(entry.OperatorId)==null||!ids.Add(entry.OperatorId+":"+entry.FacesRight))throw new InvalidOperationException("Invalid or duplicate directional identity");entry.Validate();}
  }
  [Serializable] public sealed class Entry {
   [Serializable] public sealed class Registration { public Vector2 CanvasSize,FootAnchorTopLeft;public float BodySpan; }
   public string OperatorId;
   public bool FacesRight;
   public Vector2 CanvasSize,FootAnchorTopLeft;
   public float BodySpan;
   public Texture2D[] IdleFrames=Array.Empty<Texture2D>();
   public int[] DurationsMs=Array.Empty<int>();
   public Texture2D[] BasicFrames=Array.Empty<Texture2D>();
   public int[] BasicDurationsMs=Array.Empty<int>();
   public int BasicFireFrame;
   public Texture2D[] ActiveFrames=Array.Empty<Texture2D>();
   public int[] ActiveDurationsMs=Array.Empty<int>();
   public Texture2D[] UltimateFrames=Array.Empty<Texture2D>();
   public int[] UltimateDurationsMs=Array.Empty<int>();
   public Registration BasicRegistration,ActiveRegistration,UltimateRegistration;
   public Texture2D[] ActiveEffectFrames=Array.Empty<Texture2D>(),UltimateEffectFrames=Array.Empty<Texture2D>();
   public int[] ActiveEffectDurationsMs=Array.Empty<int>(),UltimateEffectDurationsMs=Array.Empty<int>();
   public double ActiveDurationMs {get{double total=0;foreach(int duration in ActiveDurationsMs)total+=duration;return total;}}
   public Texture2D BasicProjectile;
   public Texture2D Portrait,Emblem;
   public Rect PortraitUv=new Rect(0,0,1,1),EmblemRectTopLeft;
   public bool HasIdle=>IdleFrames!=null&&IdleFrames.Length>0;
   static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
   bool ValidRect(Rect r,float width,float height)=>Finite(r.x)&&Finite(r.y)&&Finite(r.width)&&Finite(r.height)&&r.x>=0&&r.y>=0&&r.width>0&&r.height>0&&r.xMax<=width&&r.yMax<=height;
   public void Validate(){
    if(IdleFrames==null||DurationsMs==null||IdleFrames.Length!=DurationsMs.Length)throw new InvalidOperationException("Incomplete exposure table: "+OperatorId);
    if(Portrait!=null&&!ValidRect(PortraitUv,1,1))throw new InvalidOperationException("Invalid portrait crop: "+OperatorId);
    if(!HasIdle){if(Portrait==null||Emblem!=null)throw new InvalidOperationException("Entry has no supplied body or portrait: "+OperatorId);return;}
    if(!Finite(CanvasSize.x)||!Finite(CanvasSize.y)||CanvasSize.x<=0||CanvasSize.y<=0||!Finite(BodySpan)||BodySpan<=0||BodySpan>CanvasSize.y||!Finite(FootAnchorTopLeft.x)||!Finite(FootAnchorTopLeft.y)||FootAnchorTopLeft.x<0||FootAnchorTopLeft.y<0||FootAnchorTopLeft.x>CanvasSize.x||FootAnchorTopLeft.y>CanvasSize.y)throw new InvalidOperationException("Invalid source registration: "+OperatorId);
    for(int i=0;i<IdleFrames.Length;i++)if(IdleFrames[i]==null||IdleFrames[i].width!=CanvasSize.x||IdleFrames[i].height!=CanvasSize.y||DurationsMs[i]<=0)throw new InvalidOperationException("Invalid source exposure: "+OperatorId);
    if(BasicFrames==null||BasicDurationsMs==null||BasicFrames.Length!=BasicDurationsMs.Length||BasicFrames.Length>0&&(BasicFireFrame<=0||BasicFireFrame>=BasicFrames.Length))throw new InvalidOperationException("Invalid basic exposure table: "+OperatorId);
    ValidateClip(BasicFrames,BasicDurationsMs,BasicRegistration,"basic");
    if(ActiveFrames==null||ActiveDurationsMs==null||ActiveFrames.Length!=ActiveDurationsMs.Length)throw new InvalidOperationException("Invalid active exposure table: "+OperatorId);
    ValidateClip(ActiveFrames,ActiveDurationsMs,ActiveRegistration,"active");
    if(UltimateFrames==null||UltimateDurationsMs==null||UltimateFrames.Length!=UltimateDurationsMs.Length)throw new InvalidOperationException("Invalid ultimate exposure table: "+OperatorId);
    ValidateClip(UltimateFrames,UltimateDurationsMs,UltimateRegistration,"ultimate");
    if(ActiveEffectFrames==null||ActiveEffectDurationsMs==null||ActiveEffectFrames.Length!=ActiveEffectDurationsMs.Length||UltimateEffectFrames==null||UltimateEffectDurationsMs==null||UltimateEffectFrames.Length!=UltimateEffectDurationsMs.Length)throw new InvalidOperationException("Invalid independent effect exposure table: "+OperatorId);
    ValidateClip(ActiveEffectFrames,ActiveEffectDurationsMs,ActiveRegistration,"active effect");
    ValidateClip(UltimateEffectFrames,UltimateEffectDurationsMs,UltimateRegistration,"ultimate effect");
    if(Emblem!=null&&!ValidRect(EmblemRectTopLeft,CanvasSize.x,CanvasSize.y))throw new InvalidOperationException("Invalid independent emblem registration: "+OperatorId);
   }
   // Unity materializes absent inline classes as zero-filled objects in legacy assets.
   static Registration Supplied(Registration value)=>value!=null&&value.CanvasSize==Vector2.zero&&value.FootAnchorTopLeft==Vector2.zero&&value.BodySpan==0?null:value;
   void ValidateClip(Texture2D[] frames,int[] durations,Registration registration,string kind){
    registration=Supplied(registration);var canvas=registration?.CanvasSize??CanvasSize;var foot=registration?.FootAnchorTopLeft??FootAnchorTopLeft;var span=registration?.BodySpan??BodySpan;
    if(frames.Length>0&&(!Finite(canvas.x)||!Finite(canvas.y)||canvas.x<=0||canvas.y<=0||!Finite(span)||span<=0||span>canvas.y||!Finite(foot.x)||!Finite(foot.y)||foot.x<0||foot.y<0||foot.x>canvas.x||foot.y>canvas.y))throw new InvalidOperationException("Invalid "+kind+" registration: "+OperatorId);
    for(int i=0;i<frames.Length;i++)if(frames[i]==null||frames[i].width!=canvas.x||frames[i].height!=canvas.y||durations[i]<=0)throw new InvalidOperationException("Invalid "+kind+" exposure: "+OperatorId);
   }
   public Texture2D FrameAt(double milliseconds){
    if(!HasIdle)throw new InvalidOperationException("No idle supplied: "+OperatorId);
    if(double.IsNaN(milliseconds)||double.IsInfinity(milliseconds)||milliseconds<0)throw new ArgumentOutOfRangeException(nameof(milliseconds));
    double total=0;foreach(int duration in DurationsMs)total+=duration;
    double phase=milliseconds%total;for(int i=0;i<DurationsMs.Length;i++){if(phase<DurationsMs[i])return IdleFrames[i];phase-=DurationsMs[i];}
    return IdleFrames[0];
   }
   public Vector2 SizeForHeight(float height,Registration registration=null){registration=Supplied(registration);return (registration?.CanvasSize??CanvasSize)*(height/(registration?.BodySpan??BodySpan));}
   // Presentation uses the authored clock; it must never extend the domain action lock.
   public Texture2D ActiveFrameAt(double elapsedMilliseconds){
    if(double.IsNaN(elapsedMilliseconds)||double.IsInfinity(elapsedMilliseconds)||elapsedMilliseconds<0)throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
    for(int i=0;i<ActiveDurationsMs.Length;i++){if(elapsedMilliseconds<ActiveDurationsMs[i])return ActiveFrames[i];elapsedMilliseconds-=ActiveDurationsMs[i];}
    return null;
   }
   public Texture2D UltimateFrameAt(double elapsedMilliseconds){
    if(double.IsNaN(elapsedMilliseconds)||double.IsInfinity(elapsedMilliseconds)||elapsedMilliseconds<0)throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
    for(int i=0;i<UltimateDurationsMs.Length;i++){if(elapsedMilliseconds<UltimateDurationsMs[i])return UltimateFrames[i];elapsedMilliseconds-=UltimateDurationsMs[i];}
    return null;
   }
   public Texture2D EffectFrameAt(bool ultimate,double elapsedMilliseconds){
    if(double.IsNaN(elapsedMilliseconds)||double.IsInfinity(elapsedMilliseconds)||elapsedMilliseconds<0)throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
    var frames=ultimate?UltimateEffectFrames:ActiveEffectFrames;var durations=ultimate?UltimateEffectDurationsMs:ActiveEffectDurationsMs;
    for(int i=0;i<durations.Length;i++){if(elapsedMilliseconds<durations[i])return frames[i];elapsedMilliseconds-=durations[i];}
    return null;
   }
   public Texture2D BasicFrameAt(long tick,long started,long commit,long recoveryEnd,double idleMilliseconds,bool committed){
    if(BasicFrames.Length==0||started<0||commit<=started||recoveryEnd<=commit||tick<started||tick>=recoveryEnd||tick>=commit&&!committed)return FrameAt(idleMilliseconds);
    int first=tick<commit?0:BasicFireFrame,last=tick<commit?BasicFireFrame:BasicFrames.Length;
    double total=0;for(int i=first;i<last;i++)total+=BasicDurationsMs[i];
    double phase=total*(tick-(tick<commit?started:commit))/(tick<commit?commit-started:recoveryEnd-commit);
    for(int i=first;i<last;i++){if(phase<BasicDurationsMs[i])return BasicFrames[i];phase-=BasicDurationsMs[i];}return BasicFrames[last-1];
   }
   public Vector2 TopLeftForHeight(float height,Registration registration=null){registration=Supplied(registration);float scale=height/(registration?.BodySpan??BodySpan);var foot=registration?.FootAnchorTopLeft??FootAnchorTopLeft;return new Vector2(75-foot.x*scale,-160+foot.y*scale);}
  }
 }
}
