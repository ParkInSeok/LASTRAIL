using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public enum BLocalObservationDeliveryStatus { Forwarded,AlreadyForwarded,AlreadyInFlight,Superseded }
 // Owns only LOCAL callback-delivery state and its cancellation sources. Callback
 // completion does not assert server acceptance. Borrowed source/controller stays alive.
 public sealed class BRuntimeObservationDelivery:IDisposable {
  const string Version="unity.b-runtime-local-delivery-cursor.v1.20261006";
  readonly Func<BRouteRuntimeObservation> capture;
  readonly Dictionary<string,CancellationTokenSource> pending=new Dictionary<string,CancellationTokenSource>();
  readonly HashSet<string> delivered=new HashSet<string>(StringComparer.Ordinal);
  long generation;bool disposed,invalidating;
  internal long Generation=>generation;
  public BRuntimeObservationDelivery(Func<BRouteRuntimeObservation> originalCapture){capture=originalCapture??throw new ArgumentNullException(nameof(originalCapture));}
  public async Task<BLocalObservationDeliveryStatus> ForwardAsync(BRouteRuntimeObservation original,Func<BRouteRuntimeObservation,CancellationToken,Task> forward,CancellationToken cancellation=default){
   Alive();if(original==null||forward==null)throw new ArgumentNullException();original.RequireOriginalCommand();cancellation.ThrowIfCancellationRequested();
   if(capture().DeliveryKey!=original.DeliveryKey)throw new InvalidOperationException("b_runtime_delivery_original_changed");
   string key=original.DeliveryKey;if(delivered.Contains(key))return BLocalObservationDeliveryStatus.AlreadyForwarded;if(pending.ContainsKey(key))return BLocalObservationDeliveryStatus.AlreadyInFlight;
   var owned=CancellationTokenSource.CreateLinkedTokenSource(cancellation);long ticket=generation;pending.Add(key,owned);
   try{await forward(original,owned.Token);if(disposed||ticket!=generation)return BLocalObservationDeliveryStatus.Superseded;owned.Token.ThrowIfCancellationRequested();if(capture().DeliveryKey!=key)return BLocalObservationDeliveryStatus.Superseded;delivered.Add(key);return BLocalObservationDeliveryStatus.Forwarded;}
   finally{if(pending.TryGetValue(key,out var current)&&ReferenceEquals(current,owned))pending.Remove(key);owned.Dispose();}
  }
  public void InvalidateChangedSources(){Alive();if(pending.Count>0){string current=capture().DeliveryKey;if(pending.Keys.Any(key=>key!=current))Invalidate();}}
  public void Invalidate(){Alive();generation=checked(generation+1);var owned=pending.Values.ToArray();pending.Clear();invalidating=true;var errors=new List<Exception>();try{foreach(var token in owned){try{token.Cancel();}catch(Exception error){errors.Add(error);}}}finally{invalidating=false;}if(errors.Count>0)throw new AggregateException("b_runtime_delivery_cancel_failed",errors);}
  public JObject CaptureCursor(){Alive();var current=capture();return new JObject{["version"]=Version,["originalDeliveryKey"]=current.DeliveryKey,["locallyForwarded"]=delivered.Contains(current.DeliveryKey)};}
  public void RestoreCursor(JObject cursor,string independentHash){Alive();if(pending.Count!=0)throw new InvalidOperationException("b_runtime_delivery_in_flight");BDirectAttackInput.Fields(cursor,"version","originalDeliveryKey","locallyForwarded");var current=capture();if(BDirectAttackInput.Digest(cursor)!=independentHash||(string)cursor["version"]!=Version||(string)cursor["originalDeliveryKey"]!=current.DeliveryKey||cursor["locallyForwarded"].Type!=JTokenType.Boolean)throw new ArgumentException("b_runtime_delivery_cursor_conflict");generation=checked(generation+1);delivered.Clear();if((bool)cursor["locallyForwarded"])delivered.Add(current.DeliveryKey);}
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(BRuntimeObservationDelivery));if(invalidating)throw new InvalidOperationException("b_runtime_delivery_invalidating");}
  public void Dispose(){if(disposed)return;try{Invalidate();}finally{disposed=true;delivered.Clear();}}
 }
}
