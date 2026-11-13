using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public class McpExitSafetyTests
    {
        [TestCase("File/Exit", "menu_path")]
        [TestCase("file/exit", "menuPath")]
        [TestCase("File/Quit", "menu_path")]
        public void RemoteQuitIsRejectedBeforeNativeEditorShutdown(string path, string parameter)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("MCPForUnity.Editor.Tools.ExecuteMenuItem")).First(t => t != null);
            // Fail safely if the guard regresses: never invoke the crashing native menu in a test runner.
            var blocked = (HashSet<string>)type.GetField("_menuPathBlacklist", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.That(blocked.Contains(path), Is.True, "Remote shutdown must be guarded before invoking this test.");
            var response = type.GetMethod("HandleCommand").Invoke(null, new object[] { new JObject { [parameter] = path } });
            Assert.That((bool)response.GetType().GetProperty("Success").GetValue(response), Is.False);
            Assert.That((string)response.GetType().GetProperty("Error").GetValue(response), Does.Contain("blocked"));
        }
    }
}
