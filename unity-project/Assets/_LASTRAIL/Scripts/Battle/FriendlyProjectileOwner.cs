using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    /// <summary>Logical contact result, NOT a native HP/reward receipt.</summary>
    public sealed class FriendlyProjectileContact
    {
        readonly JObject result;
        public bool NewlyResolved { get; }
        public int Damage => (int)result["damage"];
        public string Outcome => (string)result["outcome"];
        internal FriendlyProjectileContact(JObject result, bool fresh)
        {
            this.result = (JObject)result.DeepClone();
            NewlyResolved = fresh;
        }
        public JObject Snapshot() => (JObject)result.DeepClone();
    }

    /// <summary>
    /// Internal standalone lifetime aggregate. Receives authority clock/order/contact facts;
    /// does not simulate geometry/speed, select targets, read live actors or change HP.
    /// Use Clone inside the existing simulation's clone/process/publish transaction, and
    /// publish the whole combat root with HP/rewards/ledger together. NewlyResolved alone
    /// is NOT an exactly-once external delivery protocol. Never persist just this owner
    /// after applying live HP. The explicit Mira first LOCAL trial composes it with native HP; other authority adapters are not implied.
    /// </summary>
    public sealed class FriendlyProjectileOwner
    {
        public const string Version = "friendly-projectile-lifecycle.v1.20261015";
        public const string ActualContactVersion = "friendly-projectile-lifecycle.v2.20261016";
        readonly string schemaVersion;
        sealed class Slot
        {
            internal JObject Reservation, Command, Result;
        }
        sealed class Branch
        {
            internal string Id, Kind, Status = "FLYING", Reason;
            internal readonly List<Slot> Slots = new List<Slot>();
        }
        sealed class Opportunity
        {
            internal string Id;
            internal JObject Packet;
            internal CombatProjectileAttackSnapshot Attack;
            internal readonly List<Branch> Branches = new List<Branch>();
        }
        readonly string combatId;
        readonly int capacity;
        readonly List<Opportunity> opportunities = new List<Opportunity>();
        readonly JArray commands = new JArray();
        long tick, lastSequence;
        bool held;
        string terminal;

        public FriendlyProjectileOwner(string combatId, int capacity):this(combatId,capacity,Version) {}
        // Explicit opt-in preserves v1 departure snapshots and reservation-order replay.
        public FriendlyProjectileOwner(string combatId, int capacity, string version)
        {
            if(version!=Version && version!=ActualContactVersion)throw new ArgumentException("Projectile lifecycle version.");
            schemaVersion=version;
            Id(combatId);
            if (capacity < 1 || capacity > 10000) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.combatId = combatId;
            this.capacity = capacity;
        }
        public bool AdvanceTo(long logicalTick)
        {
            if (logicalTick < tick) throw new InvalidOperationException("Projectile clock cannot move backwards.");
            if (held || terminal != null || logicalTick == tick) return false;
            Room();
            tick = logicalTick;
            Record(new JObject { ["kind"]="ADVANCE", ["tick"]=logicalTick });
            return true;
        }
        public bool SetHeld(bool value)
        {
            if (held == value) return false;
            if (terminal != null) throw new InvalidOperationException("Combat has ended.");
            Room();
            held = value;
            Record(new JObject { ["kind"]="HOLD", ["value"]=value });
            return true;
        }
        public bool CancelPreparation(string opportunityId, long sequence)
        {
            Id(opportunityId);
            var old = Find(opportunityId);
            if (old != null)
            {
                if (old.Packet != null) throw new InvalidOperationException("Cannot cancel a fired opportunity.");
                return false;
            }
            Running(); Order(sequence); Room();
            opportunities.Add(new Opportunity { Id=opportunityId });
            lastSequence = sequence;
            Record(new JObject { ["kind"]="CANCEL", ["opportunityId"]=opportunityId, ["sequence"]=sequence });
            return true;
        }
        /// <summary>
        /// Actual original opportunity/selection owner must validate ability and geometry eligibility,
        /// shared-HP identity, whole primary exclusions, unique secondary targets and the effect's
        /// maximum count before calling. This lifecycle owner preserves supplied reservations;
        /// it does not replace selection rules or manufacture missing authority sources.
        /// </summary>
        public bool Launch(JObject packet, bool sourceEligible)
        {
            Fields(packet, "version","opportunityId","sourceId","sourceGeneration","tick","sequence","attack","geometry","branches");
            VersionOf(packet);
            string id = Text(packet["opportunityId"]);
            var old = Find(id);
            if (old != null)
            {
                if (old.Packet == null) return false;
                if (!JToken.DeepEquals(old.Packet, packet)) throw new InvalidOperationException("Conflicting launch replay.");
                return false; // A fired packet still replays after source down, hold or combat end.
            }
            Running();
            if (Number(packet["tick"]) != tick) throw new InvalidOperationException("Launch must use authority tick.");
            long sequence = Number(packet["sequence"]); Order(sequence); Room();
            Text(packet["sourceId"]); Number(packet["sourceGeneration"]);
            var geometry = Object(packet["geometry"]); Fields(geometry, "version","revision","hash");
            Text(geometry["version"]); Number(geometry["revision"]); Text(geometry["hash"]);
            var candidate = new Opportunity { Id=id, Packet=(JObject)packet.DeepClone(),
                Attack=CombatProjectileAttackSnapshot.Capture(Object(packet["attack"])) };
            var branchIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in Array(packet["branches"]))
            {
                var row = Object(item); Fields(row, "branchId","pathId","kind","contacts");
                string branchId=Text(row["branchId"]), kind=Text(row["kind"]); Text(row["pathId"]);
                if (!branchIds.Add(branchId) || (kind!="MAIN" && kind!="SECONDARY"))
                    throw new ArgumentException("Invalid projectile branch.");
                var branch = new Branch { Id=branchId, Kind=kind };
                var ids=new HashSet<string>(StringComparer.Ordinal);
                var targets=new HashSet<string>(StringComparer.Ordinal);
                foreach (var value in Array(row["contacts"]))
                {
                    var r=Object(value); Fields(r,"contactId","targetId","targetGeneration","ordinal","coefficientBps");
                    string contact=Text(r["contactId"]), target=Text(r["targetId"]);
                    long generation=Number(r["targetGeneration"]);
                    if (!ids.Add(contact) || !targets.Add(target+"\n"+generation)
                        || Number(r["ordinal"])!=branch.Slots.Count)
                        throw new ArgumentException("Reservations require unique targets and fixed contiguous ordinals.");
                    CombatProjectileAttackSnapshot.Coefficient(Bps(r["coefficientBps"]));
                    branch.Slots.Add(new Slot { Reservation=(JObject)r.DeepClone() });
                }
                if (branch.Slots.Count==0 || kind=="SECONDARY" && branch.Slots.Count!=1)
                    throw new ArgumentException("Explicit projectile contacts required.");
                candidate.Branches.Add(branch);
            }
            if (candidate.Branches.Count==0) throw new ArgumentException("Explicit branches required.");
            // A prefire down records cancellation only; no flight or new opportunity is fabricated.
            if (!sourceEligible) candidate=new Opportunity { Id=id };
            opportunities.Add(candidate);
            lastSequence=sequence;
            Record(new JObject { ["kind"]="LAUNCH", ["packet"]=packet.DeepClone(), ["sourceEligible"]=sourceEligible });
            return sourceEligible;
        }

        public FriendlyProjectileContact ResolveContact(JObject packet)
        {
            Fields(packet,"version","opportunityId","branchId","contactId","tick","sequence","targetGeneration","eligible","arrival");
            VersionOf(packet);
            var opportunity=Find(Text(packet["opportunityId"])) ?? throw new ArgumentException("Unknown opportunity.");
            var branch=FindBranch(opportunity, Text(packet["branchId"]));
            var slot=branch.Slots.FirstOrDefault(s=>(string)s.Reservation["contactId"]==Text(packet["contactId"]))
                ?? throw new ArgumentException("Unreserved projectile contact.");
            Number(packet["tick"]); Number(packet["sequence"]); Number(packet["targetGeneration"]); Boolean(packet["eligible"]);
            if (slot.Result!=null)
            {
                if (slot.Command!=null && !JToken.DeepEquals(slot.Command,packet))
                    throw new InvalidOperationException("Conflicting contact replay.");
                return new FriendlyProjectileContact(slot.Result,false);
            }
            Running(); Room();
            if (Number(packet["tick"])!=tick || branch.Status!="FLYING"
                || schemaVersion==Version && branch.Slots.First(s=>s.Result==null)!=slot)
                throw new InvalidOperationException("Contact must follow authority time and reserved order.");
            long sequence=Number(packet["sequence"]); Order(sequence);
            bool hit=Boolean(packet["eligible"])
                && Number(packet["targetGeneration"])==Number(slot.Reservation["targetGeneration"]);
            int damage=0; JObject reference=null;
            if (hit)
            {
                var arrival=Object(packet["arrival"]);
                int bps=Bps(slot.Reservation["coefficientBps"]);
                var value=branch.Kind=="SECONDARY"
                    ? opportunity.Attack.Evaluate(arrival)
                    : opportunity.Attack.EvaluatePiercing(arrival,bps);
                damage=value.ScaleAndFloor(branch.Kind=="SECONDARY"?bps:10000);
                reference=value.Snapshot();
            }
            else if (packet["arrival"].Type!=JTokenType.Null && !(packet["arrival"] is JObject))
                throw new ArgumentException("Invalid unused arrival.");
            var result=Result(opportunity,branch,slot,sequence,hit?"HIT":"NO_HIT",damage,null,reference);
            // No external side effects above. Publish calculation + completion record together.
            slot.Command=(JObject)packet.DeepClone();
            slot.Result=result;
            if (branch.Slots.All(s=>s.Result!=null)) branch.Status="RESOLVED";
            lastSequence=sequence;
            Record(new JObject { ["kind"]="CONTACT", ["packet"]=packet.DeepClone() });
            return new FriendlyProjectileContact(result,true);
        }
        public bool StopBranch(string opportunityId,string branchId,string reason,long sequence)
        {
            Id(opportunityId); Id(branchId); Reason(reason,false);
            var opportunity=Find(opportunityId) ?? throw new ArgumentException("Unknown opportunity.");
            var branch=FindBranch(opportunity,branchId);
            if (branch.Status!="FLYING")
            {
                if (branch.Status=="INTERRUPTED" && branch.Reason!=reason)
                    throw new InvalidOperationException("Conflicting projectile termination.");
                return false;
            }
            Running(); Order(sequence); Room();
            Interrupt(opportunity,branch,reason,sequence);
            lastSequence=sequence;
            Record(new JObject { ["kind"]="STOP", ["opportunityId"]=opportunityId,["branchId"]=branchId,["reason"]=reason,["sequence"]=sequence });
            return true;
        }
        public bool End(string reason,long sequence)
        {
            Reason(reason,true);
            if (terminal!=null)
            {
                if (terminal!=reason) throw new InvalidOperationException("Conflicting combat termination.");
                return false;
            }
            Order(sequence); Room(); // Ending authority combat is permitted while held.
            foreach(var opportunity in opportunities)
                foreach(var branch in opportunity.Branches.Where(b=>b.Status=="FLYING"))
                    Interrupt(opportunity,branch,reason,sequence);
            terminal=reason; lastSequence=sequence;
            Record(new JObject { ["kind"]="END",["reason"]=reason,["sequence"]=sequence });
            return true;
        }
        void Interrupt(Opportunity opportunity,Branch branch,string reason,long sequence)
        {
            foreach(var slot in branch.Slots.Where(s=>s.Result==null))
                slot.Result=Result(opportunity,branch,slot,sequence,"INTERRUPTED",0,reason,null);
            branch.Status="INTERRUPTED"; branch.Reason=reason;
        }
        JObject Result(Opportunity opportunity,Branch branch,Slot slot,long sequence,string outcome,int damage,string reason,JObject reference) =>
            new JObject { ["opportunityId"]=opportunity.Id,["branchId"]=branch.Id,
                ["contactId"]=slot.Reservation["contactId"].DeepClone(),["sourceId"]=opportunity.Packet["sourceId"].DeepClone(),
                ["sourceGeneration"]=opportunity.Packet["sourceGeneration"].DeepClone(),
                ["targetId"]=slot.Reservation["targetId"].DeepClone(),["targetGeneration"]=slot.Reservation["targetGeneration"].DeepClone(),
                ["ordinal"]=slot.Reservation["ordinal"].DeepClone(),["tick"]=tick,["sequence"]=sequence,
                ["outcome"]=outcome,["damage"]=damage,["reason"]=reason==null?JValue.CreateNull():new JValue(reason),["reference"]=reference };

        public JObject Capture() => new JObject {
            ["version"]=schemaVersion,["combatId"]=combatId,["capacity"]=capacity,["commands"]=commands.DeepClone(),
            ["state"]=new JObject { ["tick"]=tick,["held"]=held,["terminal"]=terminal==null?JValue.CreateNull():new JValue(terminal),["lastSequence"]=lastSequence,
                ["opportunities"]=new JArray(opportunities.Select(o=>new JObject {
                    ["id"]=o.Id,["packet"]=o.Packet?.DeepClone(),
                    ["branches"]=new JArray(o.Branches.Select(b=>new JObject {
                        ["id"]=b.Id,["status"]=b.Status,["reason"]=b.Reason==null?JValue.CreateNull():new JValue(b.Reason),
                        ["results"]=new JArray(b.Slots.Select(s=>new JObject {
                            ["contactId"]=s.Reservation["contactId"].DeepClone(),
                            ["command"]=s.Command?.DeepClone(),["result"]=s.Result?.DeepClone()
                        }))
                    }))
                })) }
        };
        public static string Fingerprint(JObject snapshot) =>
            "sha256:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(snapshot));
        /// <summary>
        /// expectedHash must come from the caller's trusted composite checkpoint, not this payload.
        /// Pure replay uses recorded arrival values only; emits no HP/reward/FX or elapsed wall time.
        /// </summary>
        public static FriendlyProjectileOwner Restore(JObject snapshot,string expectedCombatId,string expectedHash)
        {
            Fields(snapshot,"version","combatId","capacity","commands","state");
            string version=(string)snapshot["version"];
            if(version!=Version && version!=ActualContactVersion)throw new ArgumentException("Projectile checkpoint version.");
            if (Text(snapshot["combatId"])!=expectedCombatId || expectedHash==null || Fingerprint(snapshot)!=expectedHash)
                throw new ArgumentException("Projectile checkpoint binding/hash mismatch.");
            long cap=Number(snapshot["capacity"]);
            if (cap>10000 || cap<1) throw new ArgumentException("Invalid checkpoint capacity.");
            var rows=snapshot["commands"] as JArray;
            if (rows==null || rows.Count>cap) throw new ArgumentException("Invalid projectile journal.");
            var owner=new FriendlyProjectileOwner(expectedCombatId,(int)cap,version);
            foreach(var row in rows) owner.Replay(Object(row));
            if (!JToken.DeepEquals(owner.Capture(),snapshot)) throw new ArgumentException("Projectile checkpoint replay mismatch.");
            return owner;
        }
        public FriendlyProjectileOwner Clone()
        {
            var snapshot=Capture();
            return Restore(snapshot,combatId,Fingerprint(snapshot));
        }
        void Replay(JObject command)
        {
            switch(Text(command["kind"]))
            {
                case "ADVANCE": Fields(command,"kind","tick"); AdvanceTo(Number(command["tick"])); break;
                case "HOLD": Fields(command,"kind","value"); SetHeld(Boolean(command["value"])); break;
                case "CANCEL": Fields(command,"kind","opportunityId","sequence"); CancelPreparation(Text(command["opportunityId"]),Number(command["sequence"])); break;
                case "LAUNCH": Fields(command,"kind","packet","sourceEligible"); Launch(Object(command["packet"]),Boolean(command["sourceEligible"])); break;
                case "CONTACT": Fields(command,"kind","packet"); ResolveContact(Object(command["packet"])); break;
                case "STOP": Fields(command,"kind","opportunityId","branchId","reason","sequence"); StopBranch(Text(command["opportunityId"]),Text(command["branchId"]),Text(command["reason"]),Number(command["sequence"])); break;
                case "END": Fields(command,"kind","reason","sequence"); End(Text(command["reason"]),Number(command["sequence"])); break;
                default: throw new ArgumentException("Unknown projectile journal command.");
            }
        }
        Opportunity Find(string id) => opportunities.FirstOrDefault(o=>o.Id==id);
        static Branch FindBranch(Opportunity opportunity,string id) =>
            opportunity.Branches.FirstOrDefault(b=>b.Id==id) ?? throw new ArgumentException("Unreserved projectile branch.");
        void Room() { if(commands.Count>=capacity) throw new InvalidOperationException("Projectile journal capacity; preserve for recovery."); }
        void Running() { if(held || terminal!=null) throw new InvalidOperationException("Projectile progression held or combat ended."); }
        void Order(long sequence) { if(sequence<=lastSequence) throw new InvalidOperationException("Authority sequence must increase."); }
        void Record(JObject command) => commands.Add(command);
        static void Reason(string reason,bool combat)
        {
            if(combat ? reason!="NODE_END" && reason!="COMBAT_END" : reason!="WALL" && reason!="INTERCEPTED" && reason!="EXPIRED")
                throw new ArgumentException("Unsupported projectile termination.");
        }
        static JObject Object(JToken value) => value as JObject ?? throw new ArgumentException("Projectile object required.");
        static JArray Array(JToken value)
        {
            var rows=value as JArray;
            if(rows==null || rows.Count>1024) throw new ArgumentException("Invalid projectile array.");
            return rows;
        }
        static void Fields(JObject value,params string[] names)
        {
            if(value==null || value.Count!=names.Length || names.Any(n=>value[n]==null))
                throw new ArgumentException("Invalid projectile fields.");
        }
        void VersionOf(JObject value)
        {
            if(value["version"]?.Type!=JTokenType.String || (string)value["version"]!=schemaVersion)
                throw new ArgumentException("Explicit projectile lifetime version required.");
        }
        static string Text(JToken value)
        {
            if(value?.Type!=JTokenType.String) throw new ArgumentException("Projectile identity required.");
            string text=(string)value; Id(text); return text;
        }
        static void Id(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length>400 || value.Any(char.IsControl))
                throw new ArgumentException("Invalid projectile identity.");
        }
        static long Number(JToken value)
        {
            if(value?.Type!=JTokenType.Integer || !long.TryParse(value.ToString(),out long number) || number<0)
                throw new ArgumentException("Nonnegative projectile integer required.");
            return number;
        }
        static int Bps(JToken value)
        {
            long bps=Number(value);
            if(bps>10000) throw new ArgumentException("Invalid projectile coefficient.");
            return (int)bps;
        }
        static bool Boolean(JToken value)
        {
            if(value?.Type!=JTokenType.Boolean) throw new ArgumentException("Projectile boolean required.");
            return (bool)value;
        }
    }
}