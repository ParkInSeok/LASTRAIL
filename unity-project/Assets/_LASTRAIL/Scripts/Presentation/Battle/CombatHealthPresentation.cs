using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle
{
    /// <summary>
    /// Detached consumer of explicit original authority, not a combat/recovery owner.
    /// No native B adapter is implied. Feed battle ticks (held with combat), never wall time.
    /// Bind/restore seed a baseline; adapters own visual objects and existing input handlers.
    /// Optional revision is an authority sequence independent of tick: same-tick support is valid.
    /// </summary>
    public sealed class CombatHealthPresentation
    {
        public const string Version = "health-authority.v1";
        const long MaxExactInteger = 9007199254740991;
        sealed class Life
        {
            public long Generation, Token;
            public bool Down;
            public string Motion;
            public HashSet<string> ShieldEvents = new HashSet<string>(StringComparer.Ordinal);
            public Life Copy() => new Life { Generation=Generation, Token=Token, Down=Down,
                Motion=Motion, ShieldEvents=new HashSet<string>(ShieldEvents, StringComparer.Ordinal) };
        }
        Dictionary<string, Life> lives = new Dictionary<string, Life>(StringComparer.Ordinal);
        Dictionary<string, long> generations = new Dictionary<string, long>(StringComparer.Ordinal);
        Action<JObject> render;
        JObject current, lastAuthority;
        long lastTick = -1, nextToken;
        long? lastRevision;

        public void Bind(Action<JObject> view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            Unbind();
            render = view;
        }
        public void Unbind()
        {
            render = null;
            lives.Clear(); generations.Clear();
            current = lastAuthority = null;
            lastTick = -1; lastRevision = null;
            // Tokens deliberately survive rebinding, so old animation callbacks cannot match.
        }
        public void Apply(JObject authority, bool restore = false)
        {
            if (render == null) return;
            var source = authority == null ? throw new ArgumentNullException(nameof(authority)) : (JObject)authority.DeepClone();
            Validate(source);
            long tick = (long)source["tick"];
            long? revision = (long?)source["revision"];
            if (!restore)
            {
                Need(tick >= lastTick, "Stale health battle tick.");
                Need(!lastRevision.HasValue || revision.HasValue, "Authority revision cannot disappear during a binding.");
                if (lastRevision.HasValue)
                {
                    Need(revision.Value >= lastRevision.Value, "Stale health authority revision.");
                    Need(revision.Value != lastRevision.Value || JToken.DeepEquals(source, lastAuthority), "Conflicting health authority revision.");
                }
                foreach (var row in (JArray)source["entities"])
                    if (generations.TryGetValue(Key(row), out var previousGeneration))
                        Need((long)row["generation"] >= previousGeneration, "Stale health entity generation.");
            }
            var staged = new Dictionary<string, Life>(StringComparer.Ordinal);
            var stagedGenerations = restore ? new Dictionary<string, long>(StringComparer.Ordinal)
                : new Dictionary<string, long>(generations, StringComparer.Ordinal);
            long token = nextToken;
            var rows = new JArray();
            foreach (JObject row in (JArray)source["entities"])
            {
                string key = Key(row); long generation = (long)row["generation"];
                bool baseline = restore || !lives.TryGetValue(key, out var prior) || prior.Generation != generation;
                Life life;
                bool down = (bool)row["down"];
                if (baseline)
                    life = new Life { Generation=generation, Down=down, Motion=down?"DownHold":"Source", Token=checked(++token) };
                else
                {
                    life = lives[key].Copy();
                    if (life.Down != down)
                    {
                        life.Token = checked(++token);
                        life.Motion = down ? "Down2" : "Recovery2";
                    }
                    life.Down = down;
                }
                bool depleted = false;
                if (row["shieldEvent"] is JObject shieldEvent)
                {
                    bool fresh = life.ShieldEvents.Add((string)shieldEvent["id"]);
                    depleted = !baseline && fresh && (string)shieldEvent["kind"] == "DEPLETED";
                }
                var output = (JObject)row.DeepClone();
                output.Remove("shieldEvent");
                output["remainingSeconds"] = JValue.CreateNull();
                long? recoverAt = (long?)row["recoverAt"];
                if (down && (bool)source["clockSynchronized"] && recoverAt.HasValue && recoverAt.Value > tick)
                {
                    long remaining = recoverAt.Value - tick, rate = (long)source["ticksPerSecond"];
                    // Positive integer ceiling without addition overflow. Never infer recovery at zero.
                    output["remainingSeconds"] = remaining / rate + (remaining % rate == 0 ? 0 : 1);
                }
                if ((string)row["kind"] != "CARRIAGE") output["broken"] = false;
                output["motion"] = life.Motion;
                output["motionToken"] = life.Token;
                output["shieldDepletedPulse"] = depleted;
                staged.Add(key, life); stagedGenerations[key] = generation; rows.Add(output);
            }
            var result = new JObject { ["version"]=Version, ["tick"]=tick, ["entities"]=rows,
                ["sharedPool"]=source["sharedPool"]?.DeepClone() ?? JValue.CreateNull() };
            lives = staged; generations = stagedGenerations; nextToken = token;
            lastTick = tick; lastRevision = revision; lastAuthority = source; current = result;
            Publish();
        }
        /// <summary>Cosmetic completion only. It never changes HP, down, eligibility or authority time.</summary>
        public void CompleteRecovery(string id, long generation, long motionToken)
        {
            if (render == null || current == null) return;
            string key = "OPERATOR/" + id;
            if (!lives.TryGetValue(key, out var life) || life.Generation != generation || life.Token != motionToken
                || life.Down || life.Motion != "Recovery2") return;
            life.Motion = "Source";
            foreach (JObject row in (JArray)current["entities"])
            {
                row["shieldDepletedPulse"] = false;
                if (Key(row) == key) row["motion"] = "Source";
            }
            Publish();
        }
        void Publish()
        {
            // A view can neither mutate retained state nor a borrowed authority snapshot.
            var callback = render;
            callback?.Invoke((JObject)current.DeepClone());
        }
        static string Key(JToken row) => (string)row["kind"] + "/" + (string)row["id"];
        static void Validate(JObject s)
        {
            Need((string)s["version"] == Version, "Unsupported health authority version.");
            Natural(s["tick"]); Positive(s["ticksPerSecond"]); Flag(s["clockSynchronized"]);
            if (s["revision"] != null) Natural(s["revision"]);
            Need(s["entities"] is JArray, "Health entities are required.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in (JArray)s["entities"])
            {
                Need(value is JObject, "Invalid health entity.");
                var row = (JObject)value;
                Text(row["id"]); Natural(row["generation"]);
                Need((string)row["kind"] == "OPERATOR" || (string)row["kind"] == "CARRIAGE", "Unknown health entity kind.");
                Need(keys.Add(Key(row)), "Duplicate health entity.");
                long max = Positive(row["maxHp"]), hp = Natural(row["hp"]), shield = Natural(row["individualShield"]);
                Need(max <= int.MaxValue && hp <= max && shield <= int.MaxValue, "Health amount out of range.");
                Need(row["individualShieldMax"] != null, "Shield maximum must be explicit or null.");
                if (row["individualShieldMax"].Type != JTokenType.Null)
                    Need(shield <= Positive(row["individualShieldMax"]), "Shield exceeds its original maximum.");
                Flag(row["down"]); Flag(row["actionEligible"]);
                Need(!(bool)row["down"] || !(bool)row["actionEligible"], "Down entity cannot be action eligible.");
                Need(row["recoverAt"] != null, "Recovery tick must be explicit or null.");
                if (row["recoverAt"].Type != JTokenType.Null) Natural(row["recoverAt"]);
                if ((string)row["kind"] == "CARRIAGE")
                {
                    Need(!(bool)row["down"], "Carriage breakage is not operator down.");
                    Flag(row["broken"]);
                }
                if (row["shieldEvent"] != null && row["shieldEvent"].Type != JTokenType.Null)
                {
                    Need(row["shieldEvent"] is JObject, "Invalid shield event.");
                    Text(row["shieldEvent"]["id"]);
                    string kind = (string)row["shieldEvent"]["kind"];
                    Need(kind == "DEPLETED" || kind == "EXPIRED", "Unknown shield event kind.");
                }
            }
            if (s["sharedPool"] != null && s["sharedPool"].Type != JTokenType.Null)
            {
                Need(s["sharedPool"] is JObject, "Invalid original shared pool.");
                Text(s["sharedPool"]["id"]);
                Need(Natural(s["sharedPool"]["remaining"]) <= Positive(s["sharedPool"]["maximum"]), "Shared pool exceeds its original maximum.");
            }
        }
        static void Need(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
        static void Text(JToken t) => Need(t != null && t.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)t), "Original health identity is required.");
        static void Flag(JToken t) => Need(t != null && t.Type == JTokenType.Boolean, "Original health flag is required.");
        static long Natural(JToken t)
        {
            Need(t != null && t.Type == JTokenType.Integer, "Original health integer is required.");
            long n;
            try { n = (long)t; } catch (Exception e) when (e is OverflowException || e is FormatException || e is InvalidCastException)
            { throw new ArgumentException("Original health integer is out of range.", e); }
            Need(n >= 0 && n <= MaxExactInteger, "Original health integer is out of range."); return n;
        }
        static long Positive(JToken t) { long n = Natural(t); Need(n > 0, "Original health positive value is required."); return n; }
    }
}
