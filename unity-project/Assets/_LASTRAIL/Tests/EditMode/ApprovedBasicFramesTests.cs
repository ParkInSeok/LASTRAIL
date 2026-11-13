using System;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json.Linq;
public sealed class ApprovedBasicFramesTests {
 [Test] public void ProjectilePresentationUsesFrozenRayAndOriginalDistanceLimit(){
  var ray=new JObject{["originMm"]=new JArray(1000,0,0),["anchorMm"]=new JArray(2000,0,0),["launchObservation"]=new JObject{["profile"]=new JObject{["ballistics"]=new JObject{["speedMmPerSecond"]=1000,["maxDistanceMm"]=2000}}}};
  var method=typeof(MiraJudgementGeometry).GetMethod("PresentationPoint",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(method);
  var p=(JArray)method.Invoke(null,new object[]{ray,500000L});Assert.AreEqual(1500,(double)p[0],.0001);
  p=(JArray)method.Invoke(null,new object[]{ray,3000000L});Assert.AreEqual(3000,(double)p[0],.0001);Assert.AreEqual(0,(double)p[1]);
 }
 [Test] public void AuthoredReleaseMatchesActualCommitAndRecoveryReturnsToIdle(){
  var e=new CombatOperatorFrames.Entry();var textures=new Texture2D[4];for(int i=0;i<4;i++)textures[i]=new Texture2D(2,2);
  try{
   e.IdleFrames=new[]{textures[0]};e.DurationsMs=new[]{100};var t=e.GetType();Assert.NotNull(t.GetField("BasicFrames"));
   t.GetField("BasicFrames").SetValue(e,new[]{textures[1],textures[2],textures[3]});t.GetField("BasicDurationsMs").SetValue(e,new[]{100,100,100});t.GetField("BasicFireFrame").SetValue(e,1);
   var method=t.GetMethod("BasicFrameAt");Assert.NotNull(method);
   Texture2D At(long tick,bool committed=true)=>(Texture2D)method.Invoke(e,new object[]{tick,10L,20L,40L,0d,committed});
   Assert.AreSame(textures[0],At(9));Assert.AreSame(textures[1],At(10));Assert.AreSame(textures[1],At(19));Assert.AreSame(textures[2],At(20));Assert.AreSame(textures[3],At(30));Assert.AreSame(textures[0],At(40));
   Assert.AreSame(textures[0],At(20,false));Assert.AreSame(textures[0],At(30,false));
  }finally{foreach(var texture in textures)UnityEngine.Object.DestroyImmediate(texture);}
 }
}
