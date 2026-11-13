using System;
namespace LastRail.Presentation.Audio
{
    // Supplied by the approved asset/disclosure composition. No default cue, priority or tuning.
    public sealed class CombatAudioResultBinding
    {
        public string Outcome {get;} public string Cue {get;} public string Family {get;}
        public int Priority {get;} public int Urgency {get;}
        public bool NonSpatialAllowed {get;}
        public CombatAudioResultBinding(string outcome,string cue,string family,int priority,int urgency,bool nonSpatialAllowed)
        {
            if(!CombatAudioResults.Known(outcome)||outcome=="Miss"||string.IsNullOrWhiteSpace(cue)||string.IsNullOrWhiteSpace(family)||priority<1||priority>4||urgency<0)
                throw new ArgumentException("Explicit confirmed outcome and approved cue/family/importance required.");
            Outcome=outcome;Cue=cue;Family=family;Priority=priority;Urgency=urgency;NonSpatialAllowed=nonSpatialAllowed;
        }
    }
}
