using System;
namespace LastRail.Presentation.Audio
{
    // Composed by the existing audio player. Base selection and temporary themes share one owner.
    public sealed class BattleMusicPlayback : IDisposable
    {
        sealed class Lease:IDisposable {Action action;public Lease(Action action){this.action=action;}public void Dispose(){var old=action;action=null;old?.Invoke();}}
        readonly Func<string,IDisposable> beginBase,beginTheme;readonly Action<bool> suppressBase;
        IDisposable current,theme;string cue;long revision=-1,themeGeneration;bool disposed;
        public string CurrentCue=>cue;
        public BattleMusicPlayback(Func<string,IDisposable> beginBase,Func<string,IDisposable> beginTheme,Action<bool> suppressBase)
        {this.beginBase=beginBase??throw new ArgumentNullException(nameof(beginBase));this.beginTheme=beginTheme??throw new ArgumentNullException(nameof(beginTheme));this.suppressBase=suppressBase??throw new ArgumentNullException(nameof(suppressBase));}
        public bool Select(long originalRevision,string nextCue,bool terminal)
        {
            if(originalRevision<0)throw new ArgumentOutOfRangeException(nameof(originalRevision));
            if(disposed||originalRevision<=revision)return false;
            revision=originalRevision;
            if(current!=null&&cue==nextCue)return true;
            IDisposable next=null;
            try{if(!string.IsNullOrWhiteSpace(nextCue))next=beginBase(nextCue);}
            catch{if(terminal){var failed=current;current=null;cue=null;failed?.Dispose();}throw;}
            if(disposed||revision!=originalRevision){next?.Dispose();return false;}
            if(next==null&&!terminal)return false;
            var old=current;current=next;cue=next==null?null:nextCue;old?.Dispose();return next!=null;
        }
        public IDisposable BeginTheme(bool standard,string readyTheme)
        {
            if(disposed||!standard||string.IsNullOrWhiteSpace(readyTheme))return null;
            long stamp=++themeGeneration;
            var old=theme;theme=null;old?.Dispose();
            // Keep the existing suppression across consecutive films; missing theme restores it.
            IDisposable next=null;
            try
            {
                next=beginTheme(readyTheme);
                if(disposed||stamp!=themeGeneration){next?.Dispose();return null;}
                theme=next;suppressBase(next!=null);
                return next==null?null:new Lease(()=>EndTheme(stamp));
            }
            catch{next?.Dispose();if(!disposed&&stamp==themeGeneration)suppressBase(false);throw;}
        }
        void EndTheme(long stamp)
        {
            if(disposed||stamp!=themeGeneration)return;
            ++themeGeneration;var old=theme;theme=null;
            try{old?.Dispose();}finally{suppressBase(false);}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;themeGeneration++;
            var a=theme;var b=current;theme=current=null;cue=null;
            try{a?.Dispose();}finally{try{b?.Dispose();}finally{suppressBase(false);}}
        }
    }
}
