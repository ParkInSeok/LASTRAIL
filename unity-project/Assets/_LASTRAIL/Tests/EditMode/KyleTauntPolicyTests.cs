using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
public sealed class KyleTauntPolicyTests {
 static KyleTauntPolicy.Candidate C(string id,long distance,int order=0,bool threat=true,bool los=true,bool immune=false)=>new KyleTauntPolicy.Candidate(id,1,(System.Numerics.BigInteger)distance*distance,order,threat,los,immune);
 [Test] public void ThreeNearestEligibleThreatsUseStableTiesAndExcludeFourth(){
  var rows=new[]{C("far",12001),C("wall",1,los:false),C("boss",2,immune:true),C("other-car",3,threat:false),C("d",12000,4),C("c",5000,2),C("b",5000,1),C("a",5000,1)};
  CollectionAssert.AreEqual(new[]{"a","b","c"},KyleTauntPolicy.Select(rows,3,12000).Select(c=>c.Id));
  CollectionAssert.AreEqual(new[]{"d"},KyleTauntPolicy.Select(new[]{C("d",12000)},3,12000).Select(c=>c.Id));
  Assert.Throws<ArgumentException>(()=>KyleTauntPolicy.Select(new[]{C("same",1),C("same",2)},3,12000));
 }
}
