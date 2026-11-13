import {expect,it} from "vitest";
import {captureCombatAuthority,readCombatAuthority} from "../../apps/game-api/src/combat-authority";
it.each([
 ["ORDINARY_SOLO","UNITY_LOCAL","LOCAL_LOGICAL"],
 ["ORDINARY_COOP","UNITY_HOST","HOST_LOGICAL"],
 ["WORLD_BOSS_SOLO","SERVER","SERVER_COMBAT"],
 ["WORLD_BOSS_COOP","SERVER","SERVER_COMBAT"],
])("assigns one combat owner for %s without moving economic authority",(mode,owner,clock)=>{
 expect(captureCombatAuthority(mode)).toMatchObject({mode,combatOwner:owner,combatClock:clock,transactionOwner:"SERVER",resultVerificationOwner:"SERVER",clientReportProvesCombat:false});
});
it("rejects forged owners, unknown modes and future versions",()=>{
 const contract=captureCombatAuthority("ORDINARY_COOP");
 expect(readCombatAuthority({...contract,combatOwner:"SERVER"}).status).toBe("INVALID");
 expect(readCombatAuthority({...contract,version:"future"}).status).toBe("INVALID");
 expect(()=>captureCombatAuthority("RAID_BOSS")).toThrow();
});
it("does not reinterpret historical snapshots or confuse policy with host assignment",()=>{
 expect(readCombatAuthority(undefined)).toEqual({status:"HISTORICAL",contract:null});
 expect(readCombatAuthority(null).status).toBe("INVALID");
 const contract=captureCombatAuthority("ORDINARY_COOP");expect(contract).not.toHaveProperty("hostAccountId");
 expect(readCombatAuthority(contract)).toEqual({status:"PINNED",contract});
});
