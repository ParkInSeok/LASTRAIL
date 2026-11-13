using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatShieldTransferTests
{
    static CombatShieldSource Source(string id)=>new CombatShieldSource(id,"kyle-original",0);
    static int Move(CombatVitals giver,CombatVitals receiver,long tick)
    {
        var method=typeof(CombatVitals).GetMethod("TransferPersonalShield");Assert.IsNotNull(method);
        return (int)method.Invoke(giver,new object[]{receiver,tick,new Func<CombatShieldSource,bool>(s=>s.AbilityId=="kyle-original")});
    }
    [Test]public void OnlyActualRoomMovesAndOriginalSourceExpiryAndHpRemain()
    {
        var a=new CombatVitals(1000,10000,true);var b=new CombatVitals(1000,10000,true);
        a.Shield(Source("a"),200,20,0);b.Shield(Source("b"),220,40,0);
        Assert.AreEqual(30,Move(a,b,1));Assert.AreEqual(170,a.ShieldRemaining(1));Assert.AreEqual(250,b.ShieldRemaining(1));Assert.AreEqual(1000,b.Hp);
        b.RebaseMaxHp(500);Assert.AreEqual(250,b.ShieldRemaining(1));Assert.AreEqual(0,Move(a,b,1));
        Assert.AreEqual(220,b.ShieldRemaining(20));Assert.AreEqual(0,a.ShieldRemaining(20));
    }
    [Test]public void DifferentExpirySlicesSurviveRestoreAndO02IsNotAnEligibleTransfer()
    {
        var a=new CombatVitals(1000,10000,true);var b=new CombatVitals(1000,10000,true);var c=new CombatVitals(1000,10000,true);
        a.Shield(Source("a"),100,20,0);c.Shield(Source("a"),100,40,0);a.Shield(new CombatShieldSource("a","O02",0),300,50,0);
        Assert.AreEqual(100,Move(a,b,1));Assert.AreEqual(100,Move(c,b,1));Assert.AreEqual(300,a.ShieldRemaining(1));
        var data=(JObject)typeof(CombatVitals).GetMethod("CaptureLocal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);
        var restored=new CombatVitals(1000,10000,true);typeof(CombatVitals).GetMethod("RestoreLocal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(restored,new object[]{data,1L});
        Assert.AreEqual(200,restored.ShieldRemaining(1));Assert.AreEqual(100,restored.ShieldRemaining(20));
        var consumed=restored.ConsumeShieldForAttack(20,1000,s=>true);Assert.AreEqual(20,consumed.Consumed);Assert.AreEqual("a",consumed.Layers[0].Source.EntityId);
        Assert.AreEqual(0,restored.ShieldRemaining(40));
    }
    [Test]public void DownRecipientAndStaleTickCannotPartiallyConsumeDonor()
    {
        var a=new CombatVitals(1000,10000,true);var b=new CombatVitals(1000,10000,true);a.Shield(Source("a"),200,20,0);
        b.Damage(1000,2,false);Assert.AreEqual(0,Move(a,b,2));Assert.AreEqual(200,a.ShieldRemaining(2));
        Assert.Throws<TargetInvocationException>(()=>Move(a,b,1));Assert.AreEqual(200,a.ShieldRemaining(2));
    }
    [Test]public void ExhaustedReceivingSliceCanReceiveAgainWithoutLosingDonorAmount()
    {
        var a=new CombatVitals(1000,10000,true);var b=new CombatVitals(1000,10000,true);
        a.Shield(Source("a"),100,20,0);Assert.AreEqual(100,Move(a,b,1));b.Damage(100,1,false);
        a.Shield(Source("a"),100,20,1);Assert.AreEqual(100,Move(a,b,1));Assert.AreEqual(100,b.ShieldRemaining(1));
    }
}
