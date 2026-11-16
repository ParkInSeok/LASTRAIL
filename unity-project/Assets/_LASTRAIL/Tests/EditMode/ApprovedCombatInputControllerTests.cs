using LastRail.Battle;
using NUnit.Framework;

public class ApprovedCombatInputControllerTests
{
    static BasicCombatSimulation Fresh()
    {
        var train=new BasicCombatActor("train",false,true,false,0,10000,10,0,1,0,0,BasicCombatTarget.EnemyAny,10000);
        var waves=new BasicCombatActor[4][];for(int i=0;i<4;i++)waves[i]=new[]{new BasicCombatActor("e"+i,true,false,false,0,1,0,0,1000,0,0,BasicCombatTarget.Train,0)};
        var model=new BasicCombatSimulation("hud-input",500000,1,new[]{train},waves);var config=CombatRailJourneyTests.Config();config["offers"][0]["afterCompletedWaves"]=1;model.ConfigureRailJourney(config);return model;
    }
    [Test] public void SpeedThrottleAndRouteIntentsReuseDriverAndStopAfterUnbind()
    {
        bool active=true;var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
        var input=new ApprovedCombatInputController(driver,()=>active);
        input.CycleSpeed();Assert.AreEqual(3,(int)driver.HudState["speedHalfUnits"]);input.CycleSpeed();Assert.AreEqual(4,(int)driver.HudState["speedHalfUnits"]);input.CycleSpeed();Assert.AreEqual(2,(int)driver.HudState["speedHalfUnits"]);
        input.ChangeThrottle(0);Assert.AreEqual((int)driver.HudState["railJourney"]["minimumBps"],(int)driver.HudState["railJourney"]["throttleBps"]);
        driver.Advance(500000,8);input.PreviewRoute(1);input.ConfirmRoute();Assert.AreEqual("Recovering",(string)driver.HudState["railJourney"]["phase"]);
        var before=driver.StateJson;active=false;input.CycleSpeed();input.ChangeThrottle(1);input.ConfirmRoute();Assert.AreEqual(before,driver.StateJson);
        active=true;input.Dispose();input.CycleSpeed();input.ChangeThrottle(1);Assert.AreEqual(before,driver.StateJson);
    }
}
