using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    public sealed class CombatShieldSource
    {
        public string EntityId { get; }
        public string AbilityId { get; }
        public int EffectIndex { get; }
        public CombatShieldSource(string entityId,string abilityId,int effectIndex)
        {
            if(string.IsNullOrWhiteSpace(entityId)||string.IsNullOrWhiteSpace(abilityId)||effectIndex<0)throw new ArgumentException();
            EntityId=entityId;AbilityId=abilityId;EffectIndex=effectIndex;
        }
        internal bool Same(CombatShieldSource other)=>EntityId==other.EntityId&&AbilityId==other.AbilityId&&EffectIndex==other.EffectIndex;
    }
    public readonly struct CombatDamageResult
    {
        public int Absorbed { get; }
        public int HpLoss { get; }
        internal CombatDamageResult(int absorbed,int hpLoss){Absorbed=absorbed;HpLoss=hpLoss;}
    }
    public sealed class CombatShieldConsumption
    {
        public int Consumed { get; }
        public int AdditionalAttackBudget { get; }
        public IReadOnlyList<CombatShieldLayerAmount> Layers { get; }
        internal CombatShieldConsumption(int consumed,int budget,IEnumerable<CombatShieldLayerAmount> layers)
        {Consumed=consumed;AdditionalAttackBudget=budget;Layers=Array.AsReadOnly(layers.ToArray());}
    }
    public sealed class CombatShieldLayerAmount
    {
        public CombatShieldSource Source { get; }
        public int Amount { get; }
        public long ExpiryTick { get; }
        internal CombatShieldLayerAmount(CombatShieldSource source,int amount,long expiry)
        {Source=source;Amount=amount;ExpiryTick=expiry;}
    }
    // Resolved integer magnitudes enter here after coefficient/category/armor calculation.
    // Each instance is one target, so the source key's target identity is implicit.
    public sealed class CombatVitals
    {
        sealed class Layer { internal CombatShieldSource Key;internal int Remaining;internal long Expiry;internal bool Transferred; }
        readonly List<Layer> layers=new List<Layer>();
        readonly int shieldCap;
        long lastTick;
        readonly bool approvedHpPolicy;
        int anchorHp,anchorMaxHp;
        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public CombatVitals(int maxHp,int shieldCapBps):this(maxHp,shieldCapBps,false){}
        // Transient transaction candidate; it never replaces the authoritative owner's identity.
        internal CombatVitals Copy()=>new CombatVitals(this);
        CombatVitals(CombatVitals source){shieldCap=source.shieldCap;approvedHpPolicy=source.approvedHpPolicy;MaxHp=source.MaxHp;RestoreLocal(source.CaptureLocal(),long.MaxValue);}
        // Explicit local policy; a server/START adapter must select this only for a supported definition.
        public CombatVitals(int maxHp,int shieldCapBps,bool useApprovedHpPolicy)
        {
            if(maxHp<=0||shieldCapBps<0||shieldCapBps>10000)throw new ArgumentOutOfRangeException();
            approvedHpPolicy=useApprovedHpPolicy;MaxHp=maxHp;Hp=maxHp;anchorHp=maxHp;anchorMaxHp=maxHp;
            shieldCap=approvedHpPolicy?int.MaxValue:(int)((long)maxHp*shieldCapBps/10000);
        }
        void AnchorActualChange(int changed){if(approvedHpPolicy&&changed>0){anchorHp=Hp;anchorMaxHp=MaxHp;}}
        static int RebasedHp(int hp,int max,int next)=>hp==0?0:(int)Math.Min(next,Math.Max(1L,(long)hp*next/max));
        // Receives one resolved final stat, not an equipment command or heal event.
        public void RebaseMaxHp(int finalMaxHp)
        {
            if(!approvedHpPolicy)throw new InvalidOperationException("Original HP policy does not support rebasing.");
            if(finalMaxHp<=0)throw new ArgumentOutOfRangeException(nameof(finalMaxHp));
            var next=RebasedHp(anchorHp,anchorMaxHp,finalMaxHp);MaxHp=finalMaxHp;Hp=next;
        }
        internal void ValidateDamageTick(long tick)
        {
            if(tick<lastTick)throw new ArgumentOutOfRangeException(nameof(tick),"Effect time cannot move backwards.");
        }
        void Expire(long tick)
        {
            ValidateDamageTick(tick);
            lastTick=tick;layers.RemoveAll(x=>x.Expiry<=tick||x.Remaining==0);
        }
        public int ShieldRemaining(long tick){Expire(tick);return layers.Sum(x=>x.Remaining);}
        IEnumerable<Layer> OrderedLayers()=>layers.OrderBy(x=>x.Expiry).ThenBy(x=>x.Key.EntityId,StringComparer.Ordinal).ThenBy(x=>x.Key.AbilityId,StringComparer.Ordinal).ThenBy(x=>x.Key.EffectIndex);
        public IReadOnlyList<CombatShieldLayerAmount> ReadShieldLayers(long tick)
        {Expire(tick);return Array.AsReadOnly(OrderedLayers().Select(x=>new CombatShieldLayerAmount(x.Key,x.Remaining,x.Expiry)).ToArray());}
        // Called once by the accepted original attack owner, inside its checkpoint
        // transaction. Eligibility comes from the original PERSONAL source contract;
        // no shared/derived layer is made eligible by this low-level operation.
        public CombatShieldConsumption ConsumeShieldForAttack(long tick,int originalAttackBudget,Func<CombatShieldSource,bool> eligiblePersonalSource)
        {
            if(originalAttackBudget<0||eligiblePersonalSource==null)throw new ArgumentException("Original attack budget and source eligibility required");
            ValidateDamageTick(tick);
            // Resolve external eligibility before mutating even expiry state.
            var eligible=OrderedLayers().Where(x=>x.Expiry>tick&&x.Remaining>0&&eligiblePersonalSource(x.Key)).ToArray();
            long total=eligible.Sum(x=>(long)x.Remaining);
            int request=Hp==0?0:(int)Math.Min(total*20/100,(long)MaxHp*5/100);
            Expire(tick);int remaining=request;var used=new List<CombatShieldLayerAmount>();
            foreach(var layer in eligible){int amount=Math.Min(remaining,layer.Remaining);if(amount==0)break;layer.Remaining-=amount;remaining-=amount;used.Add(new CombatShieldLayerAmount(layer.Key,amount,layer.Expiry));}
            return new CombatShieldConsumption(request,Math.Min(request,originalAttackBudget/2),used);
        }
        public int Shield(CombatShieldSource source,int magnitude,long expiryTick,long tick)
        {
            if(source==null||magnitude<0||tick<0||expiryTick<=tick)throw new ArgumentException();
            Expire(tick);if(Hp==0)return 0;
            var existing=layers.SingleOrDefault(x=>!x.Transferred&&x.Key.Same(source));
            var others=layers.Where(x=>x!=existing).Sum(x=>x.Remaining);
            var candidate=Math.Min(magnitude,Math.Max(0,shieldCap-others));
            if(candidate==0||(existing!=null&&candidate<existing.Remaining))return 0;
            if(existing==null){existing=new Layer{Key=source};layers.Add(existing);}
            existing.Remaining=candidate;existing.Expiry=expiryTick;return candidate;
        }
        // Original O19 command owner invokes once in its two-actor checkpoint
        // transaction. The predicate is the original transferable-personal set,
        // not the O15 consumption set. No HP/healing or shield-creation event.
        public int TransferPersonalShield(CombatVitals recipient,long tick,Func<CombatShieldSource,bool> eligibleTransferSource)
        {
            if(recipient==null||ReferenceEquals(this,recipient)||eligibleTransferSource==null)throw new ArgumentException("Distinct original owners and eligibility required");
            ValidateDamageTick(tick);recipient.ValidateDamageTick(tick);
            bool Eligible(Layer x)=>x.Expiry>tick&&x.Remaining>0&&x.Key.AbilityId!="O02"&&eligibleTransferSource(x.Key);
            // All external filtering/validation precedes either owner's mutation.
            var donors=OrderedLayers().Where(Eligible).ToArray();
            long receiving=recipient.layers.Where(Eligible).Sum(x=>(long)x.Remaining);
            long allReceiving=recipient.layers.Where(x=>x.Expiry>tick&&x.Remaining>0).Sum(x=>(long)x.Remaining);
            long room=Math.Min(Math.Max(0,(long)recipient.MaxHp*25/100-receiving),Math.Max(0,(long)recipient.shieldCap-allReceiving));
            int amount=Hp==0||recipient.Hp==0?0:(int)Math.Min(donors.Sum(x=>(long)x.Remaining),Math.Min(room,(long)recipient.MaxHp*15/100));
            var plan=new List<(Layer Donor,Layer Recipient,int Amount,bool New)>();int remaining=amount;
            foreach(var donor in donors){if(remaining==0)break;int take=Math.Min(remaining,donor.Remaining);
                var existing=recipient.layers.SingleOrDefault(x=>x.Transferred&&x.Remaining>0&&x.Key.Same(donor.Key)&&x.Expiry==donor.Expiry&&x.Expiry>tick);
                if(existing==null)existing=plan.Where(x=>x.Recipient.Key.Same(donor.Key)&&x.Recipient.Expiry==donor.Expiry).Select(x=>x.Recipient).FirstOrDefault();
                bool created=existing==null;existing=existing??new Layer{Key=donor.Key,Expiry=donor.Expiry,Transferred=true};
                plan.Add((donor,existing,take,created));remaining-=take;
            }
            if(recipient.layers.Count(x=>x.Expiry>tick&&x.Remaining>0)+plan.Count(x=>x.New)>256)throw new InvalidOperationException("Recipient shield capacity exceeded");
            Expire(tick);recipient.Expire(tick);
            foreach(var step in plan){step.Donor.Remaining-=step.Amount;if(step.New)recipient.layers.Add(step.Recipient);step.Recipient.Remaining+=step.Amount;}
            return amount;
        }
        public CombatDamageResult Damage(int magnitude,long tick,bool protectedFromDamage)
        {
            if(magnitude<0)throw new ArgumentOutOfRangeException(nameof(magnitude));
            Expire(tick);if(Hp==0||protectedFromDamage)return new CombatDamageResult(0,0);
            var remaining=magnitude;
            foreach(var layer in OrderedLayers())
            {
                var used=Math.Min(remaining,layer.Remaining);layer.Remaining-=used;remaining-=used;if(remaining==0)break;
            }
            var loss=Math.Min(Hp,remaining);Hp-=loss;AnchorActualChange(loss);return new CombatDamageResult(magnitude-remaining,loss);
        }
        // Only the damage domain routes actual carriage loss here. Armor and shield
        // already participated at the original target; remaining train shield is preserved.
        internal int ApplyTransferredHpLoss(int magnitude,long tick)
        {
            if(magnitude<0)throw new ArgumentOutOfRangeException(nameof(magnitude));
            Expire(tick);
            var loss=Math.Min(Hp,magnitude);Hp-=loss;AnchorActualChange(loss);return loss;
        }
        // Heal uses explicit10000 cap; train repair uses the profile's per-effect cap.
        // The source adapter decides eligibility from the original skill contract,
        // never a display label. Rejected/down/derived healing produces no excess.
        public CombatLivingHealResult ResolveLivingHeal(int magnitude,bool originalLivingSkill,bool allowed)
        {
            if(magnitude<0)throw new ArgumentOutOfRangeException(nameof(magnitude));
            if(!allowed||Hp==0)return new CombatLivingHealResult(this,0,0);
            int actual=RestoreHp(magnitude,10000);
            return new CombatLivingHealResult(this,actual,originalLivingSkill?magnitude-actual:0);
        }
        public int RestoreHp(int magnitude,int perEffectCapBps)
        {
            if(magnitude<0||perEffectCapBps<0||perEffectCapBps>10000)throw new ArgumentOutOfRangeException();
            if(Hp==0)return 0;
            var cap=(int)((long)MaxHp*perEffectCapBps/10000);
            var restored=Math.Min(MaxHp-Hp,Math.Min(magnitude,cap));Hp+=restored;AnchorActualChange(restored);return restored;
        }
        internal void RecoverFromDown(int recoveryHp)
        {
            if(Hp!=0||recoveryHp<=0||recoveryHp>MaxHp)throw new InvalidOperationException("Invalid scheduled recovery.");
            Hp=recoveryHp;AnchorActualChange(recoveryHp);
        }
        static JObject CaptureLayer(Layer x){var row=new JObject{["entityId"]=x.Key.EntityId,["abilityId"]=x.Key.AbilityId,["effectIndex"]=x.Key.EffectIndex,["remaining"]=x.Remaining,["expiry"]=x.Expiry};if(x.Transferred)row["transferred"]=true;return row;}
        JObject CaptureBase()=>new JObject{["hp"]=Hp,["lastTick"]=lastTick,["layers"]=new JArray(layers.Select(CaptureLayer))};
        internal JObject CaptureLocal(){var result=CaptureBase();if(approvedHpPolicy){result["schema"]="combat-vitals.rebase-v1";result["maxHp"]=MaxHp;result["anchorHp"]=anchorHp;result["anchorMaxHp"]=anchorMaxHp;}return result;}
        internal void RestoreLocal(JObject data,long tick)
        {
            if(data==null||!data.Properties().Select(x=>x.Name).OrderBy(x=>x).SequenceEqual((approvedHpPolicy?new[]{"hp","lastTick","layers","schema","maxHp","anchorHp","anchorMaxHp"}:new[]{"hp","lastTick","layers"}).OrderBy(x=>x))||data["hp"]?.Type!=JTokenType.Integer||data["lastTick"]?.Type!=JTokenType.Integer||!(data["layers"] is JArray saved))throw new ArgumentException("Invalid vitals.");
            int max=MaxHp,baseHp=anchorHp,baseMax=anchorMaxHp;
            if(approvedHpPolicy){
                if((string)data["schema"]!="combat-vitals.rebase-v1"||data["maxHp"]?.Type!=JTokenType.Integer||data["anchorHp"]?.Type!=JTokenType.Integer||data["anchorMaxHp"]?.Type!=JTokenType.Integer)throw new ArgumentException("Missing HP policy state.");
                max=(int)data["maxHp"];baseHp=(int)data["anchorHp"];baseMax=(int)data["anchorMaxHp"];
                if(max<=0||baseMax<=0||baseHp<0||baseHp>baseMax)throw new ArgumentException("Invalid HP anchor.");
            }
            var hp=(int)data["hp"];if(approvedHpPolicy&&hp!=RebasedHp(baseHp,baseMax,max))throw new ArgumentException("HP differs from preserved anchor.");
            var at=(long)data["lastTick"];if(hp<0||hp>max||at<0||at>tick||saved.Count>256)throw new ArgumentException("Invalid vitals bounds.");
            var restored=new List<Layer>();
            foreach(var token in saved)
            {
                if(!(token is JObject row)||!row.Properties().Select(x=>x.Name).OrderBy(x=>x).SequenceEqual((row["transferred"]==null?new[]{"entityId","abilityId","effectIndex","remaining","expiry"}:new[]{"entityId","abilityId","effectIndex","remaining","expiry","transferred"}).OrderBy(x=>x))||row["entityId"]?.Type!=JTokenType.String||row["abilityId"]?.Type!=JTokenType.String||row["effectIndex"]?.Type!=JTokenType.Integer||row["remaining"]?.Type!=JTokenType.Integer||row["expiry"]?.Type!=JTokenType.Integer)throw new ArgumentException();
                if(row["transferred"]!=null&&(row["transferred"].Type!=JTokenType.Boolean||!(bool)row["transferred"]))throw new ArgumentException();
                var layer=new Layer{Key=new CombatShieldSource((string)row["entityId"],(string)row["abilityId"],(int)row["effectIndex"]),Remaining=(int)row["remaining"],Expiry=(long)row["expiry"],Transferred=row["transferred"]!=null};
                if(layer.Remaining<0||layer.Expiry<0||restored.Any(x=>x.Key.Same(layer.Key)&&x.Transferred==layer.Transferred&&(!layer.Transferred||x.Expiry==layer.Expiry)))throw new ArgumentException();restored.Add(layer);
            }
            if(restored.Sum(x=>(long)x.Remaining)>shieldCap)throw new ArgumentException("Shield cap exceeded.");
            MaxHp=max;anchorHp=baseHp;anchorMaxHp=baseMax;Hp=hp;lastTick=at;layers.Clear();layers.AddRange(restored);
        }
    }
}
