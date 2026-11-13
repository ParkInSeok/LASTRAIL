using System;
using System.Collections.Generic;
using LastRail.Run;
using UnityEngine;

namespace LastRail.Battle {
 // Borrows approved spatial bodies; the simulation alone owns tick, progress, HP and lifetime.
 public sealed class OrdinaryNodeSceneMotion:IDisposable {
  readonly Transform railStart;readonly Matrix4x4 railBasis;readonly Func<bool> current;
  readonly Transform[] frames=new Transform[4];readonly BoxCollider[] bodies=new BoxCollider[4];
  readonly long[] offsets={-6000,-19000,-32000,0};
  long lastTick=-1,lastFront;bool disposed,applying;
  public OrdinaryNodeSceneMotion(OrdinaryFiveStartInput start,OrdinaryNodeStart node,OrdinaryNodeAttempt attempt,Transform railStart,Dictionary<string,Transform> frames,Func<bool> currentAttempt){
   if(start==null||node==null||attempt==null||!railStart||frames==null||currentAttempt==null)throw new ArgumentException("Original node, rail origin and borrowed bodies required.");
   node.RequireOriginal(start);attempt.RequireNode(node);
   if(start.Rail?.SourceVersion!=OrdinaryRailSource.MotionVersion||start.Rail.PhysicalInstanceIds.Count!=3||node.Enemies?.Spatial==null||frames.Count!=4)throw new ArgumentException("Current original three-car and enemy spatial source required.");
   var spatial=node.Enemies.Spatial.CaptureDefinition();
   if((string)spatial["motion"]?["source"]!="PLAYER_ACTUAL_MOTION"||(long?)spatial["motion"]?["relativeSpeedMmPerSecond"]!=0||(long?)spatial["geometry"]?["lengthMm"]!=12000)throw new ArgumentException("Original following enemy motion required.");
   offsets[3]=checked((long)spatial["placement"]["gapFromPlayerFrontMm"]+(long)spatial["geometry"]["lengthMm"]/2);
   this.railStart=railStart;railBasis=railStart.localToWorldMatrix;current=currentAttempt;
   var unique=new HashSet<Transform>();
   for(int i=0;i<4;i++){
    string id=i<3?start.Rail.PhysicalInstanceIds[i]:node.Enemies.Spatial.PhysicalInstanceId;
    if(!frames.TryGetValue(id,out var frame)||!frame||!unique.Add(frame)||frame==railStart||railStart.IsChildOf(frame))throw new ArgumentException("Every original physical body must be bound once.");
    this.frames[i]=frame;var body=frame.Find("BodyCollider");bodies[i]=body?body.GetComponent<BoxCollider>():null;
    if(!start.Rail.InitialFrontArcMm.HasValue||!ValidBody(i)||frame.position!=Position(i,start.Rail.InitialFrontArcMm.Value))throw new ArgumentException("Actual approved body dimensions and initial rail placement required.");
   }
   Active();
  }
  Vector3 Position(int i,long front)=>railStart.TransformPoint(new Vector3((front+offsets[i])/1000f,0,0));
  bool ValidBody(int i){var frame=frames[i];var body=bodies[i];return body&&body.transform.IsChildOf(frame)&&frame.lossyScale==Vector3.one&&railStart.lossyScale==Vector3.one&&frame.rotation==railStart.rotation&&body.transform.lossyScale==Vector3.one&&body.transform.rotation==frame.rotation&&body.size==new Vector3(12,3.2f,3)&&frame.InverseTransformPoint(body.transform.TransformPoint(body.center))==new Vector3(0,1.6f,0);}
  void Active(){if(disposed)throw new ObjectDisposedException(nameof(OrdinaryNodeSceneMotion));bool valid=current();if(disposed)throw new ObjectDisposedException(nameof(OrdinaryNodeSceneMotion));if(!valid||!railStart||railStart.localToWorldMatrix!=railBasis)throw new InvalidOperationException("Original scene motion attempt or rail basis is no longer current.");}
  public void Apply(long tick,long frontArcMm){
   if(applying)throw new InvalidOperationException("Scene motion cannot reenter.");applying=true;
   try{
    Active();if(tick<0||tick<lastTick||frontArcMm<38000||frontArcMm>1000000000||(tick==lastTick&&frontArcMm!=lastFront))throw new InvalidOperationException("Ordered original tick and absolute front required.");
    // Validate all surviving bodies before publishing any pose. Destruction is observed by the geometry owner.
    for(int i=0;i<4;i++)if(frames[i]&&(!ValidBody(i)||(tick==lastTick&&frames[i].position!=Position(i,lastFront))))throw new InvalidOperationException("Borrowed body changed outside the original pose publication.");
    Active();if(tick==lastTick)return;
    for(int i=0;i<4;i++)if(frames[i])frames[i].position=Position(i,frontArcMm);
    lastTick=tick;lastFront=frontArcMm;
   }finally{applying=false;}
  }
  public void Dispose(){disposed=true;Array.Clear(frames,0,frames.Length);Array.Clear(bodies,0,bodies.Length);}
 }
}
