using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LastRail.Battle {
 public sealed class CombatLivingHealResult {
  internal CombatVitals Target {get;}
  public int ActualHpGain {get;}
  public int Excess {get;}
  internal CombatLivingHealResult(CombatVitals target,int actual,int excess){Target=target;ActualHpGain=actual;Excess=excess;}
 }
 // Historical capped policy only; preserved for old definition/snapshot evidence.
 // New approved definitions use CombatAttributedLifesteal, not this window.
 // A per-actor budget, composed with the existing HP owner. The caller supplies
 // combat time and invokes once for a journal-accepted original body opportunity.
 // It is not a grant/ID/authority adapter; derived hits must not be marked eligible.
 public sealed class CombatLifestealWindow {
  readonly CombatVitals owner;readonly string scope;readonly long second;
  readonly List<(long Tick,int Gain)> gains=new List<(long,int)>();long observed;
  public sealed class State {
   internal readonly string Scope;internal readonly long Second,Observed;internal readonly (long Tick,int Gain)[] Gains;
   internal State(string scope,long second,long observed,IEnumerable<(long Tick,int Gain)> gains){Scope=scope;Second=second;Observed=observed;Gains=gains.ToArray();}
  }
  public CombatLifestealWindow(CombatVitals owner,string actorScope,long combatTicksPerSecond){
   this.owner=owner??throw new ArgumentNullException(nameof(owner));if(string.IsNullOrWhiteSpace(actorScope)||combatTicksPerSecond<=0)throw new ArgumentException();scope=actorScope;second=combatTicksPerSecond;
  }
  public int Apply(long tick,IReadOnlyList<CombatDamageResult> originalBodyHits,bool eligibleDirectOwnBody){
   if(tick<observed||originalBodyHits==null)throw new ArgumentException("Invalid recovery observation");
   long damage=0;foreach(var hit in originalBodyHits)damage=checked(damage+hit.HpLoss);
   // Fractional damage recovery is resolved after summing this original body's splits.
   long request=checked(damage*12)/100;
   gains.RemoveAll(x=>tick-x.Tick>=second);observed=tick;
   if(!eligibleDirectOwnBody||owner.Hp==0)return 0;
   long remaining=Math.Max(0,(long)owner.MaxHp*8/100-gains.Sum(x=>(long)x.Gain));
   int actual=owner.RestoreHp((int)Math.Min(int.MaxValue,Math.Min(request,remaining)),10000);
   if(actual>0){if(gains.Count>0&&gains[gains.Count-1].Tick==tick){var old=gains[gains.Count-1];gains[gains.Count-1]=(tick,checked(old.Gain+actual));}else gains.Add((tick,actual));}
   return actual;
  }
  public State Capture()=>new State(scope,second,observed,gains);
  public static CombatLifestealWindow Restore(CombatVitals owner,string actorScope,State state){
   if(state==null||state.Scope!=actorScope)throw new ArgumentException("Recovery scope mismatch");
   var result=new CombatLifestealWindow(owner,actorScope,state.Second);result.observed=state.Observed;result.gains.AddRange(state.Gains);return result;
  }
 }
 // Current uncapped policy. One original accepted damage event, attributed by
 // the authority to this beneficiary, enters once through its existing journal.
 // No clock, source-kind filter, cooldown or historical recovery budget exists.
 public sealed class CombatAttributedLifesteal {
  readonly CombatVitals beneficiary;
  public CombatAttributedLifesteal(CombatVitals beneficiary){this.beneficiary=beneficiary??throw new ArgumentNullException(nameof(beneficiary));}
  internal static int BoundedRecovery(BigInteger damage,int healingMultiplierBps,int hp,int maximumHp)
   =>(int)BigInteger.Min(damage*12*healingMultiplierBps/1000000,maximumHp-hp);
  public CombatLivingHealResult Apply(IReadOnlyList<CombatDamageResult> attributedHits,int healingMultiplierBps,bool healingAllowed){
   if(attributedHits==null||healingMultiplierBps<0)throw new ArgumentException("Invalid attributed recovery input");
   int actual=0;
   // Each list entry is an independently confirmed HP-loss event, never a parent
   // summary. Floor AFTER its modifier, before the next event; no fractional bank.
   // Callers with changing eligibility/modifiers resolve each event at its time.
   foreach(var hit in attributedHits){
    int bounded=BoundedRecovery(hit.HpLoss,healingMultiplierBps,beneficiary.Hp,beneficiary.MaxHp);
    actual=checked(actual+beneficiary.ResolveLivingHeal(bounded,false,healingAllowed).ActualHpGain);
   }
   // Only presentation may sum actual gains; no overheal conversion is issued.
   return new CombatLivingHealResult(beneficiary,actual,0);
  }
 }
 public sealed class CombatOverhealAllocation {
  public int RedistributedHp {get;}
  public long ShieldBudget {get;}
  public long AttackBudget {get;}
  internal CombatOverhealAllocation(int heal,long shield,long attack){RedistributedHp=heal;ShieldBudget=shield;AttackBudget=attack;}
 }
 // One original healing parent's budget. No projectile, shield expiry, target
 // geometry or ownership is invented here: verified consumers apply the result.
 public sealed class CombatOverhealParent {
  readonly CombatVitals healer;readonly string scope;readonly bool shield,reflux;
  bool redistributionAvailable;long remaining,lastTick=-1;
  public long RemainingAttackBudget=>remaining;
  public sealed class State {
   internal readonly string Scope;internal readonly bool Shield,Reflux,Redistribution;internal readonly long Remaining,Tick;
   internal State(CombatOverhealParent p){Scope=p.scope;Shield=p.shield;Reflux=p.reflux;Redistribution=p.redistributionAvailable;Remaining=p.remaining;Tick=p.lastTick;}
  }
  public CombatOverhealParent(CombatVitals healer,string parentScope,int attackAtCast,bool hasShieldConversion,bool hasReflux,bool hasRedistribution){
   this.healer=healer??throw new ArgumentNullException(nameof(healer));if(string.IsNullOrWhiteSpace(parentScope)||attackAtCast<0)throw new ArgumentException();scope=parentScope;shield=hasShieldConversion;reflux=hasReflux;redistributionAvailable=hasRedistribution;remaining=(long)attackAtCast*150/100;
  }
  public CombatOverhealAllocation AllocateTick(long tick,IReadOnlyList<CombatLivingHealResult> originalHeals,CombatVitals eligibleOtherRecipient,bool hasLegalAttackTarget){
   return AllocateTickWithRecipientHealing(tick,originalHeals,eligibleOtherRecipient,hasLegalAttackTarget,10000,true);
  }
  // The original effect owner supplies only the second recipient's eligible
  // receiving-heal modifier. Caster modifiers are already in the excess budget.
  public CombatOverhealAllocation AllocateTickWithRecipientHealing(long tick,IReadOnlyList<CombatLivingHealResult> originalHeals,CombatVitals eligibleOtherRecipient,bool hasLegalAttackTarget,int recipientHealingMultiplierBps,bool recipientHealingAllowed){
   if(recipientHealingMultiplierBps<0)throw new ArgumentOutOfRangeException(nameof(recipientHealingMultiplierBps));
   if(tick<0||tick<=lastTick||originalHeals==null||originalHeals.Any(h=>h==null))throw new ArgumentException("One allocation per original parent/tick required");
   if(eligibleOtherRecipient!=null&&originalHeals.Any(h=>ReferenceEquals(h.Target,eligibleOtherRecipient)))throw new ArgumentException("Redistribution requires another recipient");
   long excess=0;foreach(var h in originalHeals)excess=checked(excess+h.Excess);
   // Bound arithmetic before any HP mutation. Long budgets preserve a fractional
   // half until each conversion's final integer resolution (25% / 30%).
   checked{_ = excess*60;}
   int transferred=0;
   if(redistributionAvailable&&eligibleOtherRecipient!=null&&eligibleOtherRecipient.Hp>0&&recipientHealingAllowed){
    long allocation=excess/2;
    // Preserve the half-share fraction through the receiving modifier; floor only the final HP.
    var corrected=(BigInteger)excess*recipientHealingMultiplierBps/20000;
    int request=(int)BigInteger.Min(int.MaxValue,BigInteger.Min(allocation,corrected));
    transferred=eligibleOtherRecipient.ResolveLivingHeal(request,false,true).ActualHpGain;if(transferred>0)redistributionAvailable=false;
   }
   excess-=transferred;
   long protection=shield?excess*(reflux?25:50)/100:0;
   long planned=reflux?Math.Min(remaining,excess*(shield?30:60)/100):0;
   remaining-=planned;lastTick=tick;
   // No target/down caster: discard this allocation, never bank a shot for later.
   return new CombatOverhealAllocation(transferred,protection,hasLegalAttackTarget&&healer.Hp>0?planned:0);
  }
  public State Capture()=>new State(this);
  public static CombatOverhealParent Restore(CombatVitals healer,string parentScope,State state){
   if(state==null||state.Scope!=parentScope)throw new ArgumentException("Healing parent mismatch");
   var result=new CombatOverhealParent(healer,parentScope,0,state.Shield,state.Reflux,state.Redistribution);result.remaining=state.Remaining;result.lastTick=state.Tick;return result;
  }
 }
}
