#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastRail.Diagnostics
{
    internal static class BattleDComparisonRoster
    {
        const string NyxFrame = "03253cad690228291bba9995ee05657f4e47baaff96babb70567a704601834a5";
        internal static CombatSceneDriver CreateDriver(int initialShield = 2800, long shieldExpiry = long.MaxValue)
        {
            var roof = LastRail.Resources.PreparedContent.Load<OrdinaryRoofPresentationAssets>("CombatPresentation/OrdinaryRoof");
            var slots = (JArray)JObject.Parse(roof.Definition.text)["slots"];
            return new CombatSceneDriver(() => BasicCombatSimulation.CreateDComparison(slots, initialShield, shieldExpiry), () => null, (_, revision) => checked(revision + 1), () => true);
        }

        internal static CombatPresentationAssets CreateArt()
        {
            var source = LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation");
            var bytes = File.ReadAllBytes("D:/Project/LASTRAIL/QA/SideViewReuse-20261023/assets/" + NyxFrame + ".png");
            using (var sha = SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() != NyxFrame)
                    throw new InvalidDataException("Modified D comparison Nyx idle.");
            var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            CombatPresentationAssets art = null;
            CombatOperatorFrames registry = null;
            try
            {
                if (!frame.LoadImage(bytes, true)) throw new InvalidDataException("Unreadable Nyx idle.");
                art = Object.Instantiate(source);
                registry = Object.Instantiate(source.OperatorFrames);
                var nyx = new CombatOperatorFrames.Entry {
                    OperatorId = "op_nyx", CanvasSize = new Vector2(1263, 1246), FootAnchorTopLeft = new Vector2(907, 1223),
                    BodySpan = 1186, IdleFrames = new[] { frame }, DurationsMs = new[] { 180 },
                    Portrait = LastRail.Resources.PreparedContent.Load<Texture2D>("OperatorIdentity/op_nyx")
                };
                registry.Entries = registry.Entries.Concat(new[] { nyx }).ToArray();
                registry.Validate();
                art.OperatorFrames = registry;
                return art;
            }
            catch
            {
                Object.Destroy(frame); Object.Destroy(registry); Object.Destroy(art);
                throw;
            }
        }

        internal static void DestroyArt(CombatPresentationAssets art)
        {
            if (art == null) return;
            Object.Destroy(art.OperatorFrames.Find("op_nyx").IdleFrames[0]);
            Object.Destroy(art.OperatorFrames);
            Object.Destroy(art);
        }
    }
}

namespace LastRail.Battle
{
    public sealed partial class BasicCombatSimulation
    {
        sealed class ComparisonRoof
        {
            readonly string[] cars = { "d-qa-engine", "d-qa-combat", "d-qa-gun" };
            readonly (int order, int x)[] positions;
            internal ComparisonRoof(JArray source)
            {
                if (source.Count != 5) throw new InvalidDataException("Five comparison anchors required.");
                positions = Enumerable.Range(0, 5).Select(i => {
                    var row = source.Single(s => (int)s["formationSlot"] == i);
                    return ((int)row["vehicleOrder"], (int)row["localPositionMm"][0]);
                }).ToArray();
            }
            internal void Write(JObject hud)
            {
                // This stationary comparison scene uses an explicit local origin, not a run position.
                hud["ordinaryRail"] = new JObject { ["physicalInstanceIds"] = JArray.FromObject(cars), ["frontArcMm"] = 0 };
                foreach (var actor in (JArray)hud["actors"])
                {
                    if ((bool)actor["enemy"] || (bool)actor["train"]) continue;
                    var p = positions[(int)actor["slot"]];
                    actor["physicalInstanceId"] = cars[p.order];
                    actor["physicalOrder"] = p.order;
                    actor["localXmm"] = p.x;
                }
            }
        }
        ComparisonRoof comparisonRoof;

