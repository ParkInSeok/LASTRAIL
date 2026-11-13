using System;
using System.Threading.Tasks;
using LastRail.Battle;
using UnityEngine;

namespace LastRail.Presentation.Settings
{
    public sealed class UnitySettingsBackend:ILocalSettingsBackend
    {
        public const string ModeKey="LastRail.Presentation.Mode";
        public const string MasterKey="LastRail.Settings.MasterVolume";
        public const string MusicKey="LastRail.Settings.MusicVolume",EffectsKey="LastRail.Settings.EffectsVolume",ReducedKey="LastRail.Settings.ReducedPresentation";
        const string QualityKey="LastRail.Settings.Quality";
        const string QualityNameKey="LastRail.Settings.QualityName";
        readonly Transform applicationRoot;
        public UnitySettingsBackend(Transform applicationRoot)
        {
            this.applicationRoot=applicationRoot;
            // The approved fresh-install default never overwrites an existing mode.
            if(Application.isPlaying&&!PlayerPrefs.HasKey(ModeKey)){PlayerPrefs.SetInt(ModeKey,2);PlayerPrefs.Save();}
            if(Application.isPlaying)ApplySavedSettings();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplySavedSettings()
        {
            if(PlayerPrefs.HasKey(MasterKey))AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey,1));
            if(PlayerPrefs.HasKey(QualityKey)&&PlayerPrefs.HasKey(QualityNameKey))
            {int level=Array.IndexOf(QualitySettings.names,PlayerPrefs.GetString(QualityNameKey));if(level>=0)QualitySettings.SetQualityLevel(level,true);}
        }
        public bool Supports(LocalSetting key)=>key==LocalSetting.PresentationMode||key==LocalSetting.MasterVolume||key==LocalSetting.MusicVolume||key==LocalSetting.EffectsVolume||key==LocalSetting.ReducedPresentation||key==LocalSetting.Quality&&QualitySettings.names.Length>0;
        public static float SavedMusic=>Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey,1));
        public static float SavedEffects=>Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey,1));
        public float Read(LocalSetting key)
        {
            switch(key)
            {
                case LocalSetting.PresentationMode:return Mathf.Clamp(PlayerPrefs.GetInt(ModeKey,2),0,2);
                case LocalSetting.MasterVolume:return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey,AudioListener.volume));
                case LocalSetting.MusicVolume:return SavedMusic;
                case LocalSetting.EffectsVolume:return SavedEffects;
                case LocalSetting.ReducedPresentation:return PlayerPrefs.GetInt(ReducedKey,0)==1?1:0;
                case LocalSetting.Quality:return QualitySettings.GetQualityLevel();
                default:throw new InvalidOperationException("Setting is not connected");
            }
        }
        public void ApplyAndSave(LocalSetting key,float value)
        {
            if(!Supports(key))throw new InvalidOperationException("Setting is not connected");
            float previous=Read(key);bool hadKey=PlayerPrefs.HasKey(Key(key));
            try{Apply(key,value);}
            catch(Exception failure)
            {
                try{Apply(key,previous);if(!hadKey){PlayerPrefs.DeleteKey(Key(key));if(key==LocalSetting.Quality)PlayerPrefs.DeleteKey(QualityNameKey);PlayerPrefs.Save();}}
                catch(Exception rollback){throw new AggregateException("Setting apply and rollback failed",failure,rollback);}
                throw;
            }
        }
        void Apply(LocalSetting key,float value)
        {
            if(key==LocalSetting.ReducedPresentation){PlayerPrefs.SetInt(ReducedKey,(int)value);PlayerPrefs.Save();return;}
            if(key==LocalSetting.MusicVolume||key==LocalSetting.EffectsVolume)
            {
                PlayerPrefs.SetFloat(Key(key),value);PlayerPrefs.Save();
                if(applicationRoot!=null)foreach(var player in applicationRoot.GetComponentsInChildren<LastRail.Presentation.Audio.LastRailAudioCuePlayer>(true))
                    player.ApplyMixSettings(SavedMusic,SavedEffects);
                return;
            }
            if(key==LocalSetting.PresentationMode)
            {
                var active=applicationRoot==null?null:applicationRoot.GetComponentInChildren<ApprovedCombatView>();
                if(active!=null){active.SetPresentationMode((int)value);return;}
                PlayerPrefs.SetInt(ModeKey,(int)value);PlayerPrefs.Save();return;
            }
            if(key==LocalSetting.MasterVolume){PlayerPrefs.SetFloat(MasterKey,value);PlayerPrefs.Save();AudioListener.volume=value;return;}
            PlayerPrefs.SetInt(QualityKey,(int)value);PlayerPrefs.SetString(QualityNameKey,QualitySettings.names[(int)value]);PlayerPrefs.Save();QualitySettings.SetQualityLevel((int)value,true);
        }
        static string Key(LocalSetting key)=>key==LocalSetting.PresentationMode?ModeKey:key==LocalSetting.MasterVolume?MasterKey:key==LocalSetting.MusicVolume?MusicKey:key==LocalSetting.EffectsVolume?EffectsKey:key==LocalSetting.ReducedPresentation?ReducedKey:QualityKey;
        public async Task<DisplaySettingsInfo> ReadDisplayAsync()
        {
            await Task.Yield();
            return new DisplaySettingsInfo(QualitySettings.names,QualitySettings.GetQualityLevel(),Application.targetFrameRate<0?"자동 · 현재 실행 설정":Application.targetFrameRate+" · 현재 실행 설정",false);
        }
    }
}
