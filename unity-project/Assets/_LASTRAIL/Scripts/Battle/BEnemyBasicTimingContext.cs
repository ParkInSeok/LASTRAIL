namespace LastRail.Battle {
 // Borrows authoritative combat state; the caller owns the next-basic schedule.
 internal interface IBEnemyBasicTimingContext : IBBoardingActors {
  long NextBasicDue(string id);
  void ScheduleEnemyBasic(string id,long due);
  bool EnemyBasicTargetEligible(string source,string target);
 }
 internal sealed class BEnemyBasicTimingSource {
  internal string Id {get;} internal string Target {get;} internal long PeriodMicroseconds {get;} internal long TargetableAt {get;} internal bool Projectile {get;} internal int Ordinal {get;}
  internal BEnemyBasicTimingSource(string id,string target,long periodMicroseconds,long targetableAt,bool projectile,int ordinal){
   BuffWire.Need(!string.IsNullOrEmpty(id)&&!string.IsNullOrEmpty(target)&&periodMicroseconds>0&&periodMicroseconds<=9007199254740991&&targetableAt>=0&&targetableAt<=9007199254740991&&ordinal>=0,"b_enemy_timing_source");
   Id=id;Target=target;PeriodMicroseconds=periodMicroseconds;TargetableAt=targetableAt;Projectile=projectile;Ordinal=ordinal;
  }
 }
}
