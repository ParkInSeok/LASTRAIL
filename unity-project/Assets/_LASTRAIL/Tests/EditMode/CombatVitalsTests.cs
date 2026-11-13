using LastRail.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatVitalsTests
    {
        static CombatShieldSource Key(string id)=>new CombatShieldSource(id,"skill",0);
        [Test] public void DamageConsumesShieldBeforeHpAndExpiresAtBoundary()
        {
            var v=new CombatVitals(100,4000); v.Shield(Key("a"),30,5,0);
            var hit=v.Damage(40,1,false); Assert.AreEqual(30,hit.Absorbed);Assert.AreEqual(10,hit.HpLoss);Assert.AreEqual(90,v.Hp);
            v.Shield(Key("a"),30,5,1);hit=v.Damage(10,5,false);Assert.AreEqual(0,hit.Absorbed);Assert.AreEqual(80,v.Hp);
        }
        [Test] public void WeakerSameSourceDoesNotExtendAndDifferentSourcesShareCap()
        {
            var v=new CombatVitals(100,4000);v.Shield(Key("a"),30,5,0);v.Shield(Key("b"),30,10,0);
            Assert.AreEqual(40,v.ShieldRemaining(0));v.Shield(Key("a"),20,20,1);
            Assert.AreEqual(10,v.ShieldRemaining(5));
        }
        [Test] public void EarliestExpiryIsConsumedFirstAndProtectionPreservesShield()
        {
            var v=new CombatVitals(100,4000);v.Shield(Key("a"),20,10,0);v.Shield(Key("b"),20,5,0);
            Assert.AreEqual(0,v.Damage(100,1,true).HpLoss);Assert.AreEqual(40,v.ShieldRemaining(1));
            v.Damage(15,2,false);Assert.AreEqual(20,v.ShieldRemaining(5));
        }
        [Test] public void HealingRepairCapsAndDeathNeverRevives()
        {
            var v=new CombatVitals(100,4000);v.Damage(70,0,false);Assert.AreEqual(20,v.RestoreHp(90,2000));Assert.AreEqual(50,v.Hp);
            Assert.AreEqual(50,v.RestoreHp(1000,10000));v.Damage(100,1,false);
            Assert.AreEqual(0,v.RestoreHp(100,10000));Assert.AreEqual(0,v.Shield(Key("a"),30,10,2));Assert.AreEqual(0,v.Hp);
        }
    }
}
