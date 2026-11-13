using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class PassRewardDisplay
    {
        public string PolicyId {get;} public int Level {get;} public string Track {get;} public string Status {get;} public string Caption {get;} public string Symbol {get;} public string Payload {get;} public bool CanClaim {get;}
        public PassRewardDisplay(AccountScreenRuntime owner,string policy,JObject row,bool premium)
        {
            PolicyId=policy;Level=(int)row["level"];Track=premium?"PREMIUM":"FREE";Status=(string)row[premium?"premiumState":"freeState"];
            var rewards=(JArray)row[premium?"premium":"free"];Caption=RecurringContract.RewardText(rewards);
            if(premium&&row["cosmeticSlot"]?.Type==JTokenType.String)Caption+=row["cosmeticItemId"]?.Type==JTokenType.String?" · 외형 보상":" · 외형 준비 중";
            var first=rewards.First as JObject;Symbol=(string)first?["kind"]=="CRYSTAL"?"CRYSTAL":(string)first?["id"];
            Payload=new JObject{["level"]=Level,["track"]=Track}.ToString();CanClaim=owner.CanClaimRecurring("recurring-pass",policy,Payload);
        }
    }
    public sealed class SeasonPassDisplayState
    {
        public const int SeasonXpCapacity=5000;
        public IReadOnlyList<PassRewardDisplay> Rewards {get;}
        public IReadOnlyList<string> Objectives {get;}
        public bool ShowObjectives {get;}
        public string PolicyId {get;}
        public int PolicyCount {get;}
        public string Season {get;}
        public string ProgressText {get;}
        public float Progress {get;}
        public string Entitlement {get;}
        public string Notice {get;}
        public bool CanRefresh {get;}
        public bool CanBulk {get;}
        public string RefreshText {get;}
        public string BulkText {get;}
        public SeasonPassDisplayState(AccountScreenRuntime owner,int policyIndex,bool showObjectives)
        {
            ShowObjectives=showObjectives;var data=owner?.RecurringReady==true&&!owner.RecurringLoading?owner.Recurring:null;
            var policies=data?.Policies.Cast<JObject>().Where(p=>(string)p["system"]=="PASS50").OrderByDescending(p=>(long)p["startAt"]).ToArray()??Array.Empty<JObject>();PolicyCount=policies.Length;
            var rows=new List<PassRewardDisplay>();var objectives=new List<string>();
            var policy=policies.Length==0?null:policies[Math.Max(0,Math.Min(policyIndex,policies.Length-1))];
            if(policy!=null)
            {
                PolicyId=(string)policy["policyId"];Season=DateTimeOffset.FromUnixTimeMilliseconds((long)policy["startAt"]).ToOffset(TimeSpan.FromHours(9)).ToString("yyyy년 MM월");
                ProgressText="Lv."+(int)policy["level"]+"   ·   "+((int)policy["xp"]).ToString("N0")+" / "+SeasonXpCapacity.ToString("N0")+" XP";Progress=(int)policy["xp"]/(float)SeasonXpCapacity;
                Entitlement=(bool)policy["premium"]?"프리미엄 보유":"프리미엄 미보유";
                foreach(JObject row in policy["rewardRows"]){rows.Add(new PassRewardDisplay(owner,PolicyId,row,false));rows.Add(new PassRewardDisplay(owner,PolicyId,row,true));}
                var system=data.Progress?.SystemState("PASS50");
                if(system!=null&&(string)system["policyId"]==PolicyId)foreach(JObject row in system["objectives"])objectives.Add(((string)row["frequency"]=="DAILY"?"일일 · ":"주간 · ")+RecurringContract.Objective((string)row["objectiveId"])+" · "+(long)row["xp"]+" XP\n"+ObjectiveProgress.ProgressText(row)+"\n"+ObjectiveProgress.StatusText(row));
                else objectives.Add("이 시즌의 진행 정보를 불러올 수 없습니다.");
                var settlement=(string)policy["settlementStatus"];if(settlement!="NOT_ENDED")Notice=settlement=="SOURCE_SYNC_PENDING"?"지난 시즌 진행 반영 대기":settlement=="SETTLEMENT_PENDING"?"지난 시즌 보상 정산 중":"기록된 진행의 정산 완료";
            }
            else{Season="시즌 확인 필요";ProgressText="진행 확인 필요";Entitlement="보유 상태 확인 필요";}
            Rewards=rows.AsReadOnly();Objectives=objectives.AsReadOnly();
            Notice=(Notice??"")+(!string.IsNullOrEmpty(owner?.RecurringNotice)?"\n"+owner.RecurringNotice:"")+(!string.IsNullOrEmpty(owner?.ActionNotice)?"\n"+owner.ActionNotice:"");
            if(policy==null&&string.IsNullOrWhiteSpace(Notice))Notice=data==null?"패스 정보를 불러올 수 없습니다.":"진행 중인 패스가 없습니다.";
            if(owner?.Busy==true&&data==null)Notice="진행 정보를 확인하고 있습니다.";
            bool retry=owner?.Pending?.Kind.StartsWith("recurring-",StringComparison.Ordinal)==true;
            CanRefresh=owner!=null&&!owner.Busy&&!owner.RecurringLoading&&(owner.Pending==null||retry);RefreshText=retry?"요청 결과 재확인":"진행 갱신";
            bool pendingBulk=owner?.Pending?.Kind=="recurring-bulk";BulkText=pendingBulk?"수령 결과":"전체 수령";
            CanBulk=owner!=null&&!owner.Busy&&(pendingBulk||owner.Pending==null&&policy!=null);
        }
    }
}