        internal static BasicCombatSimulation CreateDComparison(JArray slots, int initialShield, long shieldExpiry)
        {
            string[] ids = { "op_reina", "op_kyle", "op_eve", "op_sera", "op_nyx" };
            int[] charge = { 100, 70, 50, 40, 32 }; // Rounded from the v07 web comparison at 2370 ms.
            var characters = ids.Select((id, i) => new BasicCombatActor(id, false, false, false, i, 100, 0, 0, 100, 0, 0, BasicCombatTarget.EnemyAny, 0));
            var train = new BasicCombatActor("d-qa-train", false, true, false, 0, 4800, 0, 0, 100, 0, 0, BasicCombatTarget.EnemyAny, 5834);
            var deferredEnemy = new BasicCombatActor("d-qa-deferred", true, false, true, 0, 1, 0, 0, 100, 0, 0, BasicCombatTarget.Train, 0, new BasicCombatTiming(1000000, 1000000, null, false, null));
            var model = new BasicCombatSimulation("d-comparison-local-v1", 500000, 848, characters.Concat(new[] { train }).ToArray(), new[] { new[] { deferredEnemy } });
            model.frontSlots = new[] { 3, 4 }; // Retained legacy cabin layout; exterior roof positions come from its own five anchors.
            // Use the retained CE03 cruise settings; the comparison still supplies no junction offer.
            model.ConfigureRailJourney(new JObject {
                ["version"] = "rail-journey-v1", ["selectionScaleBps"] = 100, ["recoveryMs"] = 600,
                ["baseSpeedMillimetersPerSecond"] = 10000, ["throttleMinimumBps"] = 7000,
                ["throttleCruiseBps"] = 10000, ["throttleMaximumBps"] = 13000, ["offers"] = new JArray()
            });
            model.comparisonRoof = new ComparisonRoof(slots);
            model.skillStepMs = 50;
            model.queueExpiryMs = 500;
            model.manualDisplayOrder = Array.Empty<string>();
            var trainActor = model.allies.Single(a => a.Config.Train);
            if (initialShield != 0 && initialShield != 2800) throw new ArgumentOutOfRangeException(nameof(initialShield));
            if (initialShield != 0 && trainActor.Vitals.Shield(new CombatShieldSource(train.Id, "d-comparison-initial-shield", 0), initialShield, shieldExpiry, 0) != initialShield)
                throw new InvalidDataException("D comparison shield capacity mismatch.");
            foreach (var actor in model.allies.Where(a => !a.Config.Train))
            {
                // Exercise the real charge/command owner without attributing an unverified kit or damage.
                string id = "d-comparison-ultimate";
                var row = new JObject { ["id"] = id, ["kind"] = "ULTIMATE", ["target"] = "self", ["windupMs"] = 0, ["cooldownMs"] = 0, ["recoveryMs"] = 0, ["effects"] = new JArray() };
                model.skills.Add(actor.Config.Id + "/" + id, new Skill { Caster = actor, Data = row });
            }
            model.ConfigureLocalOperatorAutomation(true);
            for (int i = 0; i < ids.Length; ++i) model.operatorCharge[ids[i]] = charge[i];
            var hud = model.ReadHudState();
            var actors = hud["actors"].Where(a => !(bool)a["enemy"] && !(bool)a["train"]).OrderBy(a => (int)a["slot"]).ToArray();
            if (!actors.Select(a => (string)a["id"]).SequenceEqual(ids) || !actors.Select(a => (int)a["slot"]).SequenceEqual(Enumerable.Range(0, 5)) ||
                !hud["ultimates"].Select(a => (int)a["charge"]).SequenceEqual(charge))
                throw new InvalidDataException("D comparison roster registration mismatch.");
            return model;
        }
        void WriteDComparisonPlacement(JObject hud)
        {
            if (comparisonRoof == null) return;
            comparisonRoof.Write(hud);
            hud["manualsAvailability"] = skills.Values.Any(s => Text(s.Data, "kind") == "TRAIN_MANUAL") ? "AVAILABLE" : "UNAVAILABLE";
            var train = allies.Single(a => a.Config.Train).Config;
            hud["actors"].Single(a => (bool)a["train"])["shieldMaximum"] = (int)((long)train.MaxHp * train.ShieldCapBps / 10000);
        }
    }
}
#endif
