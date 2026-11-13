using System;
using System.IO;
using LastRail.Editor.Signing;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "crash-fixture")
        {
            var abandoned = new BuildRegistry(args[1]).Reserve(BuildRequest.Parse(new[] { "server=DEV", "version=1" }));
            Environment.Exit(0);
        }
        try
        {
            Check(AndroidBuildPolicy.SelectDefines("FEATURE_A;LASTRAIL_ENV_PRD;FEATURE_B", "DEV") == "FEATURE_A;FEATURE_B;LASTRAIL_ENV_DEV", "define replacement");
            Check(AndroidBuildPolicy.RestoreDefines("FEATURE_A;FEATURE_B;LASTRAIL_ENV_DEV", "FEATURE_A;LASTRAIL_ENV_PRD;FEATURE_B", "DEV") == "FEATURE_A;LASTRAIL_ENV_PRD;FEATURE_B", "exact symbol restoration");
            Check(AndroidBuildPolicy.RestoreDefines("FEATURE_NEW;LASTRAIL_ENV_DEV", "FEATURE_OLD;LASTRAIL_ENV_PRD", "DEV") == "FEATURE_NEW;LASTRAIL_ENV_PRD", "preserve later feature edits");
            Reject(() => AndroidBuildPolicy.SelectDefines("LASTRAIL_ENV_DEV;LASTRAIL_ENV_PRD", "DEV"), "conflicting defines");
            Reject(() => AndroidBuildPolicy.SelectDefines("FEATURE", "QA"), "invalid server");
            AndroidBuildPolicy.ValidateContentRoot("DEV", "https://dev-content.lastrail.app/android/releases/approved-fixture/");
            Console.WriteLine("PASS approved DEV content authority");
            Reject(() => AndroidBuildPolicy.ValidateContentRoot("DEV", "https://content.lastrail.app/android/releases/release/"), "swapped content authority");
            Reject(() => AndroidBuildPolicy.ValidateContentRoot("DEV", "https://dev-content.lastrail.app.evil.example/android/releases/release/"), "lookalike authority");
            Reject(() => AndroidBuildPolicy.ValidateContentRoot("DEV", "https://dev-content.lastrail.app/android/releases/../other/"), "noncanonical release path");
            Reject(() => AndroidBuildPolicy.ValidateContentRoot("PRD", "https://dev-content.lastrail.app/android/releases/release/"), "PRD has no approved root yet");
            var request = BuildRequest.Parse(new[] { "server=DEV", "version=0.1.1" });
            Check(request.Package == "app.lastrail.game.dev" && request.VersionCode == null && request.Format == "apk", "DEV arguments");
            var production = BuildRequest.Parse(new[] { "server=PRD", "version=0.1.1", "versionCode=15", "format=aab" });
            Check(production.Package == "app.lastrail.game" && production.VersionCode == 15, "explicit PRD code");
            foreach (var code in new[] { "0", "-1", "1.5", "2147483648", "2100000001", "+2" })
                Reject(() => BuildRequest.Parse(new[] { "server=DEV", "version=1", "versionCode=" + code }), "invalid code " + code);
            foreach (var version in new[] { "../x", "a/b", "a\\b", "", "a:1", ".", ".." })
                Reject(() => BuildRequest.Parse(new[] { "server=DEV", "version=" + version }), "unsafe version");
            Reject(() => BuildRequest.Parse(new[] { "version=1" }), "missing server");
            Reject(() => BuildRequest.Parse(new[] { "server=DEV", "server=PRD", "version=1" }), "duplicate server");
            Reject(() => BuildRequest.Parse(new[] { "server=PRD", "version=1", "channel=stable" }), "unknown args");
            var root = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "lastrail-signing-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var registry = new BuildRegistry(root);
                Reject(() => registry.Reserve(request), "unknown baseline");
                registry.Reconcile("DEV", 1, 1);
                registry.Reconcile("PRD", 10, 12);
                using (var lease = registry.Reserve(request))
                {
                    Check(lease.Code == 2, "auto increment");
                    Reject(() => registry.Reserve(request), "concurrent reservation");
                    using (var other = registry.Reserve(production)) Check(other.Code == 15, "independent env");
                }
                using (var lease = registry.Reserve(request))
                {
                    Check(lease.Code == 2, "cancel consumes no code");
                    lease.CommitVerifiedSuccess();
                }
                using (var lease = registry.Reserve(request)) Check(lease.Code == 3, "successful counter");
                Reject(() => registry.Reserve(BuildRequest.Parse(new[] { "server=DEV", "version=9", "versionCode=2" })), "regression");
                Reject(() => registry.Reserve(BuildRequest.Parse(new[] { "server=PRD", "version=1", "versionCode=12" })), "published floor");
                using (var lease = registry.Reserve(BuildRequest.Parse(new[] { "server=PRD", "version=1", "format=apk" })))
                    Check(lease.Code == 13, "format shares published counter");
                Reject(() => registry.Reconcile("DEV", 0, 0), "baseline cannot regress");
                using (var child = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath)
                    { UseShellExecute = false, ArgumentList = { "crash-fixture", root } }))
                { child.WaitForExit(); Check(child.ExitCode == 0, "crash fixture exited"); }
                Reject(() => registry.Reserve(request), "interrupted reservation blocks reuse");
                Reject(() => registry.ResolveInterrupted("DEV", 3, false), "recovery requires explicit confirmation");
                registry.ResolveInterrupted("DEV", 3, true);
                using (var lease = registry.Reserve(request)) Check(lease.Code == 3, "confirmed unpublished failure can retry");
                registry.Reconcile("PRD", 2100000000, 2100000000);
                Reject(() => registry.Reserve(production), "explicit code never bumped");
                Reject(() => registry.Reserve(BuildRequest.Parse(new[] { "server=PRD", "version=1" })), "overflow");
            }
            finally { Directory.Delete(root, true); }
            Console.WriteLine("ALL POLICY CHECKS PASSED");
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL " + error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is IOException)
        { Console.WriteLine("PASS reject " + name); return; }
        throw new Exception("Expected rejection: " + name);
    }

    private static void Reject(Func<BuildLease> action, string name)
    {
        Reject(() => { using (action()) { } }, name);
    }
}
