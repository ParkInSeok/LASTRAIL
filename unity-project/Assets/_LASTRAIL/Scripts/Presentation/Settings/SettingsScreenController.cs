using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation.Settings
{
    public enum SettingsTab { Game,Sound,Display,Account }
    public sealed class SettingsScreenState
    {
        readonly Dictionary<LocalSetting,float> values=new Dictionary<LocalSetting,float>();
        readonly HashSet<LocalSetting> supported=new HashSet<LocalSetting>();
        public SettingsTab Tab {get;}
        public string SaveNotice {get;}
        public SettingsSaveStatus SaveStatus {get;}
        public SettingsReadStatus DisplayStatus {get;}
        public DisplaySettingsInfo Display {get;}
        public bool AccountReady {get;}
        public bool AccountLoading {get;}
        public bool CanLink {get;}
        public string AccountNotice {get;}
        public SettingsScreenState(SettingsTab tab,LocalSettingsModel model,AccountScreenRuntime account,bool hasLink)
        {
            Tab=tab;SaveStatus=model.SaveStatus;
            foreach(LocalSetting key in Enum.GetValues(typeof(LocalSetting))){values[key]=model.Value(key);if(model.Supports(key))supported.Add(key);}
            SaveNotice=model.SaveStatus==SettingsSaveStatus.Saved?"설정 저장됨":model.SaveStatus==SettingsSaveStatus.Failed?"설정을 저장하지 못했습니다.":"현재 설정을 표시합니다.";
            DisplayStatus=model.DisplayStatus;Display=model.Display;
            AccountReady=account?.AccountStatus==AccountStateReadStatus.Ready&&account.Account!=null;
            AccountLoading=account?.AccountStatus==AccountStateReadStatus.Loading;
            CanLink=AccountReady&&hasLink;
            AccountNotice=AccountLoading?"계정 정보를 확인하고 있습니다.":AccountReady?"계정 정보를 확인했습니다.":"계정 정보를 불러오지 못했습니다.";
        }
        public float Value(LocalSetting key)=>values[key];
        public bool Supports(LocalSetting key)=>supported.Contains(key);
    }
    public interface ISettingsScreenView
    {
        event Action<int> TabRequested;
        event Action<LocalSetting,float> ValueRequested;
        event Action BackRequested,RefreshRequested,AccountLinkRequested;
        void Render(SettingsScreenState state);
    }
    // Routes intents to the borrowed local settings/account owners. No login or device rules in View.
    public sealed class SettingsScreenController:IDisposable
    {
        readonly LocalSettingsModel model;readonly AccountScreenRuntime account;
        readonly ISettingsScreenView view;readonly Action back,link;
        SettingsTab tab;bool disposed;
        public bool DiagnosticsAvailable=>!disposed&&account?.LifecycleDiagnostics?.Enabled==true;
        public string ExportDiagnostics()=>DiagnosticsAvailable?account.LifecycleDiagnostics.Export():"";
        public SettingsScreenController(LocalSettingsModel model,AccountScreenRuntime account,ISettingsScreenView view,Action back,Action link)
        {
            this.model=model??throw new ArgumentNullException(nameof(model));this.account=account;this.view=view??throw new ArgumentNullException(nameof(view));this.back=back;this.link=link;
            model.Changed+=Render;if(account!=null)account.Changed+=Render;
            view.TabRequested+=SelectTab;view.ValueRequested+=Change;view.BackRequested+=Back;view.RefreshRequested+=Refresh;view.AccountLinkRequested+=Link;
            Render();
        }
        void SelectTab(int value)
        {
            if(disposed||value<0||value>3)return;tab=(SettingsTab)value;Render();
            if(tab==SettingsTab.Display&&model.DisplayStatus==SettingsReadStatus.NotLoaded||tab==SettingsTab.Account&&account?.AccountStatus==AccountStateReadStatus.NotLoaded)Refresh();
        }
        void Change(LocalSetting key,float value){if(!disposed)model.Change(key,value);}
        void Back(){if(!disposed)back?.Invoke();}
        void Link(){if(!disposed&&account?.AccountStatus==AccountStateReadStatus.Ready&&account.Account!=null)link?.Invoke();}
        async void Refresh(){await RefreshAsync();}
        public Task RefreshAsync()
        {
            if(disposed)return Task.CompletedTask;
            if(tab==SettingsTab.Display&&model.DisplayStatus!=SettingsReadStatus.Loading)return model.RefreshDisplayAsync();
            if(tab==SettingsTab.Account&&account!=null&&account.AccountStatus!=AccountStateReadStatus.Loading)return account.RefreshAccountAsync();
            return Task.CompletedTask;
        }
        void Render(){if(!disposed)view.Render(new SettingsScreenState(tab,model,account,link!=null));}
        public void Dispose()
        {
            if(disposed)return;disposed=true;model.Changed-=Render;if(account!=null)account.Changed-=Render;
            view.TabRequested-=SelectTab;view.ValueRequested-=Change;view.BackRequested-=Back;view.RefreshRequested-=Refresh;view.AccountLinkRequested-=Link;
        }
    }
}

