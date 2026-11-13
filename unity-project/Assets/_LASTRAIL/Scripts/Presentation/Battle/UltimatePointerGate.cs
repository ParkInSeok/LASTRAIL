using System;
using System.Collections.Generic;
namespace LastRail.Presentation.Battle
{
    // Existing gameplay input owner routes down/up here, and cancels its held commands on acquire.
    // Suppressed pointers stay suppressed until UP; releasing the hold does not resurrect them.
    public sealed class UltimatePointerGate : IDisposable
    {
        sealed class Lease:IDisposable {Action release;public Lease(Action action){release=action;}public void Dispose(){var action=release;release=null;action?.Invoke();}}
        readonly Dictionary<int,bool> pointers=new Dictionary<int,bool>();int holds;bool disposed;
        public bool Down(int pointer){if(disposed||pointers.ContainsKey(pointer))return false;bool allowed=holds==0;pointers.Add(pointer,allowed);return allowed;}
        public bool Up(int pointer){if(!pointers.TryGetValue(pointer,out bool allowed))return false;pointers.Remove(pointer);return !disposed&&holds==0&&allowed;}
        public void Cancel(int pointer)=>pointers.Remove(pointer);
        public IDisposable Acquire(){if(disposed)throw new ObjectDisposedException(nameof(UltimatePointerGate));holds++;foreach(int pointer in new List<int>(pointers.Keys))pointers[pointer]=false;return new Lease(()=>{if(!disposed)holds--;});}
        public void Dispose(){disposed=true;holds=0;pointers.Clear();}
    }
}
