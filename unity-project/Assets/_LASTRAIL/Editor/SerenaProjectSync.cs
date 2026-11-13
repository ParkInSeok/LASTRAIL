using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor
{
    public static class SerenaProjectSync
    {
        [MenuItem("LASTRAIL/Tools/Regenerate Code Analysis Projects")]
        public static void Sync()
        {
            // Use Unity's official generator without changing the user's selected external editor.
            var type = Type.GetType("Microsoft.Unity.VisualStudio.Editor.LegacyStyleProjectGeneration, Unity.VisualStudio.Editor", true);
            type.GetMethod("Sync").Invoke(Activator.CreateInstance(type), null);
            var root = Directory.GetParent(Application.dataPath).FullName;
            if (Directory.GetFiles(root, "*.csproj").Length == 0)
                throw new InvalidOperationException("Unity did not generate C# projects.");
            Debug.Log("SerenaProjectSync: official Unity C# projects generated.");
        }
    }
}
