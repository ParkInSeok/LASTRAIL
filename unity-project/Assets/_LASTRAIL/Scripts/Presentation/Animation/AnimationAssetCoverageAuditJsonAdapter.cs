using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.Animation
{
    public static class AnimationAssetCoverageAuditJsonAdapter
    {
        public static string ToJson(AnimationAssetCoverageAuditResult audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"isComplete\":").Append(audit.IsComplete ? "true" : "false").Append(',');
            builder.Append("\"operatorCount\":").Append(audit.OperatorCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"initialOperatorCount\":").Append(audit.InitialOperatorCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"gachaEliteOperatorCount\":").Append(audit.GachaEliteOperatorCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"enemyCount\":").Append(audit.EnemyCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"raidBossCount\":").Append(audit.RaidBossCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"missingRequirementCount\":").Append(audit.MissingRequirementCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"missingRequirements\":[");
            for (var i = 0; i < audit.MissingRequirements.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var missing = audit.MissingRequirements[i];
                builder.Append('{');
                AppendString(builder, "assetId", missing.AssetId).Append(',');
                AppendString(builder, "role", missing.Role.ToString()).Append(',');
                AppendString(builder, "missingKey", missing.MissingKey);
                builder.Append('}');
            }

            builder.Append("],");
            builder.Append("\"hasRawArtPayload\":false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":\"")
                .Append(Escape(value))
                .Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
