using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor
{
    public static class McpDependencyQa
    {
        public static async void Connect()
        {
            try
            {
                Verify();
                EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
                EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
                EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
                var locator = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator")).First(t => t != null);
                var manager = locator.GetProperty("TransportManager").GetValue(null);
                var method = manager.GetType().GetMethod("StartAsync");
                var mode = Enum.Parse(method.GetParameters()[0].ParameterType, "Http");
                if (!await (System.Threading.Tasks.Task<bool>)method.Invoke(manager, new[] { mode })) throw new InvalidOperationException("Unity MCP transport did not connect.");
                Debug.Log("LASTRAIL_MCP_CONNECTED");
            }
            catch (Exception error) { Debug.LogException(error); }
        }
        public static void Verify()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCPForUnity.Editor.Dependencies.PlatformDetectors.WindowsPlatformDetector")).First(t => t != null);
            var detector = Activator.CreateInstance(type);
            var lines = new System.Collections.Generic.List<string>();
            foreach (var method in new[] { "DetectPython", "DetectUv" })
            {
                var status = type.GetMethod(method).Invoke(detector, null);
                var model = status.GetType();
                var available = (bool)model.GetProperty("IsAvailable").GetValue(status);
                lines.Add(method + ": available=" + available + ", version=" + model.GetProperty("Version").GetValue(status) + ", path=" + model.GetProperty("Path").GetValue(status));
                if (!available) throw new InvalidOperationException(string.Join("\n", lines));
            }
            // Use the package's supported override when Hub still carries an older PATH.
            EditorPrefs.SetString("MCPForUnity.UvxPath", "D:/CodexData/Tools/bin/uvx.exe");
            File.WriteAllLines("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/dev-bundle-20261111/v02/mcp-dependencies.txt", lines);
            Debug.Log(string.Join("\n", lines));
        }
    }
}
