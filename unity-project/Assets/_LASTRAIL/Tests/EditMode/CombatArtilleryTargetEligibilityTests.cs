using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class CombatArtilleryTargetEligibilityTests
{
    static BasicCombatActor Actor(string id, bool train, BasicCombatTiming timing = null)
        => new BasicCombatActor(id, false, train, false, 0, 10000, 10, 0, 1, 0, 0,
            BasicCombatTarget.EnemyExterior, 4000, timing);

    static BasicCombatSimulation Fresh(string sourceKind, JObject skills = null)
    {
        var train = Actor("train", true);
        var actors = skills == null
            ? new[] { train, Actor("source", false, sourceKind == "Operator" ? null :
                new BasicCombatTiming(0, 0, sourceKind, false, null)) }
            : ((JArray)skills["operators"]).Select(op => Actor((string)op["operatorId"], false))
                .Concat(new[] { train }).ToArray();
        // The interior required enemy keeps the encounter open while only artillery is exterior.
        var enemy = new BasicCombatActor("required", true, false, false, 0, 100000, 0, 0,
            1000, 0, 0, BasicCombatTarget.Train, 0);
        var model = new BasicCombatSimulation("artillery-eligibility-test", 500000, 1, actors,
            new[] { new[] { enemy } });
        if (skills != null) model.ConfigureSkills(skills, "op_reina", 50);
        var rail = CombatRailJourneyTests.Config();
        rail["offers"] = new JArray();
        var artillery = CombatRailArtilleryTests.Config();
        artillery["encounters"][0]["spawnWave"] = 1;
        rail["artillery"] = artillery;
        model.ConfigureRailJourney(rail);
        return model;
    }

    static JObject Template() => (JObject)JObject.Parse(File.ReadAllText(
        "D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"))["draft"]["template"];

    [TestCase("Operator", false)]
    [TestCase("Combat", false)]
    [TestCase("Gun", true)]
    public void OnlyTrainWeaponsCanSelectArtilleryForBasicAttack(string sourceKind, bool allowed)
    {
        var model = Fresh(sourceKind);
        for (int i = 0; i < 5; i++) model.Advance(500000, 8);
        Assert.AreEqual(allowed, model.Events.Any(hit => hit.Source == "source" && hit.Target == "gun-2"));
        Assert.IsTrue(model.Events.Any(hit => hit.Source == "train" && hit.Target == "gun-2"));
        Assert.AreEqual(BasicCombatOutcome.Running, model.Outcome);
        var restored = BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(), () => Fresh(sourceKind));
        Assert.AreEqual(model.CaptureLocalState(), restored.CaptureLocalState());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OperatorSkillCannotReachArtilleryThroughTargetOrEffectOverride(bool effectOverride)
    {
        var template = Template();
        var active = template["operators"].Single(op => (string)op["operatorId"] == "op_reina")["active"];
        active["target"] = effectOverride ? "self" : "EXTERIOR_ENEMY_FOCUS";
        if (effectOverride) active["effects"][0]["targetOverride"] = "EXTERIOR_ENEMY_FOCUS";
        var model = Fresh("Operator", template);
        model.Advance(500000, 8);
        model.RequestSkill("operator", "op_reina", (string)active["id"]);
        model.RequestSkill("train", "train", "support_fire");
        for (int i = 0; i < 30; i++) model.Advance(500000, 8);
        Assert.AreEqual(effectOverride ? "APPLIED" : "REJECTED_TARGET", model.CommandStatus("operator"));
        Assert.IsFalse(((JArray)model.ReadHudState()["effectResults"]).Any(result =>
            (string)result["commandId"] == "operator" && (string)result["kind"] == "DAMAGE"));
        Assert.AreEqual("APPLIED", model.CommandStatus("train"));
    }
}
