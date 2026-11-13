using System;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class FormationContractTests
    {
        [Test] public void TrainLoadoutHasSeparateReadAndDraftSave()
        {Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshTrainLoadoutAsync"),Is.Not.Null);Assert.That(typeof(AccountScreenRuntime).GetMethod("SaveTrainLoadoutAsync"),Is.Not.Null);}
        [Test] public void TrainGrowthAcceptsOnlyVersionAndOwnedTargetPath()
        {Assert.DoesNotThrow(()=>OperatorGrowthWrite.Validate("growth-train","train_local","{\"economyVersion\":\"v1\"}"));Assert.Throws<FormatException>(()=>OperatorGrowthWrite.Validate("growth-train","train_local","{\"economyVersion\":\"v1\",\"targetLevel\":50}"));}
        [Test] public void RecruitmentHasServerBannerAndBatchReads()
        {Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshBannersAsync"),Is.Not.Null);Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshBatchAsync"),Is.Not.Null);}
        [Test] public void CoreReplacementAndSyncUseStrictDurableBodies()
        {
            Assert.DoesNotThrow(()=>OperatorGrowthWrite.Validate("growth-replace","2","{\"economyVersion\":\"v1\",\"operatorId\":\"op_reina\"}"));
            Assert.DoesNotThrow(()=>OperatorGrowthWrite.Validate("growth-sync","all","{\"economyVersion\":\"v1\"}"));
            Assert.Throws<FormatException>(()=>OperatorGrowthWrite.Validate("growth-replace","2","{\"economyVersion\":\"v1\",\"operatorId\":\"op_reina\",\"refund\":999}"));
            Assert.Throws<FormatException>(()=>OperatorGrowthWrite.Validate("growth-sync","all","{\"economyVersion\":\"v1\",\"targetLevel\":50}"));
        }
        [Test] public void GrowthHasSeparateBoundConfigurationRead()
        {Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshGrowthAsync"),Is.Not.Null);}
        public static string S03Json(){var r=JObject.Parse(AccountStateReadTests.Json());r["formation"]=new JObject{["activationSupported"]=true,["activePresetId"]=null,["activePresetRevision"]=null,["revision"]=0,["operators"]=new JArray(),["updatedAt"]=null,["presets"]=new JArray()};return r.ToString();}
        [Test] public void ExplicitS03EmptyFormationIsSupportedWithoutInventingSelection()
        {var state=AccountStateSnapshot.Parse(S03Json(),"account_test");Assert.That(state.ActivationSupported,Is.True);Assert.That(state.ActivePresetId,Is.Null);}
    }
}
