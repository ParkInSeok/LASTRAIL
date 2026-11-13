using System;
using System.Globalization;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Immutable, nonnegative rational. Wire boundary matches combat.exact-rational.v1.
    public sealed class CombatExactValue
    {
        public const string Version="combat.exact-rational.v1";
        const int MaxDigits=256;
        internal BigInteger Numerator { get; }
        internal BigInteger Denominator { get; }
        CombatExactValue(BigInteger numerator,BigInteger denominator) { Numerator=numerator; Denominator=denominator; }
        internal static CombatExactValue Create(BigInteger numerator,BigInteger denominator) {
            if(numerator<0 || denominator<=0) throw new ArgumentException("Invalid exact combat value.");
            var gcd=BigInteger.GreatestCommonDivisor(numerator,denominator);
            numerator/=gcd; denominator/=gcd;
            if(Text(numerator).Length>MaxDigits || Text(denominator).Length>MaxDigits)
                throw new ArgumentOutOfRangeException("value","Exact combat range exceeded.");
            return new CombatExactValue(numerator,denominator);
        }
        public static CombatExactValue From(JObject snapshot) {
            if(snapshot==null || snapshot.Count!=2 || snapshot["numerator"]==null || snapshot["denominator"]==null)
                throw new ArgumentException("Invalid exact combat snapshot.");
            return Create(Integer(snapshot["numerator"]),Integer(snapshot["denominator"]));
        }
        static BigInteger Integer(JToken token) {
            if(token.Type!=JTokenType.String) throw new ArgumentException("Exact integer text required.");
            var text=(string)token;
            if(text.Length==0 || text.Length>MaxDigits || text.Length>1&&text[0]=='0') throw new ArgumentException("Invalid exact integer text.");
            foreach(var c in text) if(c<'0'||c>'9') throw new ArgumentException("Invalid exact integer text.");
            return BigInteger.Parse(text,NumberStyles.None,CultureInfo.InvariantCulture);
        }
        public CombatExactValue Multiply(CombatExactValue other) {
            if(other==null) throw new ArgumentNullException(nameof(other));
            return Create(Numerator*other.Numerator,Denominator*other.Denominator);
        }
        public BigInteger Floor()=>Numerator/Denominator;
        public JObject Snapshot()=>new JObject{["numerator"]=Text(Numerator),["denominator"]=Text(Denominator)};
        internal static string Text(BigInteger value)=>value.ToString(CultureInfo.InvariantCulture);
    }
}
