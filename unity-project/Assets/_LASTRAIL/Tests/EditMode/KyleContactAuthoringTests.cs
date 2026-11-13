using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class KyleContactAuthoringTests {
 [Test] public void NativeKyleAuthoringIncludesMeasuredBasicContact(){
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OrdinaryLiveGeometryBuilder")).First(t=>t!=null);
  var parent=new GameObject("Foot");
  try{
   var actor=(Transform)type.GetMethod("Actor",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{parent.transform,"op_kyle",false});
   var fist=actor.Find("FistContactBasic04");Assert.NotNull(fist,"Native geometry exporter must supply the melee contact owner");
   Assert.AreEqual(new Vector3(.609f,1.310f,0),fist.localPosition);
   Assert.AreEqual(new Vector3(.5f,1.4f,0),actor.Find("Muzzle").localPosition,"Ranged emission must remain independent");
  }finally{UnityEngine.Object.DestroyImmediate(parent);}
 }
}
