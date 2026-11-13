using System;
using System.Collections.Generic;
using System.Linq;
namespace LastRail.Resources {
 public static partial class RemoteContentCatalog {
  public sealed class Entry {
   public readonly string Key,Path,Address;public readonly bool Local;readonly string type;
   public Type ResourceType=>Type.GetType(type,true);
   public Entry(string key,string path,string address,string type,bool local){Key=key;Path=path;Address=address;this.type=type;Local=local;}
  }
  static readonly Lazy<Dictionary<string,Entry>> byKey=new Lazy<Dictionary<string,Entry>>(()=>Entries.ToDictionary(e=>e.Key,StringComparer.Ordinal));
  public static Entry Find(string key)=>key!=null&&byKey.Value.TryGetValue(key,out var entry)?entry:null;
  public static string AssetPath(string key)=>Find(key)?.Path;
  public static string LegacyAddress(string key)=>Find(key)?.Address;
  public static IEnumerable<Entry> Select(IEnumerable<string> selectors){var requested=selectors.Distinct(StringComparer.Ordinal).ToArray();return Entries.Where(e=>requested.Any(s=>s.EndsWith("/",StringComparison.Ordinal)?e.Key.StartsWith(s,StringComparison.Ordinal):e.Key==s));}
 }
}
