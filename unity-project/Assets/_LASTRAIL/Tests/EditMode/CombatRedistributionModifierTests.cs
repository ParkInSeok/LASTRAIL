using System;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
public class CombatRedistributionModifierTests
{
    [Test]public void OddExcessKeepsFractionThroughReceivingModifier()
    {
        var recipient=new CombatVitals(100,0);recipient.Damage(10,0,false);
        var parent=new CombatOverhealParent(new CombatVitals(100,0),"odd",100,true,false,true);
        var result=parent.AllocateTickWithRecipientHealing(1,new[]{new CombatVitals(100,0).ResolveLivingHeal(3,true,true)},recipient,true,7500,true);
        Assert.AreEqual(91,recipient.Hp);Assert.AreEqual(1,result.RedistributedHp);Assert.AreEqual(1,result.ShieldBudget);
    }
    [TestCase(5000,true,250,375)][TestCase(20000,true,500,250)][TestCase(10000,false,0,500)]
    public void SecondRecipientModifierIsAppliedOnceAndCannotCreateTransferBudget(int multiplier,bool allowed,int gain,int shield)
    {
        var healer=new CombatVitals(2000,0);var original=new CombatVitals(2000,0);var recipient=new CombatVitals(2000,0);recipient.Damage(1000,0,false);
        var parent=new CombatOverhealParent(healer,"parent",2000,true,false,true);
        var method=typeof(CombatOverhealParent).GetMethod("AllocateTickWithRecipientHealing");Assert.IsNotNull(method);
        var allocation=(CombatOverhealAllocation)method.Invoke(parent,new object[]{1L,new[]{original.ResolveLivingHeal(1000,true,true)},recipient,true,multiplier,allowed});
        Assert.AreEqual(1000+gain,recipient.Hp);Assert.AreEqual(gain,allocation.RedistributedHp);Assert.AreEqual(shield,allocation.ShieldBudget);
    }
}
