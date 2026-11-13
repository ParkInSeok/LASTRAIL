using System;
namespace LastRail.Presentation.Battle
{
    // Bound to one player's same-timeline validated profile switch and the session's existing Cancel.
    public sealed class UltimatePresentationIntensity : IDisposable
    {
        readonly Func<bool> reduceInPlace; readonly Action safeEnd; bool reduced,disposed;
        public UltimatePresentationIntensity(Func<bool> reduceInPlace,Action safeEnd)
        {this.reduceInPlace=reduceInPlace??throw new ArgumentNullException(nameof(reduceInPlace));this.safeEnd=safeEnd??throw new ArgumentNullException(nameof(safeEnd));}
        public void Select(bool reduce)
        {
            if(disposed||reduced||!reduce)return;
            reduced=true;
            bool applied=false;
            try{applied=reduceInPlace();}finally{if(!applied&&!disposed)safeEnd();}
        }
        public void Dispose(){disposed=true;}
    }
}
