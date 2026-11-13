using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Derived core-stat observation, not a committed START or a native HP/effect owner.
    public sealed class OrdinaryFiveCoreStatSnapshot
    {
        public const string Version="combat.ordinary-five-core-stats.v1.20261023";
        public const string NyxOriginVersion="combat.ordinary-five-core-stats.v2.20261107";
        internal static readonly IReadOnlyList<string> NyxOrder=System.Array.AsReadOnly(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"});
        public const string Scope="MAX_HP_ATK_DEF_ONLY";
        public sealed class Equipment
        {
            public string InstanceId {get;} public long Revision {get;} public long EffectRevision {get;}
            public string Category {get;} public string SetTag {get;} public int Star {get;}
            internal Equipment(JObject item){
                Fields(item,"instanceId","instanceRevision","effectRevision","definitionVersion","categoryId","star","setTag","uniqueEffects","enchantment");
                InstanceId=Id(item,"instanceId");Revision=Nat(item,"instanceRevision",1);EffectRevision=Nat(item,"effectRevision",1);
                Need(Text(item,"definitionVersion")==RunEquipmentStatSource.Version);
                Category=Text(item,"categoryId");SetTag=Text(item,"setTag");Star=(int)Nat(item,"star",1,5);
                Need(new[]{"HP","ATK","DEF"}.Contains(Category)&&new[]{"SET01","SET04"}.Contains(SetTag));
                Need(Array(item["uniqueEffects"],0).Count==0&&item["enchantment"]?.Type==JTokenType.Null);
            }
            internal bool SamePlacement(Equipment other)=>other!=null&&InstanceId==other.InstanceId&&Revision==other.Revision&&SameDefinition(other);
            internal bool SameDefinition(Equipment other)=>Category==other.Category&&SetTag==other.SetTag&&Star==other.Star;
        }
        public sealed class ActorStats
        {
            readonly Equipment[] slots;
            public string OperatorId {get;} public int Slot {get;} public long CurrentHp {get;} public long MaxHp {get;}
            public CombatExactValue Attack {get;} public CombatExactValue Defense {get;} public CombatExactValue SkillMagnitude {get;}
            public long ConditionalDefenseFlat {get;}
            public Equipment EquipmentSlot(int slot){if(slot<0||slot>2)throw new ArgumentOutOfRangeException(nameof(slot));return slots[slot];}
            internal ActorStats(JObject actor,JObject source,HashSet<string> instances):this(actor,instances){
                Fields(source,"operatorId","currentHp","slots");Need(Id(source,"operatorId")==OperatorId&&CurrentHp==Nat(source,"currentHp"));
                Array(source["slots"],3);Need(JToken.DeepEquals(actor["equipment"],source["slots"]));
            }
            internal ActorStats(JObject actor,HashSet<string> instances){
                Fields(actor,"operatorId","slot","currentHp","equipment","effectiveStats","conditionalDefenseFlat","skillMagnitude");
                OperatorId=Id(actor,"operatorId");Slot=(int)Nat(actor,"slot",0,4);CurrentHp=Nat(actor,"currentHp");
                var stats=Object(actor["effectiveStats"]);Fields(stats,"maxHp","attack","defense");MaxHp=Nat(stats,"maxHp",1);Need(CurrentHp<=MaxHp);
                Attack=CombatExactValue.From(Object(stats["attack"]));Defense=CombatExactValue.From(Object(stats["defense"]));
                SkillMagnitude=CombatExactValue.From(Object(actor["skillMagnitude"]));ConditionalDefenseFlat=Nat(actor,"conditionalDefenseFlat");
                var gear=Array(actor["equipment"],3);
                slots=new Equipment[3];for(int i=0;i<3;i++){if(gear[i].Type==JTokenType.Null)continue;var item=new Equipment(Object(gear[i]));Need(instances.Add(item.InstanceId));slots[i]=item;}
            }
        }
        internal sealed class ModifierProof
        {
            internal readonly string Id,Version;internal readonly long Revision;
            internal ModifierProof(JObject input){Fields(input,"sourceId","version","revision","status");Id=OrdinaryFiveCoreStatSnapshot.Id(input,"sourceId");Version=OrdinaryFiveCoreStatSnapshot.Id(input,"version");Revision=Nat(input,"revision",1);Need(Text(input,"status")=="NO_ACTIVE_STAT_MODIFIERS");}
        }
        readonly Dictionary<string,ActorStats> actors;
        internal readonly Dictionary<string,ModifierProof> proofs;
        public string AccountId {get;} public string RunId {get;} public string OriginHash {get;} public string SourceHash {get;} public string PacketHash {get;}
        public long Revision {get;} public long InventoryRevision {get;}
        public ActorStats Actor(string operatorId)=>actors.TryGetValue(operatorId,out var actor)?actor:throw new ArgumentException("ordinary_stat_actor_missing");
        internal IEnumerable<ActorStats> Actors=>actors.Values;
        OrdinaryFiveCoreStatSnapshot(JObject packet,JObject state,string trustedPacketHash,string account,string run,string origin,string originVersion){
            Need(originVersion==Version||originVersion==NyxOriginVersion);
            Need(packet!=null&&state!=null);Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))<=131072&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(state))<=131072);
            Fields(packet,"version","accountId","runId","revision","inventoryRevision","originHash","sourceHash","scope","actors","packetHash");
            Fields(state,"accountId","runId","revision","inventoryRevision","modifiers","actors");
            Need(Text(packet,"version")==Version&&Text(packet,"scope")==Scope);
            AccountId=Id(packet,"accountId");RunId=Id(packet,"runId");Need(AccountId==account&&RunId==run&&Id(state,"accountId")==account&&Id(state,"runId")==run);
            OriginHash=Hash(packet,"originHash");SourceHash=Hash(packet,"sourceHash");PacketHash=Hash(packet,"packetHash");
            Need(OriginHash==origin&&PacketHash==trustedPacketHash&&SourceHash==Digest(state));
            var body=(JObject)packet.DeepClone();body.Remove("packetHash");Need(PacketHash==Digest(body));
            Revision=Nat(packet,"revision",1);InventoryRevision=Nat(packet,"inventoryRevision",1);
            Need(Revision==Nat(state,"revision",1)&&InventoryRevision==Nat(state,"inventoryRevision",1));
            var modifiers=Object(state["modifiers"]);Fields(modifiers,"research","commander","augments","temporary");proofs=new Dictionary<string,ModifierProof>(StringComparer.Ordinal);
            foreach(var key in new[]{"research","commander","augments","temporary"})proofs.Add(key,new ModifierProof(Object(modifiers[key])));
            var rows=Array(packet["actors"],5);var sources=Array(state["actors"],5);var sourceRows=new Dictionary<string,JObject>(StringComparer.Ordinal);
            foreach(var source in sources){var row=Object(source);var id=Id(row,"operatorId");Need(!sourceRows.ContainsKey(id));sourceRows.Add(id,row);}
            actors=new Dictionary<string,ActorStats>(StringComparer.Ordinal);var slots=new HashSet<int>();var instances=new HashSet<string>(StringComparer.Ordinal);
            var roster=originVersion==NyxOriginVersion?NyxOrder:new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};
            foreach(var row in rows){var item=Object(row);var id=Id(item,"operatorId");Need(roster.Contains(id)&&!actors.ContainsKey(id)&&sourceRows.ContainsKey(id));var actor=new ActorStats(item,sourceRows[id],instances);Need(slots.Add(actor.Slot));actors.Add(id,actor);}
            if(originVersion==NyxOriginVersion)Need(actors.Values.OrderBy(a=>a.Slot).Select(a=>a.OperatorId).SequenceEqual(NyxOrder));
        }
        public static OrdinaryFiveCoreStatSnapshot Read(JObject packet,JObject sourceState,string trustedPacketHash,string expectedAccount,string expectedRun,string expectedOriginHash)
            =>new OrdinaryFiveCoreStatSnapshot(packet,sourceState,trustedPacketHash,expectedAccount,expectedRun,expectedOriginHash,Version);
        // The server retains v1 for derived packets; v2 identifies their immutable origin and roster.
        public static OrdinaryFiveCoreStatSnapshot Read(JObject packet,JObject sourceState,string trustedPacketHash,string expectedAccount,string expectedRun,string expectedOriginHash,string expectedOriginVersion)
            =>new OrdinaryFiveCoreStatSnapshot(packet,sourceState,trustedPacketHash,expectedAccount,expectedRun,expectedOriginHash,expectedOriginVersion);
        // Validates receipt structure only; source-state verification is still required by Read.
        internal static void ValidateInitializationPacket(JObject packet,string originVersion=Version){
            Need(originVersion==Version||originVersion==NyxOriginVersion);
            Fields(packet,"version","accountId","runId","revision","inventoryRevision","originHash","sourceHash","scope","actors","packetHash");
            Need(Text(packet,"version")==Version&&Text(packet,"scope")==Scope);Id(packet,"accountId");Id(packet,"runId");Nat(packet,"revision",1);Nat(packet,"inventoryRevision",1);
            Hash(packet,"originHash");Hash(packet,"sourceHash");var hash=Hash(packet,"packetHash");var body=(JObject)packet.DeepClone();body.Remove("packetHash");Need(hash==Digest(body));
            var instances=new HashSet<string>(StringComparer.Ordinal);var actors=Array(packet["actors"],5).Select(row=>new ActorStats(Object(row),instances)).ToArray();
            Need(actors.Select(a=>a.Slot).Distinct().Count()==5);
            Need(originVersion==NyxOriginVersion?actors.OrderBy(a=>a.Slot).Select(a=>a.OperatorId).SequenceEqual(NyxOrder):actors.Select(a=>a.OperatorId).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(new[]{"op_eve","op_kyle","op_mira","op_reina","op_sera"}));
        }
        static void Fields(JObject value,params string[] keys)=>BDirectAttackInput.Fields(value,keys);
        static long Nat(JToken value,string key,long min=0,long max=9007199254740991)=>BDirectAttackInput.Nat(value,key,min,max);
        static string Text(JToken value,string key)=>BDirectAttackInput.Text(value,key);
        static JObject Object(JToken value)=>value as JObject??throw new ArgumentException("ordinary_stat_object_missing");
        static JArray Array(JToken value,int count){Need(value is JArray a&&a.Count==count);return (JArray)value;}
        static string Id(JObject value,string key){var id=Text(value,key);Need(Regex.IsMatch(id,"^[\\x21-\\x7e]{1,128}$"));return id;}
        static string Hash(JObject value,string key){var hash=Text(value,key);Need(Regex.IsMatch(hash,"^[a-f0-9]{64}$"));return hash;}
        static string Digest(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
        internal static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_stat_source_conflict");}
    }
    // Latest derived observation only. Callers retain authority and provide independently trusted hashes.
    // No network, native application, HP initialization, or START capability is exposed here.
    public sealed class OrdinaryFiveCoreStatFeed
    {
        readonly string account,run,origin,originVersion;
        readonly int owningThread=System.Threading.Thread.CurrentThread.ManagedThreadId;
        public OrdinaryFiveCoreStatSnapshot Current {get;private set;}
        public event Action Changed;
        public OrdinaryFiveCoreStatFeed(string accountId,string runId,string originalHash):this(accountId,runId,originalHash,OrdinaryFiveCoreStatSnapshot.Version){}
        public OrdinaryFiveCoreStatFeed(string accountId,string runId,string originalHash,string originalVersion){OrdinaryFiveCoreStatSnapshot.Need(originalVersion==OrdinaryFiveCoreStatSnapshot.Version||originalVersion==OrdinaryFiveCoreStatSnapshot.NyxOriginVersion);account=accountId;run=runId;origin=originalHash;originVersion=originalVersion;}
        public bool Publish(JObject packet,JObject sourceState,string trustedPacketHash){
            if(System.Threading.Thread.CurrentThread.ManagedThreadId!=owningThread)throw new InvalidOperationException("ordinary_stat_owner_thread_required");
            var next=OrdinaryFiveCoreStatSnapshot.Read(packet,sourceState,trustedPacketHash,account,run,origin,originVersion);var before=Current;
            if(before!=null){
                OrdinaryFiveCoreStatSnapshot.Need(next.Revision>=before.Revision&&next.InventoryRevision>=before.InventoryRevision);
                if(next.Revision==before.Revision){OrdinaryFiveCoreStatSnapshot.Need(next.PacketHash==before.PacketHash);return false;}
                var oldItems=before.Actors.SelectMany(a=>Enumerable.Range(0,3).Select(a.EquipmentSlot)).Where(i=>i!=null).ToDictionary(i=>i.InstanceId);
                foreach(var actor in next.Actors){var old=before.Actor(actor.OperatorId);OrdinaryFiveCoreStatSnapshot.Need(actor.Slot==old.Slot);
                    for(int i=0;i<3;i++){var item=actor.EquipmentSlot(i);var prior=old.EquipmentSlot(i);
                        if(next.InventoryRevision==before.InventoryRevision)OrdinaryFiveCoreStatSnapshot.Need(item==null?prior==null:item.SamePlacement(prior));
                        if(item!=null&&oldItems.TryGetValue(item.InstanceId,out var previous)){OrdinaryFiveCoreStatSnapshot.Need(item.Revision>=previous.Revision&&item.EffectRevision>=previous.EffectRevision&&(item.Revision!=previous.Revision||item.SameDefinition(previous)));}
                    }
                }
                foreach(var key in next.proofs.Keys){var old=before.proofs[key];var proof=next.proofs[key];OrdinaryFiveCoreStatSnapshot.Need(old.Id!=proof.Id||old.Version!=proof.Version||proof.Revision>=old.Revision);}
            }
            Current=next;Changed?.Invoke();return true;
        }
    }
}
