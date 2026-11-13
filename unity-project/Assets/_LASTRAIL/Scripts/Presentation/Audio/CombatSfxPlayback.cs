using System;
using System.Collections.Generic;
namespace LastRail.Presentation.Audio
{
    // One ordered original-event scope. Capacity is the supplied owned voice count, not a tuning default.
    // No pending sound queue. Dispose before replacing account/run/node or returning the owner to a pool.
    public sealed class CombatSfxPlayback : IDisposable
    {
        sealed class Voice { public int Priority, Urgency; public long Order; public IDisposable Lease; public bool Ended; }
        readonly int capacity;
        readonly Func<string, Action, IDisposable> begin;
        readonly List<Voice> voices = new List<Voice>();
        readonly HashSet<Tuple<string,string,string,string,string>> groups = new HashSet<Tuple<string,string,string,string,string>>();
        readonly HashSet<string> effects = new HashSet<string>(StringComparer.Ordinal);
        Tuple<string,string,string,long> currentEvent;
        long cursor = -1, groupTime = -1; bool disposed;
        public Exception LastFailure { get; private set; }
        public int ActiveCount => voices.Count;
        public CombatSfxPlayback(int capacity, Func<string, Action, IDisposable> begin)
        { if(capacity<0)throw new ArgumentOutOfRangeException(nameof(capacity)); this.capacity=capacity;this.begin=begin??throw new ArgumentNullException(nameof(begin)); }
        public string Offer(long originalOrder,string actionId,string phase,string pulseId,long originalTime,string family,string cue,int priority,int urgency)
            =>OfferEffect(originalOrder,actionId,phase,pulseId,originalTime,family,cue,priority,urgency,"default",null);
        public string OfferEffect(long originalOrder,string actionId,string phase,string pulseId,long originalTime,string family,string cue,int priority,int urgency,string effectKey,Func<string,Action,IDisposable> effectBegin)
        {
            if(disposed)return "disposed";
            if(originalOrder<0||originalTime<0||priority<1||priority>4||urgency<0||string.IsNullOrWhiteSpace(actionId)||string.IsNullOrWhiteSpace(phase)||string.IsNullOrWhiteSpace(pulseId)||string.IsNullOrWhiteSpace(family)||string.IsNullOrWhiteSpace(cue))throw new ArgumentException("Original event order/action/phase/pulse/time/family and authored importance required.");
            if(string.IsNullOrWhiteSpace(effectKey))throw new ArgumentException("Original subeffect identity required.");
            if(originalOrder<cursor)return "old_or_duplicate";
            var identity=Tuple.Create(actionId,phase,pulseId,originalTime);
            if(originalOrder==cursor&&!identity.Equals(currentEvent))throw new ArgumentException("Conflicting original event identity.");
            if(originalTime<groupTime)throw new ArgumentException("Original event time moved backwards within one scope.");
            if(originalOrder!=cursor){effects.Clear();currentEvent=identity;}
            cursor=originalOrder;
            if(!effects.Add(effectKey))return "old_or_duplicate";
            if(originalTime!=groupTime){groups.Clear();groupTime=originalTime;}
            if(priority>=3&&!groups.Add(Tuple.Create(actionId,phase,pulseId,family,effectKey)))return "same_original_group";
            if(capacity==0)return "capacity_unavailable";
            if(voices.Count>=capacity)
            {
                Voice victim=null;
                foreach(var voice in voices)
                    if((voice.Priority>priority||voice.Priority==priority&&voice.Urgency<urgency)&&
                       (victim==null||voice.Priority>victim.Priority||voice.Priority==victim.Priority&&(voice.Urgency<victim.Urgency||voice.Urgency==victim.Urgency&&voice.Order<victim.Order)))victim=voice;
                if(victim==null)return "capacity_priority";
                End(victim);
            }
            // Reentrant disposal/termination must not let a late returned lease escape.
            if(disposed||voices.Count>=capacity)return "scope_changed";
            var next=new Voice{Priority=priority,Urgency=urgency,Order=originalOrder};voices.Add(next);
            try
            {
                var lease=(effectBegin??begin)(cue,()=>End(next));
                if(lease==null){End(next);return "missing_playback";}
                if(next.Ended)Release(lease);else next.Lease=lease;
                return "played";
            }
            catch(Exception error){LastFailure=error;End(next);return "playback_failed";}
        }
        void End(Voice voice){if(voice.Ended)return;voice.Ended=true;voices.Remove(voice);var lease=voice.Lease;voice.Lease=null;Release(lease);}
        void Release(IDisposable lease){try{lease?.Dispose();}catch(Exception error){LastFailure=error;}}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var voice in voices.ToArray())End(voice);groups.Clear();}
    }
}
