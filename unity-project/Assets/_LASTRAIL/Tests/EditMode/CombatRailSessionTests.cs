using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
public class CombatRailSessionTests {
 [Test] public void NewSessionUsesActualWrapperFactoryAndDurableCasWithoutStart(){var p=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.Ce03RailSession");Assert.NotNull(type);var session=Activator.CreateInstance(type,new object[]{p,"session-local",1u});string bytes=null;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/rail-session-test.json",()=>bytes,s=>bytes=s);var method=type.GetMethod("CreateDriver");var d=(CombatSceneDriver)method.Invoke(session,new object[]{store,(Func<bool>)(()=>true)});d.Open();d.SetRailThrottle(13000);d.Advance(500000,8);var restore=(CombatSceneDriver)method.Invoke(session,new object[]{store,(Func<bool>)(()=>true)});restore.Open();Assert.AreEqual(d.StateJson,restore.StateJson);Assert.AreEqual(13000,(int)restore.HudState["railJourney"]["throttleBps"]);var other=Activator.CreateInstance(type,new object[]{p,"different",1u});var wrong=(CombatSceneDriver)method.Invoke(other,new object[]{store,(Func<bool>)(()=>true)});Assert.Throws<InvalidOperationException>(()=>wrong.Open());}
}
