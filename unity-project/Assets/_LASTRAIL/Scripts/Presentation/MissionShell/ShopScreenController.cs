using System;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed class ShopScreenSelection
    {
        public string Offer {get;private set;}="NORMAL";
        public int Count {get;private set;}=1;
        public int ProductPage {get;private set;}
        internal void SelectOffer(string offer){if(offer!="NORMAL"&&offer!="DAILY")return;Offer=offer;Count=1;}
        internal void ChangeCount(int delta,int maximum){Count=(int)Math.Max(1,Math.Min((long)maximum,(long)Count+delta));}
        internal void ClampProductPage(int pages){ProductPage=Math.Min(ProductPage,pages-1);}
        internal void NextProductPage(int pages){ProductPage=(ProductPage+1)%pages;}
    }

    public sealed class ShopScreenState
    {
        public bool Ready {get;} public bool NormalSelected {get;}
        public bool CanDecrease {get;} public bool CanIncrease {get;} public bool CanBuy {get;}
        public bool CanRefresh {get;} public bool ShowRetry {get;} public bool CanRetry {get;}
        public string Unavailable {get;} public string Balance {get;} public string UnitPrice {get;}
        public string Count {get;} public string Spend {get;} public string BuyLabel {get;} public string Reset {get;}
        public string Notice {get;} public string ProductPage {get;} public bool ShowProductPage {get;} public bool ProductsEmpty {get;}
        public System.Collections.Generic.IReadOnlyList<long> ProductQuantities {get;}
        public AdSkipSnapshot AdSkip {get;} public bool AdSkipLoading {get;} public string AdSkipNotice {get;} public bool AdSkipSalesAllowed {get;}
        internal ShopScreenState(AccountScreenRuntime account,ShopScreenSelection selection,ShopOfferQuote quote)
        {
            Ready=quote!=null;NormalSelected=selection.Offer=="NORMAL";
            Unavailable=account?.ShopLoading==true?"상품 정보를 확인하고 있습니다.":account?.ShopNotice??"교환 상품을 불러오지 못했습니다.";
            bool idle=account!=null&&account.Pending==null&&!account.Busy&&!account.ShopLoading;
            CanDecrease=Ready&&idle&&selection.Count>1;CanIncrease=Ready&&idle&&selection.Count<quote.Maximum;
            CanBuy=account?.CanBuyShopOffer(selection.Offer,selection.Count)==true;
            CanRefresh=account!=null&&!account.Busy&&!account.ShopLoading;CanRetry=account!=null&&!account.Busy;
            ShowRetry=account?.Pending?.Kind=="shop-offer"||account?.Pending?.Kind=="ticket-exchange";Notice=account?.ActionNotice??"";
            if(Ready){var wallet=account.Shop.Wallet;Balance="보유 크리스탈 "+wallet.TotalBalance+" · 무료 "+wallet.FreeBalance+" / 유료 "+wallet.PaidBalance;
                UnitPrice="1장당 "+quote.UnitCost+" 크리스탈"+(NormalSelected?" · 요청당 최대 "+quote.Maximum+"장":" · 오늘 남은 횟수 "+quote.DailyRemaining);
                Count=quote.Count+"장";Spend="합계 "+quote.Cost+" 크리스탈\n무료 "+quote.FreeSpend+" · 유료 "+quote.PaidSpend+" 사용 예정";
                BuyLabel=!NormalSelected&&quote.DailyRemaining==0?"오늘 교환 완료":"교환";
                Reset="일일 할인 갱신 "+DateTimeOffset.FromUnixTimeMilliseconds(account.Shop.NextReset).ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm");}
            var products=account?.Shop?.Products;int productCount=products?.Count??0,pages=Math.Max(1,(productCount+2)/3);
            ProductPage=(selection.ProductPage+1)+" / "+pages;ShowProductPage=productCount>3;ProductsEmpty=productCount==0;
            ProductQuantities=Array.AsReadOnly(products==null?Array.Empty<long>():products.Skip(selection.ProductPage*3).Take(3).Select(p=>(long)p["quantity"]).ToArray());
            AdSkip=account?.AdSkip?.Snapshot;AdSkipLoading=account?.AdSkip?.Loading==true;AdSkipNotice=account?.AdSkip?.Notice;AdSkipSalesAllowed=account?.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled;
        }
    }

    // Borrows account state; disposal removes this screen's subscription and render callback only.
    public sealed class ShopScreenController:IDisposable
    {
        readonly AccountScreenRuntime account;readonly ShopScreenSelection selection;readonly Func<bool> session;
        Action<ShopScreenState> render;bool disposed;
        public ShopScreenController(AccountScreenRuntime account,ShopScreenSelection selection){this.account=account;this.selection=selection??throw new ArgumentNullException(nameof(selection));session=account?.CaptureScreenSession()??(()=>true);if(account!=null)account.Changed+=Render;}
        bool Current=>!disposed&&session();
        public void Bind(Action<ShopScreenState> render){if(!Current)return;this.render=render;Render();}
        public void SelectOffer(string offer){if(!Current)return;selection.SelectOffer(offer);Render();}
        public void ChangeCount(int delta){if(!Current)return;var quote=account?.Shop?.Quote(selection.Offer,selection.Count);if(quote==null||account.Pending!=null||account.Busy||account.ShopLoading)return;selection.ChangeCount(delta,quote.Maximum);Render();}
        public void NextProductPage(){if(!Current)return;selection.NextProductPage(Math.Max(1,((account?.Shop?.Products.Count??0)+2)/3));Render();}
        public Task BuyAsync()=>!Current||account==null?Task.CompletedTask:account.BuyShopOfferAsync(selection.Offer,selection.Count);
        public Task RetryAsync()=>!Current||account==null?Task.CompletedTask:account.RetryPendingAsync();
        public Task RefreshAsync()=>!Current||account==null?Task.CompletedTask:Task.WhenAll(account.RefreshSharedAsync(),account.RefreshShopAsync(),account.RefreshAdSkipAsync());
        public Task RefreshAdSkipAsync()=>!Current||account==null?Task.CompletedTask:account.RefreshAdSkipAsync();
        public void Render(){if(!Current||render==null)return;var quote=account?.Shop?.Quote(selection.Offer,selection.Count);if(quote!=null){selection.ChangeCount(0,quote.Maximum);quote=account.Shop.Quote(selection.Offer,selection.Count);}selection.ClampProductPage(Math.Max(1,((account?.Shop?.Products.Count??0)+2)/3));render(new ShopScreenState(account,selection,quote));}
        public void Dispose(){if(disposed)return;disposed=true;if(account!=null)account.Changed-=Render;render=null;}
    }
}
