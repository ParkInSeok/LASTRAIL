using System;
using System.Numerics;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 public sealed class BScoutGunnerClockTiming {
  public BigInteger IntervalTicks {get;}public BigInteger IntervalMicroseconds {get;}public BigInteger EntryNotBeforeTicks {get;}public BigInteger EntryNotBeforeMicroseconds {get;}
  internal BScoutGunnerClockTiming(BigInteger ticks,long step,long group){IntervalTicks=ticks;IntervalMicroseconds=ticks*step;EntryNotBeforeTicks=ticks*group;EntryNotBeforeMicroseconds=IntervalMicroseconds*group;}
 }
 public static class BScoutGunnerTiming {
  public const string CadenceVersion="combat.b-risk-spawn-cadence.v1.20261003";
  internal static CombatExactValue Ratio(BigInteger n,BigInteger d)=>CombatExactValue.Create(n,d);
  internal static CombatExactValue Exact(JObject p){foreach(var name in new[]{"numerator","denominator"})Need(p[name]?.Type==JTokenType.String&&((string)p[name]).Length<=32);return CombatExactValue.From(p);}
  internal static CombatExactValue EntryInterval(string risk,int depth){int r=Array.IndexOf(new[]{"I","II","III","IV","V"},risk);Need(r>=0&&depth>=1&&depth<=11);int milliseconds=depth<=4?1800:depth<=8?1500:1200;return Ratio(milliseconds*10000,10+r);}
  static BigInteger Round(CombatExactValue value,long step){Need(step>0&&step<=9007199254740991);var divisor=value.Denominator*step;var ticks=(value.Numerator+divisor-1)/divisor;return BigInteger.Max(1,ticks);}
  public static BScoutGunnerClockTiming EntryAtClock(string risk,int depth,long stepMicroseconds,int entryGroup){Need(entryGroup>=0&&entryGroup<=8);return new BScoutGunnerClockTiming(Round(EntryInterval(risk,depth),stepMicroseconds),stepMicroseconds,entryGroup);}
  public static BScoutGunnerClockTiming GunnerFlightAtClock(int distanceMillimeters,long stepMicroseconds){Need(distanceMillimeters>=0&&distanceMillimeters<=24000);return new BScoutGunnerClockTiming(Round(Ratio((BigInteger)distanceMillimeters*1000000,24000),stepMicroseconds),stepMicroseconds,0);}
 }
}
