using System;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
public sealed class CombatCooldownTextTests {
 static string Format(long? value){var t=typeof(CombatCooldownWipe).Assembly.GetType("LastRail.Battle.CombatCooldownText");Assert.NotNull(t,"Shared product cooldown text is missing");return (string)t.GetMethod("Format").Invoke(null,new object[]{value});}
 [TestCase(0L,"")][TestCase(1L,"1초 미만")][TestCase(50L,"1초 미만")][TestCase(999L,"1초 미만")][TestCase(1000L,"1")][TestCase(1001L,"2")][TestCase(4000L,"4")][TestCase(long.MaxValue,"9223372036854776")]
 public void ExactOwnerRemainingUsesApprovedDisplayOnly(long value,string expected){Assert.AreEqual(expected,Format(value));}
 [Test] public void MissingRemainingIsNotZero(){Assert.AreEqual("미확인",Format(null));}
 [Test] public void NegativeRemainingIsNotReady(){var error=Assert.Throws<TargetInvocationException>(()=>Format(-1));Assert.IsInstanceOf<ArgumentOutOfRangeException>(error.InnerException);}
}
