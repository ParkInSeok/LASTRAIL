using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace LastRail.Qa.CarriageDamage
{
    // Evidence adapter owns file IO and Editor checks; it never owns model state.
    public static class CarriageDamageFixtureVerification
    {
        const string Root = "D:/Project/LASTRAIL/QA/CarriageDamage-20260914";
        const string Sources = "D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/QA/CarriageDamageFixture";
        static readonly string[] InputFiles = { "damage.cjs", "damage.test.cjs", "export.cjs", "scenarios.json" };
        static readonly string[] InputHashes = {
            "60280627a04c361a49e9f249f28f39f6ebbc95e134e72e4fe3e7373203bda5da",
            "00440b96d5fff114d8cab151fd8014571757eaa49f5a25c6a7c2da42e11d677e",
            "62a60e538f07d75d29cb6ce6226202443265b175fd9a157c74521b87c9c6b5f6",
            "0f629e7480077eff43a74a514eeaef03b803a16d20d717b13947fbd4e87eac3f" };
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Same(JToken expected, JToken actual, string label) => Require(JToken.DeepEquals(expected, actual), label + " mismatch: expected " + expected + " actual " + actual);
        static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        static JObject Hit(string id, long amount, string car = "a", string origin = "projectile", long? fallback = null)
        {
            var value = new JObject { ["eventId"] = id, ["route"] = "carriage", ["carId"] = car, ["amount"] = amount, ["origin"] = origin };
            if (fallback.HasValue) value["disabledDirectAmount"] = fallback.Value;
            return new JObject { ["kind"] = "damage", ["value"] = value };
        }
        static JObject Advance(long ticks) => new JObject { ["kind"] = "advance", ["ticks"] = ticks };
        static JObject Repair(string id, long amount) => new JObject { ["kind"] = "repair", ["eventId"] = id, ["amount"] = amount };
        static JToken Execute(ConvoyFixture model, JObject command)
        {
            switch ((string)command["kind"])
            {
                case "damage": return model.Apply((JObject)command["value"]);
                case "repair": return model.RepairTrain((string)command["eventId"], (long)command["amount"]);
                case "advance": model.Advance((long)command["ticks"]); return JValue.CreateNull();
                default: throw new InvalidOperationException("Unknown verification command");
            }
        }
        static JObject Replay(string name, JObject config, JArray commands)
        {
            var model = new ConvoyFixture(config); var records = new JArray();
            foreach (JObject command in commands)
            {
                var receipt = Execute(model, command);
                records.Add(new JObject { ["command"] = command.DeepClone(), ["receipt"] = receipt,
                    ["state"] = model.Capture()["state"], ["targets"] = new JArray(model.Targets()) });
            }
            return new JObject { ["name"] = name, ["records"] = records, ["snapshot"] = model.Capture() };
        }
        static JToken Record(JObject scenario, int index) => scenario["records"][index];
        static void At(JToken token, string path, JToken expected) => Same(expected, token.SelectToken(path), path);
        public static JObject VerifyPlayerExclusion()
        {
            var player = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var names = player.Select(a => a.name).OrderBy(n => n).ToArray();
            Require(!names.Any(n => n.StartsWith("LastRail.CarriageDamageFixture", StringComparison.Ordinal)), "Fixture assembly in Player compilation");
            Require(!player.Any(a => a.assemblyReferences.Any(r => r.name.StartsWith("LastRail.CarriageDamageFixture", StringComparison.Ordinal))), "Player references fixture");
            return new JObject { ["excluded"] = true, ["playerAssemblies"] = new JArray(names), ["check"] = "Unity CompilationPipeline Player assemblies and references; no Player build performed" };
        }
        public static JObject Verify()
        {
            var pins = new JArray();
            var manifest = JObject.Parse(File.ReadAllText(Root + "/manifest.json"));
            Require((string)manifest["version"] == ConvoyFixture.Version && ((JArray)manifest["files"]).Count == 4, "Input manifest version/count");
            for (int i = 0; i < InputFiles.Length; i++)
            {
                string actual = Hash(Root + "/" + InputFiles[i]);
                Require(actual == InputHashes[i], "Input SHA " + InputFiles[i]);
                Require((string)manifest["files"][i]["file"] == InputFiles[i] && (string)manifest["files"][i]["sha256"] == actual, "Manifest entry");
                pins.Add(new JObject { ["file"] = InputFiles[i], ["sha256"] = actual });
            }
            var observed = JObject.Parse(File.ReadAllText(Root + "/scenarios.json"));
            var config = (JObject)observed["config"]; var scenarios = new JArray(); int count = 0;
            foreach (JObject expected in observed["scenarios"])
            {
                var commands = new JArray(expected["records"].Select(r => r["command"].DeepClone()));
                var actual = Replay((string)expected["name"], config, commands);
                Same(expected, actual, (string)expected["name"]);
                Same(actual["snapshot"], ConvoyFixture.Restore((JObject)actual["snapshot"]).Capture(), "Observed snapshot restore");
                count += commands.Count; scenarios.Add(actual);
            }
            Require(scenarios.Count == 4 && count == 11, "Observed scenario coverage");
            var sourceHashes = new JArray(Directory.GetFiles(Sources, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".cs", StringComparison.Ordinal) || p.EndsWith(".asmdef", StringComparison.Ordinal))
                .OrderBy(p => p, StringComparer.Ordinal).Select(p => new JObject { ["file"] = p.Replace('\\', '/'), ["sha256"] = Hash(p) }));
            return new JObject { ["version"] = ConvoyFixture.Version,
                ["scope"] = "Pure C# QA model; observed parity is not an independent oracle. No production damage, visual, timing, persistent atomicity or authority claim.",
                ["inputManifest"] = manifest, ["inputHashes"] = pins, ["sourceHashes"] = sourceHashes,
                ["observedRecordCount"] = count, ["config"] = config.DeepClone(), ["scenarios"] = scenarios,
                ["additionalBoundaries"] = Boundaries(config), ["playerExclusion"] = VerifyPlayerExclusion() };
        }
        static JObject Boundaries(JObject config)
        {
            var output = new JObject();
            var tiny = (JObject)config.DeepClone(); tiny["trainMaxHp"] = 1; tiny["recoveryBps"] = 10000;
            tiny["cars"] = new JArray(new JObject { ["id"] = "a", ["maxHp"] = 4, ["devicePeriod"] = 8 });
            var fatal = Replay("fatal-transfer", tiny, new JArray(Hit("fatal-car", 4), Advance(20), Repair("late", 10)));
            var first = Record(fatal, 0);
            At(first, "receipt.loss", 4); At(first, "receipt.transfer", 1); At(first, "receipt.trainLoss", 1);
            At(first, "receipt.down", false); At(first, "receipt.terminal", false);
            At(first, "state.cars.a.hp", 0); At(first, "state.cars.a.downs", 0); At(first, "state.cars.a.recoverAt", JValue.CreateNull());
            At(first, "state.trainHp", 0); At(first, "state.phase", "defeated");
            Same(first["state"], Record(fatal, 2)["state"], "Fatal state remains frozen");
            output["fatalTransfer"] = fatal;
            tiny["trainMaxHp"] = 2;
            var survive = Replay("survive-by-one", tiny, new JArray(Hit("down", 4), Hit("down", 4), Advance(10), Advance(7), Advance(1)));
            At(Record(survive, 0), "receipt.down", true); At(Record(survive, 0), "state.cars.a.downs", 1);
            At(Record(survive, 0), "state.cars.a.recoverAt", 10); At(Record(survive, 0), "state.trainHp", 1);
            Same(Record(survive, 0)["state"], Record(survive, 1)["state"], "Duplicate down");
            At(Record(survive, 2), "state.cars.a.hp", 4); At(Record(survive, 2), "state.trainHp", 1);
            At(Record(survive, 3), "state.cars.a.pulses", 0); At(Record(survive, 4), "state.cars.a.pulses", 1);
            output["surviveByOne"] = survive;
            var protection = Replay("protection-end", config, new JArray(Hit("down", 500), Advance(10), Hit("at-ten", 100), Advance(1), Hit("at-eleven", 100), Advance(1), Hit("at-twelve", 100)));
            foreach (int index in new[] { 2, 4 }) { At(Record(protection, index), "receipt.loss", 0); At(Record(protection, index), "receipt.protected", true); }
            At(Record(protection, 6), "receipt.loss", 100); At(Record(protection, 6), "receipt.protected", false);
            Same(new JArray("a", "b"), Record(protection, 2)["targets"], "Protected targets retained");
            output["protectionEnd"] = protection;
            var independent = Replay("staggered-recovery", config, new JArray(Hit("down-a", 500), Advance(3), Hit("down-b", 500, "b"), Advance(7), Advance(3)));
            At(Record(independent, 3), "state.cars.a.hp", 200); At(Record(independent, 3), "state.cars.b.hp", 0);
            At(Record(independent, 3), "state.cars.b.recoverAt", 13); At(Record(independent, 4), "state.cars.b.hp", 200);
            At(Record(independent, 4), "state.cars.a.nextPulse", 18); At(Record(independent, 4), "state.cars.b.nextPulse", 21);
            output["independentRecovery"] = independent;
            var repeatedCommands = new JArray(); int eventNumber = 0;
            foreach (int delay in new[] { 10, 15, 20, 20 }) { repeatedCommands.Add(Hit("down-" + eventNumber++, 500)); repeatedCommands.Add(Advance(delay + 2)); }
            var repeated = Replay("capped-recovery", config, repeatedCommands);
            int[] delays = { 10, 15, 20, 20 };
            for (int i = 0; i < 4; i++) Require((long)Record(repeated, 2*i).SelectToken("state.cars.a.recoverAt") - (long)Record(repeated, 2*i).SelectToken("state.tick") == delays[i], "Repeated recovery delay");
            output["repeatedRecovery"] = repeated;
            var retry = new ConvoyFixture(config); retry.Apply((JObject)Hit("down", 500)["value"]); retry.Advance(4);
            var saved = retry.Capture(); var restored = ConvoyFixture.Restore(saved); var restoredBefore = restored.Capture();
            Same(saved, restoredBefore, "Restore all fields");
            var receipt = restored.Apply((JObject)Hit("down", 500)["value"]); Same(saved, restored.Capture(), "Restored retry no mutation");
            retry.Advance(6); restored.Advance(6); Same(retry.Capture(), restored.Capture(), "Restore continued clock");
            output["restoreRetry"] = new JObject { ["before"] = saved, ["restoredBefore"] = restoredBefore, ["duplicateReceipt"] = receipt, ["continuedOriginal"] = retry.Capture(), ["continuedRestored"] = restored.Capture() };
            var repair = Replay("train-repair-retry", config, new JArray(Hit("down", 500), Repair("repair", 80), Hit("later", 100, "b"), Repair("repair", 80)));
            At(Record(repair, 1), "state.cars.a.hp", 0); At(Record(repair, 1), "state.trainHp", 930);
            Same(Record(repair, 1)["receipt"], Record(repair, 3)["receipt"], "Repair retry receipt");
            Same(Record(repair, 2)["state"], Record(repair, 3)["state"], "Repair retry state"); output["trainRepairRetry"] = repair;
            var rejected = new JArray(); var damaged = new ConvoyFixture(config); damaged.Apply((JObject)Hit("down", 500)["value"]);
            var late = damaged.Apply((JObject)Hit("late", 100)["value"]); At(late, "loss", 0); output["lateProjectile"] = late;
            Reject(rejected, damaged, "missing-physical-fallback", () => damaged.Apply((JObject)Hit("rock", 100, "a", "physical")["value"]));
            Reject(rejected, damaged, "event-conflict", () => damaged.Apply((JObject)Hit("down", 499)["value"]));
            Reject(rejected, damaged, "cross-kind-conflict", () => damaged.RepairTrain("down", 500));
            foreach (var invalid in new JToken[] { -1, 1.5, 1.0000000000000002d, double.NaN, double.PositiveInfinity, 9007199254740992L, 9007199254740992d })
            { var bad = (JObject)Hit("bad", 0)["value"]; bad["amount"] = invalid; Reject(rejected, damaged, "numeric-" + invalid.ToString(Formatting.None), () => damaged.Apply(bad)); }
            var numericConfig = (JObject)config.DeepClone(); numericConfig["trainMaxHp"] = ConvoyFixture.MaxSafeInteger;
            numericConfig["cars"] = new JArray(new JObject { ["id"] = "a", ["maxHp"] = ConvoyFixture.MaxSafeInteger, ["devicePeriod"] = 8 });
            var numeric = Replay("max-safe-integer", numericConfig, new JArray(Hit("max", ConvoyFixture.MaxSafeInteger)));
            At(Record(numeric, 0), "receipt.transfer", 2702159776422297L); output["numericMaximum"] = numeric;
            numericConfig["repairTicks"] = new JArray(ConvoyFixture.MaxSafeInteger); var clock = new ConvoyFixture(numericConfig);
            Reject(rejected, clock, "unsafe-tick-sum", () => clock.Advance(1)); output["rejections"] = rejected;
            return output;
        }
        static void Reject(JArray records, ConvoyFixture model, string name, Action action)
        {
            var before = model.Capture(); string reason = null;
            try { action(); } catch (InvalidOperationException error) { reason = error.Message; }
            Require(reason != null, "Expected rejection " + name); var after = model.Capture(); Same(before, after, "Atomic rejection " + name);
            records.Add(new JObject { ["name"] = name, ["error"] = reason, ["before"] = before, ["after"] = after });
        }
        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-kyleOutput");
                Require(index >= 0 && index + 1 < args.Length, "Output required");
                string output = Path.GetFullPath(args[index + 1]);
                Require(output.StartsWith(Path.GetFullPath(Root + "/Unity/"), StringComparison.OrdinalIgnoreCase) && !Directory.Exists(output), "Fresh Unity-owned D QA output required");
                var evidence = Verify(); Directory.CreateDirectory(output);
                string file = Path.Combine(output, "verification.json"); File.WriteAllText(file, evidence.ToString(Formatting.Indented) + "\n");
                File.WriteAllText(Path.Combine(output, "manifest.json"), new JObject { ["file"] = "verification.json", ["sha256"] = Hash(file), ["result"] = "PASS" }.ToString(Formatting.Indented) + "\n");
                Debug.Log("Carriage v2 PASS: 4 scenarios / 11 records / 4 restored snapshots; additional boundaries and Player exclusion; " + file);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
