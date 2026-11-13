using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatDamageRouterTests
    {
        static CombatShieldSource Shield(string id)=>new CombatShieldSource(id,"shield",0);
        static CombatDamageRouter Router(CombatVitals train,CombatVitals car,int capacity=32)=>new CombatDamageRouter("train",train,new Dictionary<string,CombatVitals>{{"car",car}},capacity);
        [Test] public void ActualLossFloorsTransferAndUsesExistingHpOwners(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var r=Router(t,c).ApplyCarriageDamage("hit","car",101,1,false);Assert.IsTrue(r.Applied);Assert.AreEqual(101,r.Receipt.TargetHpLoss);Assert.AreEqual(30,r.Receipt.TransferToTrain);Assert.AreEqual(399,c.Hp);Assert.AreEqual(970,t.Hp);}
        [Test] public void OverkillAndLateHitsTransferOnlyActualLoss(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var m=Router(t,c);m.ApplyCarriageDamage("first","car",480,1,false);var d=m.ApplyCarriageDamage("down","car",100,2,false).Receipt;Assert.AreEqual(20,d.TargetHpLoss);Assert.AreEqual(6,d.TransferToTrain);Assert.IsTrue(d.CarriageDown);var late=m.ApplyCarriageDamage("late","car",100,3,false).Receipt;Assert.AreEqual(0,late.TargetHpLoss);Assert.IsFalse(late.CarriageDown);Assert.AreEqual(850,t.Hp);}
        [Test] public void CarShieldAppliedOnceAndTrainShieldPreserved(){var t=new CombatVitals(1000,10000);var c=new CombatVitals(500,10000);t.Shield(Shield("train"),200,100,0);c.Shield(Shield("car"),80,100,0);var r=Router(t,c).ApplyCarriageDamage("hit","car",180,1,false).Receipt;Assert.AreEqual(80,r.Absorbed);Assert.AreEqual(100,r.TargetHpLoss);Assert.AreEqual(30,r.TrainHpLoss);Assert.AreEqual(970,t.Hp);Assert.AreEqual(200,t.ShieldRemaining(1));}
        [Test] public void FullAbsorptionAndProtectionTransferZero(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,10000);var m=Router(t,c);c.Shield(Shield("car"),100,10,0);Assert.AreEqual(0,m.ApplyCarriageDamage("protected","car",200,1,true).Receipt.TargetHpLoss);Assert.AreEqual(100,c.ShieldRemaining(1));var r=m.ApplyCarriageDamage("absorbed","car",100,2,false).Receipt;Assert.AreEqual(100,r.Absorbed);Assert.AreEqual(0,r.TransferToTrain);Assert.AreEqual(1000,t.Hp);}
        [Test] public void DirectTrainDamageRetainsShieldAndProtection(){var t=new CombatVitals(1000,10000);var c=new CombatVitals(500,0);var m=Router(t,c);t.Shield(Shield("train"),80,10,0);Assert.AreEqual(0,m.ApplyDirectTrainDamage("protected",180,1,true).Receipt.TrainHpLoss);Assert.AreEqual(80,t.ShieldRemaining(1));var r=m.ApplyDirectTrainDamage("direct",180,2,false).Receipt;Assert.AreEqual(80,r.Absorbed);Assert.AreEqual(100,r.TrainHpLoss);Assert.AreEqual(0,r.TransferToTrain);Assert.AreEqual(900,t.Hp);Assert.AreEqual(500,c.Hp);}
        [Test] public void ReplayReturnsOriginalReceiptWithoutSecondTransition(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var m=Router(t,c);var first=m.ApplyCarriageDamage("down","car",500,1,false);m.ApplyDirectTrainDamage("later",20,2,false);var retry=m.ApplyCarriageDamage("down","car",500,1,false);Assert.IsFalse(retry.Applied);Assert.AreSame(first.Receipt,retry.Receipt);Assert.IsTrue(retry.Receipt.CarriageDown);Assert.AreEqual(830,t.Hp);Assert.AreEqual(0,c.Hp);}
        [Test] public void PayloadAndRouteConflictsRejectWithoutMutation(){var t=new CombatVitals(1000,10000);var c=new CombatVitals(500,10000);var m=Router(t,c);m.ApplyCarriageDamage("event","car",10,1,false);t.Shield(Shield("train"),100,100,1);c.Shield(Shield("car"),100,100,1);Assert.Throws<InvalidOperationException>(()=>m.ApplyCarriageDamage("event","car",11,1,false));Assert.Throws<InvalidOperationException>(()=>m.ApplyDirectTrainDamage("event",10,1,false));Assert.AreEqual(997,t.Hp);Assert.AreEqual(490,c.Hp);Assert.AreEqual(100,t.ShieldRemaining(1));Assert.AreEqual(100,c.ShieldRemaining(1));}
        [Test] public void StaleTrainClockRejectsBeforeCarMutation(){var t=new CombatVitals(1000,10000);var c=new CombatVitals(500,10000);var m=Router(t,c);t.Shield(Shield("train"),100,100,10);c.Shield(Shield("car"),100,100,0);Assert.Throws<ArgumentOutOfRangeException>(()=>m.ApplyCarriageDamage("event","car",200,5,false));Assert.AreEqual(500,c.Hp);Assert.AreEqual(100,c.ShieldRemaining(0));Assert.AreEqual(1000,t.Hp);Assert.IsTrue(m.ApplyCarriageDamage("event","car",200,10,false).Applied);}
        [Test] public void StaleCarClockRejectsBeforeTrainShieldExpiry(){var t=new CombatVitals(1000,10000);var c=new CombatVitals(500,0);var m=Router(t,c);t.Shield(Shield("train"),100,5,0);c.Damage(0,10,false);Assert.Throws<ArgumentOutOfRangeException>(()=>m.ApplyCarriageDamage("event","car",200,5,false));Assert.AreEqual(100,t.ShieldRemaining(0));Assert.AreEqual(500,c.Hp);}
        [Test] public void FatalTransferCannotEmitRecoveryTransition(){var t=new CombatVitals(1,0);var c=new CombatVitals(4,0);var r=Router(t,c).ApplyCarriageDamage("fatal","car",4,1,false).Receipt;Assert.AreEqual(4,r.TargetHpLoss);Assert.AreEqual(1,r.TransferToTrain);Assert.AreEqual(1,r.TrainHpLoss);Assert.IsTrue(r.TrainDefeated);Assert.IsFalse(r.TerminalOnEntry);Assert.IsFalse(r.CarriageDown);Assert.AreEqual(0,t.Hp);Assert.AreEqual(0,c.Hp);Assert.AreEqual(0,t.RestoreHp(100,10000));}
        [Test] public void SurvivingByOneEmitsOneEligibleTransition(){var t=new CombatVitals(2,0);var c=new CombatVitals(4,0);var m=Router(t,c);var first=m.ApplyCarriageDamage("down","car",4,1,false);Assert.IsTrue(first.Applied&&first.Receipt.CarriageDown);Assert.IsFalse(first.Receipt.TrainDefeated);Assert.AreEqual(1,t.Hp);Assert.AreEqual(0,c.Hp);Assert.IsFalse(m.ApplyCarriageDamage("down","car",4,1,false).Applied);Assert.AreEqual(0,c.Hp);}
        [Test] public void AlreadyDefeatedTrainDoesNotDamageLivingCar(){var t=new CombatVitals(1,0);var c=new CombatVitals(4,10000);var m=Router(t,c);m.ApplyDirectTrainDamage("fatal",1,1,false);c.Shield(Shield("car"),4,10,0);var r=m.ApplyCarriageDamage("late","car",4,2,false).Receipt;Assert.IsTrue(r.TerminalOnEntry);Assert.IsTrue(r.TrainDefeated);Assert.IsFalse(r.CarriageDown);Assert.AreEqual(0,r.TargetHpLoss);Assert.AreEqual(4,c.Hp);Assert.AreEqual(4,c.ShieldRemaining(0));}
        [Test] public void IntMaximumTransferExactAndCapped(){var t=new CombatVitals(1,0);var c=new CombatVitals(int.MaxValue,0);var r=Router(t,c).ApplyCarriageDamage("max","car",int.MaxValue,0,false).Receipt;Assert.AreEqual(644245094,r.TransferToTrain);Assert.AreEqual(1,r.TrainHpLoss);Assert.IsFalse(r.CarriageDown);}
        [Test] public void FullLedgerRejectsNewEventButRetainsReplay(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var m=Router(t,c,1);m.ApplyCarriageDamage("one","car",10,1,false);Assert.Throws<InvalidOperationException>(()=>m.ApplyCarriageDamage("two","car",10,2,false));Assert.IsFalse(m.ApplyCarriageDamage("one","car",10,1,false).Applied);Assert.AreEqual(490,c.Hp);Assert.AreEqual(997,t.Hp);}
        [Test] public void InvalidInputDoesNotConsumeId(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var m=Router(t,c);Assert.Throws<ArgumentOutOfRangeException>(()=>m.ApplyCarriageDamage("event","car",-1,0,false));Assert.Throws<ArgumentOutOfRangeException>(()=>m.ApplyCarriageDamage("event","car",1,-1,false));Assert.Throws<ArgumentException>(()=>m.ApplyCarriageDamage("event","missing",1,0,false));Assert.Throws<ArgumentException>(()=>m.ApplyDirectTrainDamage(" ",1,0,false));Assert.IsTrue(m.ApplyCarriageDamage("event","car",1,0,false).Applied);Assert.AreEqual(499,c.Hp);}
        [Test] public void BindingCopyPreventsRetargetAndRejectsAliases(){var t=new CombatVitals(1000,0);var c=new CombatVitals(500,0);var other=new CombatVitals(500,0);var bindings=new Dictionary<string,CombatVitals>{{"car",c}};var m=new CombatDamageRouter("train",t,bindings,10);bindings["car"]=other;m.ApplyCarriageDamage("event","car",10,1,false);Assert.AreEqual(490,c.Hp);Assert.AreEqual(500,other.Hp);Assert.Throws<ArgumentException>(()=>new CombatDamageRouter("train",t,new Dictionary<string,CombatVitals>{{"car",t}},10));Assert.Throws<ArgumentException>(()=>new CombatDamageRouter("train",t,new Dictionary<string,CombatVitals>{{"a",c},{"b",c}},10));}
        [Test] public void ExistingDirectAttackAppliesTrainArmorThenShieldExactlyOnce()
        {
            var train=new BasicCombatActor("train",false,true,false,0,100,0,5000,10,0,0,BasicCombatTarget.EnemyAny,4000);
            var enemy=new BasicCombatActor("enemy",true,false,false,0,100,100,0,10,0,0,BasicCombatTarget.Train,0);
            var simulation=new BasicCombatSimulation("TEST_ONLY_direct_defense",500000,1u,new[]{train},new[]{new[]{enemy}});
            simulation.ApplyResolvedShield("train",Shield("operator"),30,10);
            simulation.Advance(500000,1);
            var hit=simulation.Events.Single(e=>e.Source=="enemy");
            Assert.AreEqual(30,hit.Absorbed);Assert.AreEqual(20,hit.HpLoss);Assert.AreEqual(80,simulation.GetHp("train"));
        }
        [Test] public void RetryCannotChangeTargetTickOrProtection()
        {
            var train=new CombatVitals(1000,0);var a=new CombatVitals(100,0);var b=new CombatVitals(100,0);
            var router=new CombatDamageRouter("train",train,new Dictionary<string,CombatVitals>{{"a",a},{"b",b}},8);
            router.ApplyCarriageDamage("event","a",10,1,false);
            Assert.Throws<InvalidOperationException>(()=>router.ApplyCarriageDamage("event","b",10,1,false));
            Assert.Throws<InvalidOperationException>(()=>router.ApplyCarriageDamage("event","a",10,2,false));
            Assert.Throws<InvalidOperationException>(()=>router.ApplyCarriageDamage("event","a",10,1,true));
            Assert.AreEqual(90,a.Hp);Assert.AreEqual(100,b.Hp);Assert.AreEqual(997,train.Hp);
        }
    }
}
