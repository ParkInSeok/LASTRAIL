using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Server-controlled local process only. No upload route, database writer or completion permission.
internal static class Program
{
    private const int MaximumLocalInputBytes = 1024 * 1024;

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--session-lock")
            {
                var path = Path.GetFullPath(args[1]);
                if (!path.StartsWith("D:\\Project\\LASTRAIL\\", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Project-local session lock required.");
                using var held = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                held.SetLength(0);
                var bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { processId = Environment.ProcessId }));
                held.Write(bytes); held.Flush(true);
                Console.WriteLine("LOCKED");
                string? command;
                while ((command = Console.ReadLine()) != null)
                {
                    if (!command.StartsWith("SAVE ", StringComparison.Ordinal) || command.Length > 48 * 1024 * 1024)
                        throw new ArgumentException("Invalid local persistence command.");
                    var snapshot = Convert.FromBase64String(command.Substring(5));
                    var database = Path.Combine(Path.GetDirectoryName(path)!, "session.sqlite");
                    using (var output = new FileStream(database + ".next", FileMode.Create, FileAccess.Write, FileShare.None))
                    { output.Write(snapshot); output.Flush(true); }
                    File.Move(database + ".next", database, true);
                    Console.WriteLine("SAVED");
                }
                return 0;
            }
            if (args.Length == 4 && args[0] == "--local-execution")
            {
                Pin("LastRail.Combat.Core.dll", args[2]);
                Pin("Newtonsoft.Json.dll", args[3]);
                return ExecuteLocal(args[1]);
            }
            if (args.Length != 4 && args.Length != 5) throw new ArgumentException("Expected original, checkpoint, two trusted assembly hashes and optional server prefix.");
            Pin("LastRail.Combat.Core.dll", args[2]);
            Pin("Newtonsoft.Json.dll", args[3]);
            return Replay(args[0], args[1], args.Length == 5 ? args[4] : null);
        }
        catch (Exception error)
        {
            if (args.Length > 0 && args[0] == "--local-execution")
            {
                var cause = error.GetBaseException();
                Console.Error.WriteLine(cause.GetType().Name + ": " + cause.Message);
                Console.Error.WriteLine(cause.StackTrace);
            }
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
                status = "REJECTED", completionAuthorized = false, error = error.GetType().Name
            }));
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ExecuteLocal(string path)
    {
        // Preserve original timestamp strings: date coercion changes the pinned canonical START hash.
        using var reader = new JsonTextReader(new StringReader(ReadBounded(path))) { DateParseHandling = DateParseHandling.None, MaxDepth = 32 };
        var input = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
        if (reader.Read() || input.Descendants().Any(token => token.Type == JTokenType.Comment))
            throw new ArgumentException("Invalid local execution JSON content.");
        if ((string?)input["version"] != "ordinary.local-execution-input.v1.20261106")
            throw new ArgumentException("Local execution input version mismatch.");
        // Fixed Unity-owned entry point, loaded only after independently configured assembly pins pass.
        var assembly = System.Reflection.Assembly.Load("LastRail.Combat.Core");
        var type = assembly.GetType("LastRail.Battle.OrdinaryLocalExecutionReplay", true)!;
        var execute = type.GetMethod("Execute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(JObject) }, null) ?? throw new InvalidOperationException("Shared execution entry point unavailable.");
        var result = execute.Invoke(null, new object[] { input }) as JObject
            ?? throw new InvalidOperationException("Shared execution result unavailable.");
        if ((string?)result["version"] != "ordinary.local-execution-result.v1.20261106")
            throw new InvalidOperationException("Shared execution result version mismatch.");
        Console.WriteLine(result.ToString(Formatting.None));
        return 0;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void Pin(string name, string expected)
    {
        if (expected.Length != 64 || Hash(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, name))) != expected)
            throw new InvalidOperationException("Trusted assembly pin mismatch.");
    }
    private static string ReadBounded(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length > MaximumLocalInputBytes) throw new ArgumentException("Local replay input too large.");
        using var reader = new StreamReader(file, new System.Text.UTF8Encoding(false, true));
        return reader.ReadToEnd();
    }

    // Keep domain assembly loading after pin checks. The original Player code owns every rule.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Replay(string originalPath, string checkpointPath, string? prefixPath)
    {
        using var process = Process.GetCurrentProcess();
        var initialCpu = process.TotalProcessorTime;
        var timer = Stopwatch.StartNew();
        var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
        var source = JObject.Parse(ReadBounded(originalPath), settings);
        var original = OrdinaryFiveStartInput.Read((JObject)source["originalStart"]!, (string)source["accountId"]!, (string)source["runId"]!);
        var factory = OrdinaryFiveCombatFactory.FromInitialization(original, source["initializationResponse"]!.ToString(Formatting.None), 500000L);
        var checkpointText = ReadBounded(checkpointPath);
        var checkpoint = JObject.Parse(checkpointText, settings);
        if ((string?)checkpoint["schema"] != OrdinaryFiveCombatFactory.CheckpointVersion)
            throw new ArgumentException("Unsupported execution version.");
        if (checkpoint["operations"] is not JArray operations || prefixPath == null && operations.Count != 0)
            throw new ArgumentException("Accepted input owner is required for non-empty operations.");
        var model = prefixPath == null
            ? BasicCombatSimulation.RestoreLocalState(checkpointText, factory.Create)
            : ReplayPrefix.Restore(JObject.Parse(ReadBounded(prefixPath), settings), factory, checkpoint);
        var replayed = model.CaptureLocalState();
        var hud = model.ReadHudState();
        timer.Stop(); process.Refresh();
        Console.WriteLine(new JObject {
            ["version"] = "ordinary.shared-replay-host.v1.20261028",
            ["status"] = "REPLAYED_UNVERIFIED", ["completionAuthorized"] = false,
            ["executionVersion"] = OrdinaryFiveCombatFactory.Version,
            ["executionPhase"] = hud["executionPhase"]!.DeepClone(), ["tick"] = model.Tick,
            ["checkpoint"] = replayed, ["checkpointSha256"] = Hash(System.Text.Encoding.UTF8.GetBytes(replayed)),
            ["elapsedMs"] = timer.Elapsed.TotalMilliseconds,
            ["cpuMs"] = (process.TotalProcessorTime - initialCpu).TotalMilliseconds,
            ["peakWorkingSetBytes"] = process.PeakWorkingSet64,
            ["privateMemoryBytes"] = process.PrivateMemorySize64,
            ["acceptedInputVerified"] = false, ["seedAuthorityVerified"] = false,
            ["matchedAcceptedSequence"] = prefixPath == null ? 0 : operations.Count,
            ["prefixConsistencyVerified"] = prefixPath != null
        }.ToString(Formatting.None));
        return 0;
    }
}
