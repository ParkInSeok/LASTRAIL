#if UNITY_EDITOR
using System;
using System.IO;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace LastRail.EditorSetup
{
    public static class LastRailUnityMcpSetup
    {
        [MenuItem("LASTRAIL/Tools/Connect Local Unity MCP")]
        public static async void Connect()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!string.Equals(root.TrimEnd('\\', '/'), @"D:\Project\LASTRAIL\unity-project", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unity MCP setup is limited to the LASTRAIL project.");
            Environment.SetEnvironmentVariable("UNITY_MCP_DISABLE_TELEMETRY", "true");
            EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled", true);
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:18888");
            EditorPrefs.SetString("MCPForUnity.WebSocketUrl", "");
            EditorPrefs.SetBool("MCPForUnity.Security.AllowLanHttpBind", false);
            EditorPrefs.SetBool("MCPForUnity.Security.AllowInsecureRemoteHttp", false);
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", false);
            EditorPrefs.SetBool("MCPForUnity.SetupDismissed", true);
            try
            {
                var connected = await MCPServiceLocator.Bridge.StartAsync();
                Debug.Log("LASTRAIL_MCP_CONNECT=" + connected);
            }
            catch (Exception error)
            {
                Debug.LogError("LASTRAIL_MCP_CONNECT_FAILED: " + error.GetType().Name);
            }
        }
    }
}
#endif
