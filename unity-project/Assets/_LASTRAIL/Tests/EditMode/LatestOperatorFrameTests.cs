using System;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class LatestOperatorFrameTests {
 const BindingFlags Public=BindingFlags.Instance|BindingFlags.Public;
 [Test] public void DirectionSelectsAuthoredSourceWithoutMirroringOrChangingLegacyDefault(){
  var set=ScriptableObject.CreateInstance<CombatOperatorFrames>();
  var left=new CombatOperatorFrames.Entry{OperatorId="op_sera",FacesRight=false};
  var right=new CombatOperatorFrames.Entry{OperatorId="op_sera",FacesRight=true};
  try{
   set.Entries=new[]{left};
   var field=typeof(CombatOperatorFrames).GetField("DirectionalEntries",Public);Assert.NotNull(field);
   field.SetValue(set,new[]{right});
   var find=typeof(CombatOperatorFrames).GetMethod("Find",new[]{typeof(string),typeof(bool)});Assert.NotNull(find);
   Assert.AreSame(left,set.Find("op_sera"));
   Assert.AreSame(left,find.Invoke(set,new object[]{"op_sera",false}));
   Assert.AreSame(right,find.Invoke(set,new object[]{"op_sera",true}));
   Assert.IsNull(find.Invoke(set,new object[]{"missing",true}));
  }finally{UnityEngine.Object.DestroyImmediate(set);}
 }
 [Test] public void UltimateUsesAuthoredExposureAndStopsAtEnd(){
  var first=new Texture2D(2,2);var second=new Texture2D(2,2);
  try{
   var entry=new CombatOperatorFrames.Entry();var type=entry.GetType();
   var frames=type.GetField("UltimateFrames");Assert.NotNull(frames);
   frames.SetValue(entry,new[]{first,second});type.GetField("UltimateDurationsMs").SetValue(entry,new[]{180,1080});
   var at=type.GetMethod("UltimateFrameAt");Assert.NotNull(at);
   Assert.AreSame(first,at.Invoke(entry,new object[]{179d}));Assert.AreSame(second,at.Invoke(entry,new object[]{180d}));
   Assert.AreSame(second,at.Invoke(entry,new object[]{1259d}));Assert.IsNull(at.Invoke(entry,new object[]{1260d}));
  }finally{UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);}
 }
 [Test] public void CancelledLatestCastDoesNotResumePreviousAppliedClip(){
  var method=typeof(ApprovedCombatView).GetMethod("LatestPresentationCast",BindingFlags.NonPublic|BindingFlags.Static);
  var casts=Newtonsoft.Json.Linq.JArray.Parse("[{actor:'a',ability:'op_sera.active',status:'APPLIED',due:10,windupTicks:2},{actor:'a',ability:'op_sera.ultimate',status:'CASTING',due:20,windupTicks:3}]");
  Assert.AreSame(casts[1],method.Invoke(null,new object[]{casts,"a","op_sera"}));
  casts[1]["status"]="CANCELLED";Assert.IsNull(method.Invoke(null,new object[]{casts,"a","op_sera"}));
 }
 [Test] public void ExistingSerializedRegistryRemainsValid(){
  var registry=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/KyleRightActive02/Roster.asset");
  Assert.NotNull(registry);Assert.DoesNotThrow(registry.Validate);
 }
 [Test] public void IndependentEffectEndsAndPerMotionCanvasKeepsItsOwnRegistration(){
  var texture=new Texture2D(2,2);
  try{var entry=new CombatOperatorFrames.Entry{ActiveEffectFrames=new[]{texture},ActiveEffectDurationsMs=new[]{100},CanvasSize=new Vector2(10,20),BodySpan=10};
   Assert.AreSame(texture,entry.EffectFrameAt(false,99));Assert.IsNull(entry.EffectFrameAt(false,100));Assert.IsNull(entry.EffectFrameAt(true,0));
   var registration=new CombatOperatorFrames.Entry.Registration{CanvasSize=new Vector2(30,40),BodySpan=20,FootAnchorTopLeft=new Vector2(15,35)};
   Assert.AreEqual(new Vector2(60,80),entry.SizeForHeight(40,registration));Assert.AreEqual(new Vector2(45,-90),entry.TopLeftForHeight(40,registration));
  }finally{UnityEngine.Object.DestroyImmediate(texture);}
 }
}
