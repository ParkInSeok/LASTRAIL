import {z} from 'zod';
export const geometryVersion='world-boss.rail-geometry.v1.20261002';
const id=z.string().min(1).max(200),n=z.number().int().nonnegative().max(1000000000),positive=n.positive();
const point=z.strictObject({sMm:n,xMm:z.number().int().min(-1000000000).max(1000000000),yMm:z.number().int().min(-1000000000).max(1000000000)});
const path=z.strictObject({splitSmm:positive,mergeSmm:positive,points:z.array(point).min(4).max(128)});
export const geometryProfileSchema=z.strictObject({version:z.literal(geometryVersion),status:z.literal('LOCAL_TEST_PROFILE'),geometryRef:z.string().min(1).max(2407),contentVersion:id,roundId:id,frame:z.literal('PATH_LOCAL_FORWARD_RIGHT_MM'),maxVehicles:positive.max(64),maxConsistLengthMm:positive,maxSpeedMmPerSecond:positive.max(1000000),maxAccelerationMmPerSecondSquared:positive.max(1000000),maxSampleMs:positive.max(10000),paths:z.strictObject({LEFT:path,RIGHT:path}),vehicles:z.array(z.strictObject({id,lengthMm:positive,gapAfterMm:n,halfWidthMm:positive})).min(1).max(64),attack:z.strictObject({impactAtMs:z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER),areas:z.array(z.strictObject({minXmm:z.number().finite(),maxXmm:z.number().finite(),minYmm:z.number().finite(),maxYmm:z.number().finite()})).min(1).max(64)})}).superRefine((p,c)=>{
 const bad=(message:string)=>c.addIssue({code:'custom',message});
 if(p.vehicles.length>p.maxVehicles||new Set(p.vehicles.map(v=>v.id)).size!==p.vehicles.length||p.vehicles.at(-1)!.gapAfterMm!==0||p.vehicles.reduce((s,v)=>s+v.lengthMm+v.gapAfterMm,0)>p.maxConsistLengthMm)bad('geometry_consist_invalid');
 for(const a of p.attack.areas)if(a.minXmm>=a.maxXmm||a.minYmm>=a.maxYmm)bad('geometry_attack_area_invalid');
 for(const [side,route] of Object.entries(p.paths)){
  if(route.points[0]!.sMm!==0||route.splitSmm>=route.mergeSmm)bad('geometry_path_order');
  let deviated=false;
  route.points.forEach((q,i)=>{if(i){const prev=route.points[i-1]!;if(q.sMm<=prev.sMm||q.xMm<=prev.xMm||Math.abs(q.sMm-prev.sMm-Math.hypot(q.xMm-prev.xMm,q.yMm-prev.yMm))>1)bad('geometry_arc_invalid');}
   if(side==='LEFT'&&q.yMm!==0)bad('geometry_mainline_invalid');
   if(side==='RIGHT'&&q.yMm<0)bad('geometry_driver_right_invalid');
   if(q.sMm>route.splitSmm&&q.sMm<route.mergeSmm&&q.yMm>0)deviated=true;
  });
  const split=route.points.find(q=>q.sMm===route.splitSmm),merge=route.points.find(q=>q.sMm===route.mergeSmm);
  if(!split||!merge||split.yMm!==0||merge.yMm!==0||route.points.at(-1)!.sMm<=route.mergeSmm||(side==='RIGHT'&&!deviated))bad('geometry_missing_junction');
 }
 const l=p.paths.LEFT,r=p.paths.RIGHT;
 const prefix=(a:typeof l)=>a.points.filter(q=>q.sMm<=a.splitSmm);
 if(JSON.stringify(prefix(l))!==JSON.stringify(prefix(r)))bad('geometry_approach_mismatch');
 const suffix=(a:typeof l)=>a.points.filter(q=>q.sMm>=a.mergeSmm).map(q=>({sMm:q.sMm-a.mergeSmm,xMm:q.xMm,yMm:q.yMm}));
 if(JSON.stringify(suffix(l))!==JSON.stringify(suffix(r)))bad('geometry_merge_mismatch');
});
export type GeometryProfile=z.infer<typeof geometryProfileSchema>;
export const parseGeometryProfile=(input:unknown)=>geometryProfileSchema.parse(input);
export const geometrySampleSchema=z.strictObject({atMs:z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER),sequence:n,headDistanceMm:n,distanceRemainder2000:n.max(1999),actualMmPerSecond:n.max(1000000),targetMmPerSecond:n.max(1000000),forcedSlowdownBps:n.max(10000)});
export type GeometrySample=z.infer<typeof geometrySampleSchema>;