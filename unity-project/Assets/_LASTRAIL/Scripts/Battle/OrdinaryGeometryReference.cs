using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original START metadata; a reference is not proof of installed geometry or execution support.
 public sealed class OrdinaryGeometryReference {
  public const string Version="ordinary.native-geometry-definition.v1.20261031";
  public string DefinitionId {get;}
  public string DefinitionHash {get;}
  internal OrdinaryGeometryReference(JToken token){
   var value=token as JObject;BDirectAttackInput.Fields(value,"definitionId","version","definitionHash");
   if(value["definitionId"]?.Type!=JTokenType.String||value["version"]?.Type!=JTokenType.String||value["definitionHash"]?.Type!=JTokenType.String)throw new ArgumentException("ordinary_geometry_reference_invalid");
   DefinitionId=(string)value["definitionId"];DefinitionHash=(string)value["definitionHash"];
   if(DefinitionId.Length<1||DefinitionId.Length>200||(string)value["version"]!=Version||DefinitionHash.Length!=64||!DefinitionHash.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'))throw new ArgumentException("ordinary_geometry_reference_invalid");
  }
  internal bool Matches(OrdinaryGeometryReference other)=>other!=null&&DefinitionId==other.DefinitionId&&DefinitionHash==other.DefinitionHash;
 }
}
