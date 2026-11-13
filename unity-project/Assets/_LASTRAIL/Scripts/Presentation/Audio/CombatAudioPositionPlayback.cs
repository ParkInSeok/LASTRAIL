using System;
using UnityEngine;
namespace LastRail.Presentation.Audio
{
    // Presentation-only lease. Refresh changes mix, never starts or repeats an event.
    public sealed class CombatAudioPositionPlayback : IDisposable
    {
        readonly CombatAudioSpatialPolicy policy;
        readonly CombatAudioPosition initial;
        readonly bool loop, nonSpatialAllowed;
        readonly int priority;
        readonly Func<CombatAudioPosition> source;
        readonly Func<bool> mono;
        readonly Action<Vector2> apply;
        Action stop;
        public Exception LastFailure { get; private set; }
        public CombatAudioPositionPlayback(CombatAudioSpatialPolicy policy,CombatAudioPosition initial,bool loop,bool nonSpatialAllowed,int priority,
            Func<CombatAudioPosition> source,Func<bool> mono,Action<Vector2> apply,Action stop)
        {
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            this.initial=initial??throw new ArgumentNullException(nameof(initial));
            if(string.IsNullOrWhiteSpace(initial.Owner)||initial.Generation<0)throw new ArgumentException("Original owner and generation required.");
            this.loop=loop;this.nonSpatialAllowed=nonSpatialAllowed;this.priority=priority;
            this.source=source??throw new ArgumentNullException(nameof(source));this.mono=mono??throw new ArgumentNullException(nameof(mono));
            this.apply=apply??throw new ArgumentNullException(nameof(apply));this.stop=stop??throw new ArgumentNullException(nameof(stop));
        }
        public void Refresh()
        {
            if(stop==null)return;
            try {
                var current=source();
                if(stop==null)return;
                if(current==null||current.Owner!=initial.Owner||current.Generation!=initial.Generation){Dispose();return;}
                var position=loop?current:initial;
                bool monoValue=mono();
                if(stop==null)return;
                var mix=policy.Evaluate(position.Point,current.Audible,current.DirectionDisclosed,nonSpatialAllowed,position.Distance,priority,monoValue);
                if(stop==null)return;
                if(!mix.HasValue){Dispose();return;}
                apply(mix.Value);
            } catch(Exception error){LastFailure=error;Dispose();}
        }
        public void Dispose(){var old=stop;stop=null;old?.Invoke();}
    }
}
