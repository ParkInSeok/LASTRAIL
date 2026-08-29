using System;
using System.Collections.Generic;

namespace LastRail.Operators
{
    public static class ActOneOperatorRosterRules
    {
        public const string KyleOperatorId = "op_kyle";
        public const string ReinaOperatorId = "op_reina";
        public const string EveOperatorId = "op_eve";
        public const string SeraOperatorId = "op_sera";
        public const string MiraOperatorId = "op_mira";

        public const string VexOperatorId = "op_vex";
        public const string NyxOperatorId = "op_nyx";
        public const string SableOperatorId = "op_sable";
        public const string MarrowOperatorId = "op_marrow";
        public const string IrisOperatorId = "op_iris";
        public const string RailOperatorId = "op_rail";
        public const string AnchorOperatorId = "op_anchor";
        public const string LumenOperatorId = "op_lumen";
        public const string CinderOperatorId = "op_cinder";
        public const string WardenOperatorId = "op_warden";

        private static readonly string[] InitialOperatorIds =
        {
            KyleOperatorId,
            ReinaOperatorId,
            EveOperatorId,
            SeraOperatorId,
            MiraOperatorId
        };

        private static readonly string[] GachaEliteOperatorIds =
        {
            VexOperatorId,
            NyxOperatorId,
            SableOperatorId,
            MarrowOperatorId,
            IrisOperatorId,
            RailOperatorId,
            AnchorOperatorId,
            LumenOperatorId,
            CinderOperatorId,
            WardenOperatorId
        };

        public static OperatorRoster CreateInitialRoster()
        {
            return new OperatorRoster(
                CreateInitialOperatorIds(),
                CreateGachaEliteOperatorIds());
        }

        public static IReadOnlyList<string> CreateInitialOperatorIds()
        {
            return Copy(InitialOperatorIds);
        }

        public static IReadOnlyList<string> CreateGachaEliteOperatorIds()
        {
            return Copy(GachaEliteOperatorIds);
        }

        public static IReadOnlyList<string> CreateLaunchVisualOperatorIds()
        {
            var operatorIds = new string[InitialOperatorIds.Length + GachaEliteOperatorIds.Length];
            Array.Copy(InitialOperatorIds, 0, operatorIds, 0, InitialOperatorIds.Length);
            Array.Copy(GachaEliteOperatorIds, 0, operatorIds, InitialOperatorIds.Length, GachaEliteOperatorIds.Length);
            return operatorIds;
        }

        public static bool IsLaunchGachaEliteOperator(string operatorId)
        {
            return Contains(GachaEliteOperatorIds, operatorId);
        }

        public static bool IsLaunchVisualOperator(string operatorId)
        {
            return Contains(InitialOperatorIds, operatorId) || Contains(GachaEliteOperatorIds, operatorId);
        }

        private static IReadOnlyList<string> Copy(IReadOnlyList<string> values)
        {
            var copy = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                copy[i] = values[i];
            }

            return copy;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
