using System;
using System.Collections.Generic;
namespace LastRail.Resources
{
    // Small immutable selection, pinned at preparation time. No source media copies or latest-file lookup.
    public sealed class UltimateAvSelection<T>:IDisposable where T:UnityEngine.Object
    {
        readonly List<CombatAssetLease<T>> leases;
        readonly List<Action<Exception>> failures;
        readonly Action<Exception> report;
        readonly List<T> assets;
        IDisposable playback;bool starting,started,disposed,cleaned;
        public string CombinationId{get;} public string TimelineRevision{get;} public string CueRevision{get;}
        public bool Reduced{get;} public IReadOnlyList<string> References{get;}
        internal UltimateAvSelection(string combination,string timeline,string cue,bool reduced,IReadOnlyList<string> references,
            IReadOnlyList<CombatAssetLease<T>> leases,IReadOnlyList<Action<Exception>> failures,Action<Exception> report)
        {
            CombinationId=combination;TimelineRevision=timeline;CueRevision=cue;Reduced=reduced;
            References=new List<string>(references).AsReadOnly();this.leases=new List<CombatAssetLease<T>>(leases);this.failures=new List<Action<Exception>>(failures);this.report=report;
            assets=new List<T>();foreach(var lease in leases)assets.Add(lease.Asset);
        }
        public IDisposable Play(Func<IReadOnlyList<T>,IDisposable> begin)
        {
            if(begin==null)throw new ArgumentNullException(nameof(begin));
            if(disposed||started)throw new InvalidOperationException("AV selection can be played once.");
            started=true;starting=true;
            try{playback=begin(assets.AsReadOnly());if(playback==null)throw new InvalidOperationException("AV playback lifetime missing.");return this;}
            catch(Exception error){Fail(error);throw;}
            finally{starting=false;Cleanup();}
        }
        // Bind the actual decoder/cue failure here and to the owning session's failure callback.
        public void Fail(Exception error){if(disposed){report(error);return;}try{foreach(var failure in failures)failure(error);}finally{Dispose();}}
        public void Dispose(){disposed=true;Cleanup();}
        void Cleanup()
        {
            if(!disposed||starting||cleaned)return;cleaned=true;
            var old=playback;playback=null;Release(old);
            foreach(var lease in leases)Release(lease);
            assets.Clear();leases.Clear();failures.Clear();
        }
        void Release(IDisposable value){try{value?.Dispose();}catch(Exception error){report(error);}}
    }
}
