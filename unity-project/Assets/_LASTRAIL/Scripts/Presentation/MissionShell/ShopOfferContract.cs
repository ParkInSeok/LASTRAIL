using System;
using System.Linq;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class ShopSnapshot {
  readonly JObject data;
  public string Policy=>(string)data["policyVersion"];
  public long DayStart=>(long)data["dayStart"];
  public long NextReset=>(long)data["nextResetAt"];
  public CrystalWalletRead Wallet {get;}
  public JArray Offers=>(JArray)data["ticketOffers"].DeepClone();
  public JArray Products=>(JArray)data["crystalProducts"].DeepClone();
  public string UnavailableReason=>(string)data["ticketUnavailableReason"];
  public ShopSnapshot(string json){data=GameApiJson.ReadObject(json);RecurringContract.Accept(data);if(GameApiJson.String(data,"version")!="shop-v1")throw new FormatException();MonthlyAttendanceContract.Id(data,"policyVersion");var now=DateTimeOffset.FromUnixTimeMilliseconds(AccountStateSnapshot.Number(data,"serverTime"));var service=now.AddHours(5);var day=new DateTimeOffset(service.Year,service.Month,service.Day,0,0,0,TimeSpan.Zero).AddHours(-5);if(AccountStateSnapshot.Number(data,"dayStart")!=day.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(data,"nextResetAt")!=day.AddDays(1).ToUnixTimeMilliseconds())throw new FormatException();
   Wallet=CrystalWalletRead.Parse(200,GameApiJson.Object(data,"wallet").ToString());if(Wallet.Status!=CrystalWalletReadStatus.Ready||Wallet.PolicyVersion!=Policy)throw new FormatException();var offers=RecruitmentJson.Array(data,"ticketOffers");RecurringContract.Unique(offers,"offer");var reason=RecruitmentJson.NullableString(data,"ticketUnavailableReason");if(offers.Count!=0&&offers.Count!=2||offers.Count==0&&reason!="shop_launch_price_unconfigured"||offers.Count==2&&reason!=null)throw new FormatException();
   foreach(JObject o in offers){var daily=RecurringContract.One(o,"offer","NORMAL","DAILY")=="DAILY";if(GameApiJson.String(o,"ticketItemId")!="cur_command_ticket"||AccountStateSnapshot.Number(o,"crystalsPerTicket")!=(daily?50:100))throw new FormatException();var max=MonthlyAttendanceContract.Range(o,"maxTicketsPerRequest",1,int.MaxValue);if(daily){if(max!=1||AccountStateSnapshot.Number(o,"dailyLimit")!=1||GameApiJson.String(o,"offerFamily")!="COMMAND_TICKET_DAILY")throw new FormatException();MonthlyAttendanceContract.Range(o,"dailyRemaining",0,1);}else if(o["dailyLimit"]?.Type!=JTokenType.Null)throw new FormatException();}
   var caps=GameApiJson.Object(data,"capabilities");RecurringContract.One(caps,"materialShop","UNCONFIGURED");RecurringContract.One(caps,"cashPriceSource","STORE_REQUIRED");GameApiJson.Boolean(caps,"crystalPurchaseVerifierAvailable");if(GameApiJson.String(caps,"passCatalog")!="/v1/recurring/state")throw new FormatException();RecurringContract.One(caps,"creditRecruitmentExchange","NOT_SUPPORTED");
   foreach(JObject p in RecruitmentJson.Array(data,"crystalProducts")){GameApiJson.String(p,"productId");MonthlyAttendanceContract.Range(p,"quantity",1,int.MaxValue);RecurringContract.One(p,"provider","GOOGLE_PLAY","APP_STORE");GameApiJson.String(p,"appId");GameApiJson.Boolean(p,"purchaseAvailable");if(p["price"]?.Type!=JTokenType.Null||GameApiJson.String(p,"priceSource")!="STORE_REQUIRED")throw new FormatException();}
  }
 }
 public sealed class ShopOfferReceipt {
  public TicketExchangeReceipt Exchange {get;}
  public string Offer {get;}
  public long DayStart {get;}
  public ShopOfferReceipt(string json,string payload){var b=Request(payload);var r=GameApiJson.ReadObject(json);Offer=RecurringContract.One(r,"offer","NORMAL","DAILY");DayStart=AccountStateSnapshot.Number(r,"dayStart");var cost=checked((long)b["ticketCount"]*((string)b["offer"]=="DAILY"?50:100));if(Offer!=(string)b["offer"]||DayStart!=(long)b["expectedDayStart"]||AccountStateSnapshot.Number(r,"cost")!=cost)throw new FormatException();if(Offer=="DAILY"?AccountStateSnapshot.Number(r,"dailyRemaining")!=0:r["dailyRemaining"]?.Type!=JTokenType.Null)throw new FormatException();Exchange=new TicketExchangeReceipt(json,new JObject{["policyVersion"]=(string)b["policyVersion"],["ticketCount"]=(long)b["ticketCount"]}.ToString());if(checked(Exchange.FreeSpent+Exchange.PaidSpent)!=cost)throw new FormatException();}
  public static JObject Request(string payload){if(payload==null||payload.Length>1000)throw new FormatException();var b=GameApiJson.ReadObject(payload);if(b.Count!=4)throw new FormatException();MonthlyAttendanceContract.Id(b,"policyVersion");var offer=RecurringContract.One(b,"offer","NORMAL","DAILY");var count=MonthlyAttendanceContract.Range(b,"ticketCount",1,int.MaxValue);if(offer=="DAILY"&&count!=1)throw new FormatException();var day=AccountStateSnapshot.Number(b,"expectedDayStart");if(day%86400000!=68400000)throw new FormatException();return b;}
 }
}
