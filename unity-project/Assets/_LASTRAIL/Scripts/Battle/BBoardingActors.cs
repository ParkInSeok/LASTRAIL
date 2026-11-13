using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LastRail.Battle {
 // Borrow the executing node's clock and health; boarding never owns another HP ledger.
 internal interface IBBoardingActors {
  long Tick {get;}
  int Hp(string id);
  long ProjectileTargetGeneration(string id);
 }
 internal sealed class BBoardingRoster {
  internal sealed class Entity {
   internal string Id {get;} internal string Kind {get;} internal string Definition {get;} internal int Slot {get;}
   internal Entity(string id,string kind,string definition,int slot){Id=id;Kind=kind;Definition=definition;Slot=slot;}
  }
  internal IReadOnlyDictionary<string,Entity> Entities {get;}
  internal IReadOnlyList<string> CarriageIds {get;}
  internal long StepMicroseconds {get;}
  internal BBoardingRoster(IEnumerable<Entity> entities,IEnumerable<string> carriages,long step){
   if(step<=0)throw new ArgumentOutOfRangeException(nameof(step));
   var cars=carriages.ToArray();if(cars.Length==0||cars.Distinct().Count()!=cars.Length)throw new ArgumentException("Distinct physical boarding hosts required.");
   var rows=entities.ToArray();if(rows.Any(e=>e==null||string.IsNullOrWhiteSpace(e.Id)||string.IsNullOrWhiteSpace(e.Definition)||!new[]{"OPERATOR","LOCAL_ENEMY","CARRIAGE","TRAIN"}.Contains(e.Kind)||e.Slot<-1||e.Slot>=cars.Length||e.Kind=="OPERATOR"&&e.Slot<0))throw new ArgumentException("Original boarding roster required.");
   Entities=new ReadOnlyDictionary<string,Entity>(rows.ToDictionary(e=>e.Id,StringComparer.Ordinal));CarriageIds=Array.AsReadOnly(cars);StepMicroseconds=step;
  }
  internal static BBoardingRoster FromNative(BDirectAttackInput input)=>new BBoardingRoster(input.Entities.Values.Select(e=>new Entity(e.Id,e.Kind,e.Definition,e.Slot)),input.Formation.Slots.Select(s=>s.CarriageId),input.StepTimeSpanTicks/10);
 }
}
