using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 sealed class RigShot {public string Source;public float End;public RawImage Image;}
 readonly List<RigShot> captureRigShots=new List<RigShot>();
 public int CaptureRigShotCount{get;private set;}
 void OnCaptureRigShot(JToken hit){
  string source=(string)hit["Source"];if(!captureRigs.TryGetValue(source,out var rig)||!rig.TrySocket("muzzle",out _,out _)||assets.TurretRig==null)return;
  var image=Raw("ActualOperatorMuzzle",root,assets.TurretRig,0,0,27,18);image.uvRect=new Rect(770f/1536,1-1000f/1024,766f/1536,500f/1024);image.rectTransform.pivot=new Vector2(.15f,.5f);
  captureRigShots.Add(new RigShot{Source=source,End=(long)hit["Tick"]*.05f+.09f,Image=image});CaptureRigShotCount++;
 }
 void RefreshCaptureRigShots(float seconds){
  foreach(var shot in captureRigShots.ToArray()){
   if(seconds>=shot.End||!captureRigs.TryGetValue(shot.Source,out var rig)||!rig.TrySocket("muzzle",out var point,out var direction)){DisposeVisual(shot.Image.gameObject);captureRigShots.Remove(shot);continue;}
   var at=RootPoint(point);var local=root.InverseTransformDirection(direction);shot.Image.rectTransform.anchoredPosition=at;shot.Image.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(local.y,local.x)*Mathf.Rad2Deg);shot.Image.color=new Color(1,1,1,Mathf.Clamp01((shot.End-seconds)/.035f));
  }
 }
}
}
