using System;

namespace LastRail.Presentation.Animation
{
    public static class AnimationAssetPipelineReadinessAudit
    {
        public static AnimationAssetPipelineReadinessAuditResult AuditLaunchPipeline(
            AnimationAssetManifest manifest,
            AnimationAssetPipelineSpec spec,
            string packageManifestJson)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            var coverage = AnimationAssetCoverageAudit.AuditActOneLaunch(manifest);
            return new AnimationAssetPipelineReadinessAuditResult(
                coverage,
                spec,
                IsPackageDeclared(packageManifestJson, spec.RequiredUnityPackageId));
        }

        private static bool IsPackageDeclared(string packageManifestJson, string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageManifestJson))
            {
                return false;
            }

            return packageManifestJson.IndexOf("\"" + packageId + "\"", StringComparison.Ordinal) >= 0;
        }
    }
}
