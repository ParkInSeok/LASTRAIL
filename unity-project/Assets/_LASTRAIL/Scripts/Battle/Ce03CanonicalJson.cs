using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Preserve the existing ordinal canonical bytes without cloning the token tree.
 // Writers and buffers belong to this call; no shared mutable state or cache.
 internal static class Ce03CanonicalJson {
  public static string Write(JToken token){
   using var text=new StringWriter(CultureInfo.InvariantCulture);
   using var writer=new JsonTextWriter(text){Formatting=Formatting.None};
   WriteToken(token,writer);writer.Flush();return text.ToString();
  }
  static void WriteToken(JToken token,JsonWriter writer){
   if(token is JObject obj){writer.WriteStartObject();foreach(var p in obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal)){writer.WritePropertyName(p.Name);WriteToken(p.Value,writer);}writer.WriteEndObject();}
   else if(token is JArray array){writer.WriteStartArray();foreach(var value in array)WriteToken(value,writer);writer.WriteEndArray();}
   else token.WriteTo(writer);
  }
 }
}
