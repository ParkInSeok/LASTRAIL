using System;
using System.Numerics;
namespace LastRail.Battle {
 // Approved v08 arithmetic, not an admitted named-ability definition.
 public sealed class BInterceptApprovedLocalPolicy {
  public const string Version="unity.combat-b.interception-approved-local.v08.20261004";
  private BInterceptApprovedLocalPolicy(){}
  public static long[] Ticks(string actor,long stepMicroseconds,long interceptionHasteBps){
   if(stepMicroseconds<=0||interceptionHasteBps<0||interceptionHasteBps>9007199254740991)throw new ArgumentException("b_intercept_timing_boundary");
   long preparation,recovery;if(actor=="op_kyle"){preparation=250000;recovery=450000;}else if(actor=="op_sable"){preparation=200000;recovery=650000;}else throw new ArgumentException("b_intercept_unsupported_actor");
   long Quantize(long duration){BigInteger numerator=(BigInteger)duration*10000,denominator=(BigInteger)stepMicroseconds*(10000+(BigInteger)interceptionHasteBps);return checked((long)BigInteger.Max(1,(numerator+denominator-1)/denominator));}
   return new[]{Quantize(preparation),Quantize(recovery)};
  }
 }
}
