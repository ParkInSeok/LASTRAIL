using System;
using NUnit.Framework;
using LastRail.Operators;

namespace LastRail.Tests.EditMode
{
    public sealed class ActOneOperatorRosterRulesTests
    {
        [Test]
        public void CreateInitialRoster_GrantsFiveFreeOperators()
        {
            var roster = ActOneOperatorRosterRules.CreateInitialRoster();

            Assert.That(roster.OwnedOperatorIds.Count, Is.EqualTo(5));
            Assert.That(roster.OwnedOperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(roster.OwnedOperatorIds[1], Is.EqualTo("op_reina"));
            Assert.That(roster.OwnedOperatorIds[2], Is.EqualTo("op_eve"));
            Assert.That(roster.OwnedOperatorIds[3], Is.EqualTo("op_sera"));
            Assert.That(roster.OwnedOperatorIds[4], Is.EqualTo("op_mira"));
            Assert.That(roster.IsOwned("op_kyle"), Is.EqualTo(true));
            Assert.That(roster.IsOwned("op_vex"), Is.EqualTo(false));
        }

        [Test]
        public void CreateInitialRoster_SeparatesRecruitableEliteOperatorsFromOwnedOperators()
        {
            var roster = ActOneOperatorRosterRules.CreateInitialRoster();

            Assert.That(roster.RecruitableOperatorIds.Count, Is.EqualTo(10));
            Assert.That(roster.RecruitableOperatorIds[0], Is.EqualTo("op_vex"));
            Assert.That(roster.RecruitableOperatorIds[1], Is.EqualTo("op_nyx"));
            Assert.That(roster.RecruitableOperatorIds[2], Is.EqualTo("op_sable"));
            Assert.That(roster.RecruitableOperatorIds[3], Is.EqualTo("op_marrow"));
            Assert.That(roster.RecruitableOperatorIds[4], Is.EqualTo("op_iris"));
            Assert.That(roster.RecruitableOperatorIds[5], Is.EqualTo("op_rail"));
            Assert.That(roster.RecruitableOperatorIds[6], Is.EqualTo("op_anchor"));
            Assert.That(roster.RecruitableOperatorIds[7], Is.EqualTo("op_lumen"));
            Assert.That(roster.RecruitableOperatorIds[8], Is.EqualTo("op_cinder"));
            Assert.That(roster.RecruitableOperatorIds[9], Is.EqualTo("op_warden"));
            Assert.That(roster.IsRecruitable("op_vex"), Is.EqualTo(true));
            Assert.That(roster.IsRecruitable("op_kyle"), Is.EqualTo(false));
            Assert.That(roster.IsRecruitable("op_rin"), Is.EqualTo(false));
            Assert.That(roster.IsRecruitable("op_cipher"), Is.EqualTo(false));
        }

        [Test]
        public void StableIds_ExposeOperatorIdsForServerInitialAccountState()
        {
            Assert.That(ActOneOperatorRosterRules.KyleOperatorId, Is.EqualTo("op_kyle"));
            Assert.That(ActOneOperatorRosterRules.ReinaOperatorId, Is.EqualTo("op_reina"));
            Assert.That(ActOneOperatorRosterRules.EveOperatorId, Is.EqualTo("op_eve"));
            Assert.That(ActOneOperatorRosterRules.SeraOperatorId, Is.EqualTo("op_sera"));
            Assert.That(ActOneOperatorRosterRules.MiraOperatorId, Is.EqualTo("op_mira"));
            Assert.That(ActOneOperatorRosterRules.VexOperatorId, Is.EqualTo("op_vex"));
            Assert.That(ActOneOperatorRosterRules.NyxOperatorId, Is.EqualTo("op_nyx"));
            Assert.That(ActOneOperatorRosterRules.SableOperatorId, Is.EqualTo("op_sable"));
            Assert.That(ActOneOperatorRosterRules.MarrowOperatorId, Is.EqualTo("op_marrow"));
            Assert.That(ActOneOperatorRosterRules.IrisOperatorId, Is.EqualTo("op_iris"));
            Assert.That(ActOneOperatorRosterRules.RailOperatorId, Is.EqualTo("op_rail"));
            Assert.That(ActOneOperatorRosterRules.AnchorOperatorId, Is.EqualTo("op_anchor"));
            Assert.That(ActOneOperatorRosterRules.LumenOperatorId, Is.EqualTo("op_lumen"));
            Assert.That(ActOneOperatorRosterRules.CinderOperatorId, Is.EqualTo("op_cinder"));
            Assert.That(ActOneOperatorRosterRules.WardenOperatorId, Is.EqualTo("op_warden"));
        }

        [Test]
        public void StableIdLists_SeparateInitialGachaEliteAndVisualScopes()
        {
            var initial = ActOneOperatorRosterRules.CreateInitialOperatorIds();
            var gachaElite = ActOneOperatorRosterRules.CreateGachaEliteOperatorIds();
            var visualScope = ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds();

            Assert.That(initial.Count, Is.EqualTo(5));
            Assert.That(gachaElite.Count, Is.EqualTo(10));
            Assert.That(visualScope.Count, Is.EqualTo(15));
            Assert.That(visualScope[0], Is.EqualTo("op_kyle"));
            Assert.That(visualScope[4], Is.EqualTo("op_mira"));
            Assert.That(visualScope[5], Is.EqualTo("op_vex"));
            Assert.That(visualScope[14], Is.EqualTo("op_warden"));
            Assert.That(ActOneOperatorRosterRules.IsLaunchGachaEliteOperator("op_vex"), Is.EqualTo(true));
            Assert.That(ActOneOperatorRosterRules.IsLaunchGachaEliteOperator("op_kyle"), Is.EqualTo(false));
            Assert.That(ActOneOperatorRosterRules.IsLaunchVisualOperator("op_warden"), Is.EqualTo(true));
            Assert.That(ActOneOperatorRosterRules.IsLaunchVisualOperator("op_cipher"), Is.EqualTo(false));
        }

        [Test]
        public void OperatorRoster_CopiesInputCollections()
        {
            var owned = new[] { "op_kyle" };
            var recruitable = new[] { "op_vex" };

            var roster = new OperatorRoster(owned, recruitable);
            owned[0] = "op_reina";
            recruitable[0] = "op_rin";

            Assert.That(roster.OwnedOperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(roster.RecruitableOperatorIds[0], Is.EqualTo("op_vex"));
        }

        [Test]
        public void OperatorRoster_RejectsDuplicateOwnedAndRecruitableOperatorIds()
        {
            Assert.Throws<InvalidOperationException>(() => new OperatorRoster(
                new[] { "op_kyle" },
                new[] { "op_kyle" }));
        }
    }
}
