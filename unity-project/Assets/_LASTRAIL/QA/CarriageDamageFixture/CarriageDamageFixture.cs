using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Qa.CarriageDamage
{
    // Isolated deterministic QA model. Ticks and balances are explicit fixture inputs.
    // No Unity, UI, network or persistence ownership; exports are detached copies.
    public sealed class ConvoyFixture
    {
        public const long MaxSafeInteger = 9007199254740991L;
        public const string Version = "carriage-fixture-v2";
        const int TransferBps = 3000;
        sealed class Car
        {
            internal readonly string Id;
            internal readonly long MaxHp, Period;
            internal long Hp, Downs, ProtectedUntil, Pulses;
            internal long? RecoverAt, NextPulse;
            internal Car(string id, long hp, long period) { Id = id; MaxHp = Hp = hp; Period = period; NextPulse = period; }
            internal JObject Capture() => new JObject { ["hp"] = Hp, ["downs"] = Downs,
                ["recoverAt"] = RecoverAt, ["protectedUntil"] = ProtectedUntil,
                ["nextPulse"] = NextPulse, ["pulses"] = Pulses };
        }
        readonly JObject config;
        readonly List<Car> cars = new List<Car>();
        readonly Dictionary<string, Car> byId = new Dictionary<string, Car>(StringComparer.Ordinal);
        readonly JObject ledger = new JObject();
        readonly JArray operations = new JArray();
        readonly long trainMaxHp, recoveryBps, protectionTicks;
        readonly long[] repairTicks;
        long tick, trainHp;
        bool defeated;

        public ConvoyFixture(JObject input)
        {
            if (input == null) throw Invalid("Missing config");
            config = (JObject)input.DeepClone();
            trainHp = trainMaxHp = Integer(config["trainMaxHp"], 1);
            recoveryBps = Integer(config["recoveryBps"], 1);
            if (recoveryBps > 10000) throw Invalid("Recovery percent");
            protectionTicks = Integer(config["protectionTicks"]);
            if (!(config["repairTicks"] is JArray repairs) || repairs.Count == 0) throw Invalid("Repair times");
            repairTicks = repairs.Select(n => Integer(n, 1)).ToArray();
            if (!(config["cars"] is JArray definitions) || definitions.Count == 0) throw Invalid("Cars");
            foreach (var entry in definitions)
            {
                if (!(entry is JObject definition)) throw Invalid("Car definition");
                string id = Identifier(definition["id"]);
                long hp = Integer(definition["maxHp"], 1), period = Integer(definition["devicePeriod"], 1);
                if (byId.ContainsKey(id)) throw Invalid("Duplicate car");
                if (Fraction(hp, recoveryBps) < 1) throw Invalid("Recovery HP must be positive");
                var car = new Car(id, hp, period);
                cars.Add(car); byId.Add(id, car);
            }
        }

        public IReadOnlyList<string> Targets() => defeated ? Array.Empty<string>() : cars.Where(c => c.Hp > 0).Select(c => c.Id).ToArray();

        public JObject Apply(JObject input)
        {
            if (input == null) throw Invalid("Missing damage");
            string id = Identifier(input["eventId"]);
            long amount = Integer(input["amount"]);
            string route = Text(input["route"]), origin = Text(input["origin"]);
            if ((route != "train" && route != "carriage") || (origin != "physical" && origin != "projectile")) throw Invalid("Damage route");
            Car car = null;
            if (route == "carriage" && !byId.TryGetValue(Identifier(input["carId"]), out car)) throw Invalid("Unknown car");
            long? fallback = input.Property("disabledDirectAmount") == null ? (long?)null : Integer(input["disabledDirectAmount"]);
            var e = new JObject { ["eventId"] = id, ["route"] = route, ["carId"] = car == null ? JValue.CreateNull() : new JValue(car.Id),
                ["amount"] = amount, ["origin"] = origin, ["disabledDirectAmount"] = fallback };
            string signature = new JArray("damage", e).ToString(Formatting.None);
            var old = Existing(id, signature);
            if (old != null) return old;
            if (!defeated && car != null && car.Hp == 0 && origin == "physical" && !fallback.HasValue) throw Invalid("Explicit disabled physical amount required");

            long loss = 0, transfer = 0, trainLoss = 0;
            bool isProtected = false, down = false;
            string appliedRoute = car == null ? "direct-train" : "carriage-transfer";
            bool terminalOnEntry = defeated;
            if (!defeated)
            {
                if (car != null && car.Hp == 0 && origin == "physical")
                {
                    appliedRoute = "direct-train";
                    trainLoss = Math.Min(trainHp, fallback.Value);
                }
                else if (car != null)
                {
                    isProtected = tick < car.ProtectedUntil;
                    loss = isProtected ? 0 : Math.Min(car.Hp, amount);
                    transfer = Fraction(loss, TransferBps);
                    trainLoss = Math.Min(trainHp, transfer);
                    // v2: a fatal transfer ends the convoy without scheduling recovery.
                    down = car.Hp > 0 && car.Hp - loss == 0 && trainHp > trainLoss;
                    long? recoverAt = down ? SafeSum(tick, repairTicks[(int)Math.Min(car.Downs, repairTicks.Length - 1)]) : (long?)null;
                    car.Hp -= loss;
                    if (down) { car.Downs++; car.RecoverAt = recoverAt; car.NextPulse = null; }
                }
                else trainLoss = Math.Min(trainHp, amount);
                trainHp -= trainLoss;
                if (trainHp == 0) defeated = true;
            }
            var receipt = new JObject { ["eventId"] = id, ["route"] = appliedRoute, ["loss"] = loss,
                ["transfer"] = transfer, ["trainLoss"] = trainLoss, ["protected"] = isProtected,
                ["down"] = down, ["terminal"] = terminalOnEntry };
            return Record(id, signature, receipt, new JObject { ["kind"] = "damage", ["value"] = e });
        }

        public JObject RepairTrain(string eventId, long amount)
        {
            Identifier(eventId); Integer(amount);
            string signature = new JArray("repair", amount).ToString(Formatting.None);
            var old = Existing(eventId, signature);
            if (old != null) return old;
            long healed = defeated ? 0 : Math.Min(amount, trainMaxHp - trainHp);
            trainHp += healed;
            return Record(eventId, signature, new JObject { ["eventId"] = eventId, ["healed"] = healed },
                new JObject { ["kind"] = "repair", ["eventId"] = eventId, ["amount"] = amount });
        }

        public void Advance(long ticks)
        {
            Integer(ticks);
            if (ticks > 1000000) throw Invalid("Bounded simulation only");
            SafeSum(tick, ticks, Math.Max(protectionTicks, Math.Max(repairTicks.Max(), cars.Max(c => c.Period))));
            for (long i = 0; i < ticks && !defeated; i++)
            {
                tick++;
                foreach (var car in cars)
                {
                    if (car.Hp == 0 && car.RecoverAt.HasValue && tick >= car.RecoverAt.Value)
                    {
                        car.Hp = Fraction(car.MaxHp, recoveryBps); car.RecoverAt = null;
                        car.ProtectedUntil = tick + protectionTicks; car.NextPulse = tick + car.Period;
                    }
                    else if (car.Hp > 0 && car.NextPulse.HasValue && tick >= car.NextPulse.Value)
                    { car.Pulses++; car.NextPulse = tick + car.Period; }
                }
            }
            operations.Add(new JObject { ["kind"] = "advance", ["ticks"] = ticks });
        }

        public JObject Capture()
        {
            var carStates = new JObject();
            foreach (var car in cars) carStates.Add(car.Id, car.Capture());
            return new JObject { ["version"] = Version, ["config"] = config.DeepClone(),
                ["state"] = new JObject { ["tick"] = tick, ["trainHp"] = trainHp,
                    ["phase"] = defeated ? "defeated" : "running", ["cars"] = carStates },
                ["ledger"] = ledger.DeepClone(), ["operations"] = operations.DeepClone() };
        }

        // Local replay consistency only; this is not authenticated or atomic persistent storage.
        public static ConvoyFixture Restore(JObject saved)
        {
            if (saved == null || Text(saved["version"]) != Version || !(saved["operations"] is JArray ops) || ops.Count > 100000)
                throw Invalid("Invalid snapshot");
            var model = new ConvoyFixture(saved["config"] as JObject);
            foreach (var token in ops)
            {
                if (!(token is JObject op)) throw Invalid("Invalid command");
                switch (Text(op["kind"]))
                {
                    case "damage":
                        if (!(op["value"] is JObject value)) throw Invalid("Invalid damage command");
                        var input = (JObject)value.DeepClone();
                        if (input["disabledDirectAmount"]?.Type == JTokenType.Null) input.Remove("disabledDirectAmount");
                        model.Apply(input); break;
                    case "repair": model.RepairTrain(Identifier(op["eventId"]), Integer(op["amount"])); break;
                    case "advance": model.Advance(Integer(op["ticks"])); break;
                    default: throw Invalid("Unknown command");
                }
            }
            if (model.Capture().ToString(Formatting.None) != saved.ToString(Formatting.None)) throw Invalid("Snapshot replay mismatch");
            return model;
        }

        JObject Existing(string id, string signature)
        {
            var entry = ledger[id];
            if (entry == null) return null;
            if ((string)entry["signature"] != signature) throw Invalid("Conflicting event");
            return (JObject)entry["receipt"].DeepClone();
        }
        JObject Record(string id, string signature, JObject receipt, JObject operation)
        {
            ledger.Add(id, new JObject { ["signature"] = signature, ["receipt"] = receipt.DeepClone() });
            operations.Add(operation.DeepClone());
            return (JObject)receipt.DeepClone();
        }
        static InvalidOperationException Invalid(string message) => new InvalidOperationException(message);
        static string Text(JToken token) => token?.Type == JTokenType.String ? (string)token : null;
        static string Identifier(JToken token)
        {
            string id = Text(token);
            if (id == null || !Regex.IsMatch(id, "\\A[a-z][a-z0-9_-]{0,63}\\z") || id == "constructor" || id == "prototype" || id == "__proto__") throw Invalid("Invalid identifier");
            return id;
        }
        static long Integer(JToken token, long minimum = 0)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) throw Invalid("Expected safe integer");
            // Check binary floating values before decimal conversion (which rounds to 15 digits).
            var raw = ((JValue)token).Value;
            if (raw is double || raw is float)
            {
                double floating = Convert.ToDouble(raw);
                if (double.IsNaN(floating) || double.IsInfinity(floating) || floating < minimum ||
                    floating > MaxSafeInteger || Math.Truncate(floating) != floating) throw Invalid("Expected safe integer");
                return (long)floating;
            }
            // Integer/decimal inputs must not be rounded through binary floating point either.
            decimal value;
            try { value = (decimal)token; }
            catch (Exception e) when (e is OverflowException || e is FormatException || e is InvalidCastException) { throw Invalid("Expected safe integer"); }
            if (value < minimum || value > MaxSafeInteger || decimal.Truncate(value) != value) throw Invalid("Expected safe integer");
            return (long)value;
        }
        static long Fraction(long amount, long bps) => (long)((BigInteger)amount * bps / 10000);
        static long SafeSum(params long[] values)
        {
            BigInteger sum = 0;
            foreach (long value in values) sum += value;
            if (sum < 0 || sum > MaxSafeInteger) throw Invalid("Expected safe integer tick sum");
            return (long)sum;
        }
    }
}
