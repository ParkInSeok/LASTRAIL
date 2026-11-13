using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Frozen supplied definition data; no duplicated balance table or operating rank grant.
 public sealed class BCarriageBuffDefinitions {
  public const string Version="combat.b-carriage-buffs.v1.20261002";
  readonly JObject original;
  public string Hash {get;}
  public BCarriageBuffDefinitions(string json,string trustedSha256) {
   if(json==null||json.Length>65536)throw new ArgumentException("combat_b_definition_invalid");
   using(var sha=SHA256.Create())Hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","").ToLowerInvariant();
   BuffWire.Need(Hash==trustedSha256,"combat_b_definition_hash");
   var packet=JObject.Parse(json);BuffWire.Fields(packet,"version","definitions");BuffWire.Version(packet);
   var rows=BuffWire.Array(packet["definitions"],7,7);var types=rows.Select(x=>BuffWire.Type(x["type"])).ToArray();
   BuffWire.Need(types.Distinct().Count()==7,"combat_b_definition_duplicate");
   foreach(var row in rows){var d=BuffWire.Object(row);BuffWire.Fields(d,"type","values","secondary","group");
    foreach(var v in BuffWire.Array(d["values"],5,5))BuffWire.Number(v);
    var secondary=BuffWire.Array(d["secondary"],0,5);BuffWire.Need(secondary.Count==0||secondary.Count==5);
    foreach(var v in secondary)BuffWire.Number(v);BuffWire.Text(d["group"],128);
   }
   original=(JObject)packet.DeepClone();
  }
  public JObject Capture()=>(JObject)original.DeepClone();
  internal (long value,long secondary) Rank(string type,int rank){BuffWire.Type(new JValue(type));BuffWire.Need(rank>=1&&rank<=5,"combat_b_rank_invalid");
   var d=((JArray)original["definitions"]).Single(x=>(string)x["type"]==type);var s=(JArray)d["secondary"];
   return (BuffWire.Number(d["values"][rank-1]),s.Count==0?0:BuffWire.Number(s[rank-1]));}
 }
 internal static class BuffWire {
  internal const long Safe=9007199254740991;
  internal static void Need(bool value,string error="combat_b_input_invalid"){if(!value)throw new ArgumentException(error);}
  internal static JObject Object(JToken x)=>x as JObject??throw new ArgumentException("combat_b_object_invalid");
  internal static void Fields(JObject x,params string[] fields){Need(x!=null&&x.Count==fields.Length&&fields.All(f=>x[f]!=null));}
  internal static JArray Array(JToken x,int min,int max){var a=x as JArray;Need(a!=null&&a.Count>=min&&a.Count<=max);return a;}
  internal static string Text(JToken x,int max=128){Need(x?.Type==JTokenType.String&&((string)x).Length>0&&((string)x).Length<=max);return (string)x;}
  internal static string One(JToken x,params string[] values){var s=Text(x);Need(values.Contains(s));return s;}
  internal static long Number(JToken x,long min=0,long max=Safe){Need(x!=null&&(x.Type==JTokenType.Integer||x.Type==JTokenType.Float));
   var d=(double)x;Need(!double.IsNaN(d)&&!double.IsInfinity(d)&&d==Math.Truncate(d)&&d>=min&&d<=max);return checked((long)d);}
  internal static bool Flag(JToken x){Need(x?.Type==JTokenType.Boolean);return (bool)x;}
  internal static string Type(JToken x)=>One(x,"BCB01","BCB02","BCB03","BCB04","BCB05","BCB06","BCB07");
  // Exact JSON.stringify string-array parity for opaque Server source identities.
  // Json.NET deliberately escapes additional Unicode separators, which changes this identity string.
  internal static string ServerSourceId(params string[] values){
   var b=new StringBuilder("[");for(int n=0;n<values.Length;n++){if(n>0)b.Append(',');b.Append('"');var s=values[n];
    for(int p=0;p<s.Length;p++){char c=s[p];switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;
     case '\b':b.Append("\\b");break;case '\f':b.Append("\\f");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;
     default:if(c<32||(char.IsSurrogate(c)&&!(char.IsHighSurrogate(c)&&p+1<s.Length&&char.IsLowSurrogate(s[p+1]))&&!(char.IsLowSurrogate(c)&&p>0&&char.IsHighSurrogate(s[p-1]))))b.Append("\\u").Append(((int)c).ToString("x4",System.Globalization.CultureInfo.InvariantCulture));else b.Append(c);break;}}
    b.Append('"');}return b.Append(']').ToString();
  }
  internal static void Version(JObject x){Need((string)x["version"]==BCarriageBuffDefinitions.Version,"combat_b_version_invalid");}
 }
}