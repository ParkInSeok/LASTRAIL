using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatShieldConsumptionTests
{
    static readonly CombatShieldSource A=new CombatShieldSource("caster-a","O02",0),B=new CombatShieldSource("caster-b","native",0),Shared=new CombatShieldSource("shared","barrier",0);
    static int Consume(CombatVitals v,long tick,int budget)
    {
        var method=typeof(CombatVitals).GetMethod("ConsumeShieldForAttack");Assert.IsNotNull(method);
        var receipt=method.Invoke(v,new object[]{tick,budget,new Func<CombatShieldSource,bool>(s=>s.EntityId!="shared")});
        return (int)receipt.GetType().GetProperty("Consumed").GetValue(receipt);
    }
    static CombatVitals Owner(){var v=new CombatVitals(10000,10000);v.Shield(A,100,20,0);v.Shield(B,200,50,0);return v;}
    [Test]public void AttackThenDamageAndDamageThenAttackUseActualRemainingInOrder()
    {
        var a=Owner();Assert.AreEqual(60,Consume(a,1,1000));var hit=a.Damage(50,1,false);Assert.AreEqual(50,hit.Absorbed);Assert.AreEqual(190,a.ShieldRemaining(1));Assert.AreEqual(10000,a.Hp);
        var b=Owner();b.Damage(50,1,false);Assert.AreEqual(50,Consume(b,1,1000));Assert.AreEqual(200,b.ShieldRemaining(1));
    }
    [Test]public void ConsumptionExcludesUnapprovedSourceAndExpiredLayersAndCapsAtOwnHp()
    {
        var v=new CombatVitals(1000,10000);v.Shield(A,600,10,0);v.Shield(Shared,400,20,0);
        Assert.AreEqual(50,Consume(v,1,1000));Assert.AreEqual(950,v.ShieldRemaining(1));
        Assert.AreEqual(0,Consume(v,10,1000));Assert.AreEqual(400,v.ShieldRemaining(10));Assert.AreEqual(1000,v.Hp);
    }
    [Test]public void RestoreKeepsConsumedRemainingInsteadOfReissuingShield()
    {
        var v=Owner();Assert.AreEqual(60,Consume(v,1,1000));
        var capture=typeof(CombatVitals).GetMethod("CaptureLocal",BindingFlags.Instance|BindingFlags.NonPublic);
        var saved=(JObject)capture.Invoke(v,null);var restored=new CombatVitals(10000,10000);
        typeof(CombatVitals).GetMethod("RestoreLocal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(restored,new object[]{saved,1L});
        Assert.AreEqual(240,restored.ShieldRemaining(1));Assert.AreEqual(40,(int)saved["layers"][0]["remaining"]);
        Assert.AreEqual(50,restored.Damage(50,1,false).Absorbed);Assert.AreEqual(190,restored.ShieldRemaining(1));
    }
}
