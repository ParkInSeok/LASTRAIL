using LastRail.Battle;
using Newtonsoft.Json.Linq;

// Reads a server-owned immutable prefix. Never creates acceptance from a client checkpoint.
internal static class ReplayPrefix
{
    public static BasicCombatSimulation Restore(JObject prefix, OrdinaryFiveCombatFactory factory, JObject checkpoint)
    {
        Fields(prefix, "version", "accountId", "runId", "startSnapshotHash", "configurationSha256", "inputs");
        var model = factory.Create();
        var initial = JObject.Parse(model.CaptureLocalState());
        if (Text(prefix, "version") != "ordinary.replay-prefix.v1.20261028"
            || Text(prefix, "accountId") != factory.Original.AccountId
            || Text(prefix, "runId") != factory.Original.RunId
            || Text(prefix, "startSnapshotHash") != factory.Original.StartSnapshotHash
            || Text(prefix, "configurationSha256") != (string?)initial["configurationSha256"])
            throw new ArgumentException("Accepted prefix origin mismatch.");
        if (prefix["inputs"] is not JArray inputs || inputs.Count > 10000
            || checkpoint["operations"] is not JArray reported || reported.Count != inputs.Count)
            throw new ArgumentException("Accepted prefix coverage mismatch.");
        for (var i = 0; i < inputs.Count; i++)
        {
            if (inputs[i] is not JObject input) throw new ArgumentException("Input object required.");
            Fields(input, "sequence", "tick", "command");
            if (Integer(input, "sequence") != i + 1 || Integer(input, "tick") != model.Tick)
                throw new ArgumentException("Accepted sequence or logical time mismatch.");
            if (input["command"] is not JObject command || !JToken.DeepEquals(command, reported[i]))
                throw new ArgumentException("Submitted input differs from accepted prefix.");
            Fields(command, "kind", "value");
            switch (Text(command, "kind"))
            {
                case "PAUSE": model.SetPaused(Boolean(command, "value")); break;
                case "SPEED": model.SetSpeed(checked((int)Integer(command, "value"))); break;
                case "ULTIMATE_AUTO": model.SetUltimateAuto(Boolean(command, "value")); break;
                default: throw new ArgumentException("Command requires an unsupported execution or time authority.");
            }
        }
        // Compare every field, including state, effects and operation history; no HP-only acceptance.
        if (!JToken.DeepEquals(JObject.Parse(model.CaptureLocalState()), checkpoint))
            throw new ArgumentException("Accepted input replay differs from submitted checkpoint.");
        return model;
    }

    private static void Fields(JObject value, params string[] names)
    {
        if (value.Count != names.Length || names.Any(name => value.Property(name) == null))
            throw new ArgumentException("Unexpected input fields.");
    }
    private static string Text(JObject value, string name) => value[name]?.Type == JTokenType.String
        ? (string)value[name]! : throw new ArgumentException("String required.");
    private static long Integer(JObject value, string name) => value[name]?.Type == JTokenType.Integer
        && (long)value[name]! >= 0 ? (long)value[name]! : throw new ArgumentException("Nonnegative integer required.");
    private static bool Boolean(JObject value, string name) => value[name]?.Type == JTokenType.Boolean
        ? (bool)value[name]! : throw new ArgumentException("Boolean required.");
}
