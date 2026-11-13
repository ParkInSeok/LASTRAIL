using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LastRail.Presentation.Settings
{
    public enum LocalSetting { PresentationMode,MasterVolume,MusicVolume,EffectsVolume,VoiceVolume,Shake,Flash,Vibration,Quality,ReducedPresentation }
    public enum SettingsSaveStatus { Unchanged,Saved,Failed }
    public enum SettingsReadStatus { NotLoaded,Loading,Ready,Unavailable }
    public sealed class DisplaySettingsInfo
    {
        public IReadOnlyList<string> QualityNames {get;}
        public int CurrentQuality {get;}
        public string FrameText {get;}
        public bool CanReset {get;}
        public DisplaySettingsInfo(IReadOnlyList<string> names,int current,string frame,bool canReset)
        {QualityNames=new List<string>(names??throw new ArgumentNullException(nameof(names))).AsReadOnly();CurrentQuality=current;FrameText=frame;CanReset=canReset;}
    }
    public interface ILocalSettingsBackend
    {
        bool Supports(LocalSetting key);
        float Read(LocalSetting key);
        void ApplyAndSave(LocalSetting key,float value);
        Task<DisplaySettingsInfo> ReadDisplayAsync();
    }
    // Owns the local settings snapshot and valid commands; the backend owns device/storage effects.
    public sealed class LocalSettingsModel:IDisposable
    {
        readonly ILocalSettingsBackend backend;
        readonly Dictionary<LocalSetting,float> values=new Dictionary<LocalSetting,float>();
        bool disposed;int displayRequest;
        public event Action Changed;
        public SettingsSaveStatus SaveStatus {get;private set;}
        public SettingsReadStatus DisplayStatus {get;private set;}
        public DisplaySettingsInfo Display {get;private set;}
        public Exception LastFailure {get;private set;}
        public LocalSettingsModel(ILocalSettingsBackend backend)
        {
            this.backend=backend??throw new ArgumentNullException(nameof(backend));
            foreach(LocalSetting key in Enum.GetValues(typeof(LocalSetting)))if(backend.Supports(key))values[key]=backend.Read(key);
        }
        public float Value(LocalSetting key)=>values.TryGetValue(key,out var value)?value:0;
        public bool Supports(LocalSetting key)=>!disposed&&backend.Supports(key)&&(key!=LocalSetting.Quality||DisplayStatus==SettingsReadStatus.Ready&&Display.QualityNames.Count>0);
        bool Valid(LocalSetting key,float value)
        {
            if(float.IsNaN(value)||float.IsInfinity(value))return false;
            if(key==LocalSetting.PresentationMode)return value>=0&&value<=2&&value==(int)value;
            if(key==LocalSetting.ReducedPresentation)return value==0||value==1;
            if(key==LocalSetting.Quality)return Display!=null&&value>=0&&value<Display.QualityNames.Count&&value==(int)value;
            return value>=0&&value<=1;
        }
        public bool Change(LocalSetting key,float value)
        {
            if(!Supports(key)||!Valid(key,value))return false;
            if(Value(key)==value)return true;
            try{backend.ApplyAndSave(key,value);values[key]=backend.Read(key);SaveStatus=SettingsSaveStatus.Saved;}
            catch(Exception error)
            {
                LastFailure=error;
                SaveStatus=SettingsSaveStatus.Failed;
                // Preserve the last observed value if even the follow-up read is unavailable.
                try{values[key]=backend.Read(key);}catch(Exception readError){LastFailure=new AggregateException("Settings save and follow-up read failed",error,readError);}
                Changed?.Invoke();return false;
            }
            Changed?.Invoke();return true;
        }
        public async Task RefreshDisplayAsync()
        {
            if(disposed)return;int request=++displayRequest;Display=null;DisplayStatus=SettingsReadStatus.Loading;Changed?.Invoke();
            try
            {
                var result=await backend.ReadDisplayAsync();if(disposed||request!=displayRequest)return;
                if(result==null||result.QualityNames.Count==0||result.CurrentQuality<0||result.CurrentQuality>=result.QualityNames.Count)throw new InvalidOperationException("Display settings unavailable");
                Display=result;values[LocalSetting.Quality]=result.CurrentQuality;DisplayStatus=SettingsReadStatus.Ready;
            }
            catch(Exception error){if(disposed||request!=displayRequest)return;LastFailure=error;Display=null;DisplayStatus=SettingsReadStatus.Unavailable;}
            Changed?.Invoke();
        }
        public void Dispose(){disposed=true;displayRequest++;Display=null;Changed=null;}
    }
}
