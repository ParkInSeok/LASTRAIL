#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor.Signing
{
    public sealed class AndroidSignedBuildWindow : EditorWindow
    {
        [SerializeField] private int server;
        [SerializeField] private int format;
        [SerializeField] private string version = "";
        [SerializeField] private string code = "";
        [SerializeField] private string successful = "";
        [SerializeField] private string published = "";
        [SerializeField] private bool publishDevInternal;
        [SerializeField] private string contentCandidate = "";
        [SerializeField] private bool legacyCutoverAcknowledged;
        [NonSerialized] private string password = "";
        [NonSerialized] private string status = "";

        [MenuItem("LASTRAIL/Build/Signed Android")]
        private static void Open() => GetWindow<AndroidSignedBuildWindow>("Android Build");

        private void OnGUI()
        {
            server = EditorGUILayout.Popup("Server", server, new[] { "DEV", "PRD" });
            format = EditorGUILayout.Popup("Format", format, new[] { "APK", "AAB" });
            version = EditorGUILayout.TextField("Version", version);
            code = EditorGUILayout.TextField("Version Code (optional)", code);
            password = EditorGUILayout.PasswordField("Signing Password", password ?? "");
            publishDevInternal = EditorGUILayout.Toggle("DEV Publication Candidate", publishDevInternal);
            if (publishDevInternal)
            {
                contentCandidate = EditorGUILayout.TextField("Content Candidate", contentCandidate);
                if (GUILayout.Button("Acknowledge First Registry Cutover"))
                    legacyCutoverAcknowledged = EditorUtility.DisplayDialog("First DEV Registry Cutover", "First registration blocks all unregistered legacy version codes. Confirm only after reviewing the exact cutover scope.", "Acknowledge", "Cancel");
            }
            if (GUILayout.Button("Prepare Environment")) Perform(() => AndroidSignedBuild.Prepare(Request()));
            if (GUILayout.Button("Build Signed Artifact"))
            {
                try { Perform(() => {
                    if (publishDevInternal) DevPlayPublisher.BuildPublicationCandidate(Request(), password, contentCandidate);
                    else AndroidSignedBuild.Build(Request(), password);
                }); }
                finally { password = ""; }
            }
            if (GUILayout.Button("Cancel / Restore")) Perform(AndroidSignedBuild.Cancel);
            if (GUILayout.Button("Resume DEV Publication")) Perform(() => {
                var path = EditorUtility.OpenFilePanel("Verified DEV Publication Manifest", "Builds/Android/DEV", "json");
                if (!string.IsNullOrEmpty(path)) DevPlayPublisher.PublishExisting(path, legacyCutoverAcknowledged);
            });
            EditorGUILayout.Space();
            successful = EditorGUILayout.TextField("Confirmed Successful Code", successful);
            published = EditorGUILayout.TextField("Confirmed Uploaded Code", published);
            if (GUILayout.Button("Record Confirmed Baselines")) Perform(() => {
                if (!int.TryParse(successful, out var built) || !int.TryParse(published, out var uploaded))
                    throw new ArgumentException("Explicit integer baselines are required; blank is not zero.");
                if (EditorUtility.DisplayDialog("Confirm Build History", "These values must come from verified local build and upload history. This does not query Play Console.", "Record", "Cancel"))
                    AndroidSignedBuild.Reconcile(server == 0 ? "DEV" : "PRD", built, uploaded);
            });
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        }

        private BuildRequest Request()
        {
            var arguments = new List<string> { "server=" + (server == 0 ? "DEV" : "PRD"), "version=" + version, "format=" + (format == 0 ? "apk" : "aab") };
            if (!string.IsNullOrEmpty(code)) arguments.Add("versionCode=" + code);
            return BuildRequest.Parse(arguments.ToArray());
        }

        private void Perform(Action action)
        {
            try { action(); status = "Completed."; }
            catch (Exception error) { status = error.Message; }
        }

        private void OnDisable() { password = ""; }
    }
}
#endif
