using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle {
 // Spaces already-committed effects; never advances combat or applies damage.
 public sealed class ReinaImpactPlayback {
  public sealed class Scheduled {
   public JObject Row {get;internal set;} public double Due {get;internal set;}
  }
  readonly Queue<Scheduled> pending=new Queue<Scheduled>();
  string command;double firstCombat,firstDue,lastCombat;
  public int PendingCount=>pending.Count;
  public Scheduled Enqueue(JObject row,double now){
   double time=(double?)row?["combatSeconds"]??double.NaN;
   string id=(string)row?["commandId"];
   if(string.IsNullOrEmpty(id)||double.IsNaN(time)||double.IsInfinity(time)||time<0||double.IsNaN(now)||double.IsInfinity(now))throw new ArgumentException("Native impact time required.");
   if(command==id&&time<=lastCombat)throw new ArgumentException("Ordered impact times required.");
   if(command!=id){command=id;firstCombat=time;firstDue=now+.3;}
   lastCombat=time;
   var item=new Scheduled{Row=(JObject)row.DeepClone(),Due=firstDue+time-firstCombat};pending.Enqueue(item);return item;
  }
  public Scheduled Take(double now){if(pending.Count==0||now+1e-9<pending.Peek().Due)return null;return pending.Dequeue();}
  public void Cancel(string id){var keep=new List<Scheduled>();while(pending.Count>0){var p=pending.Dequeue();if((string)p.Row["commandId"]!=id)keep.Add(p);}foreach(var p in keep)pending.Enqueue(p);}
  public void Clear(){pending.Clear();command=null;firstCombat=firstDue=lastCombat=0;}
 }
}
