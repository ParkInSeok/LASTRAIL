import { z } from 'zod';
export const buffVersion = 'combat.b-carriage-buffs.v1.20261002';
export const buffTypeSchema = z.enum(['BCB01','BCB02','BCB03','BCB04','BCB05','BCB06','BCB07']);
export type BuffType = z.infer<typeof buffTypeSchema>;
const definitions: {type:BuffType; values:number[]; secondary:number[]; group:string}[] = [
 {type:'BCB01',values:[1000,1500,2000,2500,3000],secondary:[],group:'B_DIRECT_DAMAGE_C'},
 {type:'BCB02',values:[800,1100,1400,1700,2000],secondary:[],group:'SKILL_HASTE'},
 {type:'BCB03',values:[1000,1500,2000,2500,3000],secondary:[],group:'INTERCEPTION_HASTE'},
 {type:'BCB04',values:[1500,2000,2500,3000,3500],secondary:[1000,1500,2000,2500,3000],group:'B_DIRECT_DAMAGE_C'},
 {type:'BCB05',values:[800,1100,1400,1700,2000],secondary:[],group:'B_SUPPORT_AMPLIFICATION'},
 {type:'BCB06',values:[40,55,70,85,100],secondary:[20,30,40,50,60],group:'EXISTING_FLAT_DEF'},
 {type:'BCB07',values:[1500,2000,2500,3000,3500],secondary:[],group:'B_DIRECT_DAMAGE_C'},
];
export function captureDefinitions(){return structuredClone(definitions);}
export function readBuffRank(type:BuffType,rank:number){
 const def=definitions.find(d=>d.type===type);
 if(!def||!Number.isInteger(rank)||rank<1||rank>5)throw new Error('combat_b_rank_invalid');
 return {value:def.values[rank-1]!,secondary:def.secondary[rank-1]??0,group:def.group};
}