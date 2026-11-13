using System;
using System.Collections.Generic;
namespace LastRail.Battle {
 // Owned by the command loop; replay derives this state from PAUSE/HOLD commands.
 // User pause is independent of each subsystem's named hold. Speed belongs to clock.
 internal sealed class BCombatPauseReasons {
  readonly HashSet<string> holds=new HashSet<string>(StringComparer.Ordinal);
  bool userPaused;
  internal bool Paused=>userPaused||holds.Count!=0;
  internal void SetUser(bool value)=>userPaused=value;
  internal void SetHold(string owner,bool held){
   if(string.IsNullOrWhiteSpace(owner))throw new ArgumentException("Hold owner required");
   if(held)holds.Add(owner);else holds.Remove(owner);
  }
  internal BCombatPauseReasons Clone(){var copy=new BCombatPauseReasons{userPaused=userPaused};foreach(var h in holds)copy.holds.Add(h);return copy;}
 }
}
