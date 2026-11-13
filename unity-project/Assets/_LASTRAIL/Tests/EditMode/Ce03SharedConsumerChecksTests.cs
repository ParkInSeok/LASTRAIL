using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class Ce03SharedConsumerChecksTests
{
    [Test] public void SharedChecksExecuteAllTenLegacyPolicyCategoriesAgainstActualPrepare()
    {
        var type=typeof(Ce03CommandContract).Assembly.GetType("LastRail.Diagnostics.Ce03SharedConsumerChecks");
        Assert.IsNotNull(type,"Player and Editor need the same executed model checks.");
        var prepared=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattlePlayer435-prepare/prepare.json"));
        var instance=Activator.CreateInstance(type,new object[]{prepared});
        var output="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/SharedChecks-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(output);
        var fixture=File.ReadAllText("QA/Ce03CommandIntegration/synthetic-r6-transport.json");
        type.GetMethod("Run").Invoke(instance,new object[]{output,fixture});
        var results=(JArray)type.GetProperty("Results").GetValue(instance);
        Assert.AreEqual(10,results.Count);Assert.AreEqual(10,results.Select(x=>(string)x["coverage"]).Distinct().Count());
        Assert.IsTrue(results.All(x=>(string)x["status"]=="PASS"));
    }
}
