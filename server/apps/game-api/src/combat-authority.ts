import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
export const combatAuthorityVersion="combat.authority-split.v1";
const modeSchema=z.enum(["ORDINARY_SOLO","ORDINARY_COOP","WORLD_BOSS_SOLO","WORLD_BOSS_COOP"]);
/** Policy only: does not assign a host, approve a report, or enable an execution engine. */
export function captureCombatAuthority(input:unknown){
 const mode=modeSchema.parse(input),world=mode==="WORLD_BOSS_SOLO"||mode==="WORLD_BOSS_COOP";
 return {version:combatAuthorityVersion,mode,combatOwner:world?"SERVER":mode==="ORDINARY_COOP"?"UNITY_HOST":"UNITY_LOCAL",
  combatClock:world?"SERVER_COMBAT":mode==="ORDINARY_COOP"?"HOST_LOGICAL":"LOCAL_LOGICAL",
  transactionOwner:"SERVER",resultVerificationOwner:"SERVER",clientReportProvesCombat:false} as const;
}
export function readCombatAuthority(input:unknown){
 if(input===undefined)return {status:"HISTORICAL",contract:null} as const;
 const identity=z.object({mode:modeSchema}).safeParse(input);
 if(identity.success){
  const contract=captureCombatAuthority(identity.data.mode);
  try{if(canonicalJson(input as Parameters<typeof canonicalJson>[0])===canonicalJson(contract))return {status:"PINNED",contract} as const;}catch{ /* Malformed reports never become an authority contract. */ }
 }
 return {status:"INVALID",contract:null} as const;
}
