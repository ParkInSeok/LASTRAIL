import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
import {geometryVersion,parseGeometryProfile,geometrySampleSchema,type GeometryProfile,type GeometrySample} from './world-boss-rail-geometry-contract';
import {WorldBossRailAttempt} from './world-boss-rail-state';
type Route='ENTRY'|'LEFT'|'RIGHT';
type Kind='HEAD_SPLIT'|'TAIL_SPLIT'|'TAIL_MERGE';
function pose(profile:GeometryProfile,side:'LEFT'|'RIGHT',distance:number){const pts=profile.paths[side].points;if(distance<0||distance>pts.at(-1)!.sMm)throw new Error('geometry_path_exhausted');let i=1;while(i<pts.length-1&&pts[i]!.sMm<distance)i++;const a=pts[i-1]!,b=pts[i]!,t=(distance-a.sMm)/(b.sMm-a.sMm);return {sMm:distance,xMm:a.xMm+(b.xMm-a.xMm)*t,yMm:a.yMm+(b.yMm-a.yMm)*t};}
function interval(a:{xMm:number;yMm:number},b:{xMm:number;yMm:number},area:GeometryProfile['attack']['areas'][number],width:number){let low=0,high=1;for(const [start,delta,min,max] of [[a.xMm,b.xMm-a.xMm,area.minXmm-width,area.maxXmm+width],[a.yMm,b.yMm-a.yMm,area.minYmm-width,area.maxYmm+width]]){if(delta===0){if(start!<min!||start!>max!)return null;continue;}const t1=(min!-start!)/delta!,t2=(max!-start!)/delta!;low=Math.max(low,Math.min(t1,t2));high=Math.min(high,Math.max(t1,t2));if(low>high)return null;}return {low,high};}
function bodyHit(profile:GeometryProfile,side:'LEFT'|'RIGHT',rear:number,front:number,width:number){const points=profile.paths[side].points;for(let i=1;i<points.length;i++){const a=points[i-1]!,b=points[i]!,lo=Math.max(rear,a.sMm),hi=Math.min(front,b.sMm);if(lo>hi)continue;const start=pose(profile,side,lo),end=pose(profile,side,hi);if(profile.attack.areas.some(area=>interval(start,end,area,width)!==null))return true;}return false;}
/** Local authority geometry; authenticated command and durable transaction adapters remain external. */
export class RailGeometryMotion {
 private readonly profile:GeometryProfile;
 private readonly initial:GeometrySample;
 private samples:GeometrySample[]=[];
 private selectedRoute:Route='ENTRY';
 private selectedAtSequence:number|null=null;
 private events:{eventId:string;kind:Kind;previousSampleAtMs:number;observedAtMs:number}[]=[];
 private attackResult:{eventId:string;observedAtMs:number;hit:boolean;hitVehicles:string[]}|null=null;
 constructor(profile:unknown,initial:unknown){this.profile=parseGeometryProfile(profile);this.initial=geometrySampleSchema.parse(initial);if(this.initial.sequence!==0||this.initial.headDistanceMm<this.length()||this.initial.headDistanceMm>=this.profile.paths.LEFT.splitSmm||this.initial.actualMmPerSecond>this.profile.maxSpeedMmPerSecond||this.initial.targetMmPerSecond>this.profile.maxSpeedMmPerSecond)throw new Error('geometry_initial_invalid');this.samples=[structuredClone(this.initial)];}
 private length(){return this.profile.vehicles.reduce((s,v)=>s+v.lengthMm+v.gapAfterMm,0);}
 private side():'LEFT'|'RIGHT'{return this.selectedRoute==='RIGHT'?'RIGHT':'LEFT';}
 selectRoute(side:'LEFT'|'RIGHT') {z.enum(['LEFT','RIGHT']).parse(side);if(this.selectedRoute===side)return this.snapshot();if(this.selectedAtSequence!==null||this.samples.at(-1)!.headDistanceMm>=this.profile.paths.LEFT.splitSmm)throw new Error('geometry_route_locked');this.selectedRoute=side;this.selectedAtSequence=this.samples.at(-1)!.sequence;return this.snapshot();}
 accept(input:unknown){const next=geometrySampleSchema.parse(input),last=this.samples.at(-1)!;const old=this.samples.find(s=>s.sequence===next.sequence);if(old){if(canonicalJson(old)!==canonicalJson(next))throw new Error('geometry_sample_conflict');return this.snapshot();}
  const dt=next.atMs-last.atMs;if(next.sequence!==last.sequence+1||dt<=0||dt>this.profile.maxSampleMs||next.actualMmPerSecond>this.profile.maxSpeedMmPerSecond||next.targetMmPerSecond>this.profile.maxSpeedMmPerSecond||Math.abs(next.actualMmPerSecond-last.actualMmPerSecond)*1000>this.profile.maxAccelerationMmPerSecondSquared*dt)throw new Error('geometry_sample_invalid');
  const numerator=BigInt(last.actualMmPerSecond+next.actualMmPerSecond)*BigInt(dt)+BigInt(last.distanceRemainder2000);
  if(next.headDistanceMm!==last.headDistanceMm+Number(numerator/2000n)||next.distanceRemainder2000!==Number(numerator%2000n))throw new Error('geometry_distance_conflict');
  const route=this.profile.paths[this.side()];if(next.headDistanceMm>route.points.at(-1)!.sMm||this.samples.length>=36001)throw new Error('geometry_path_exhausted');
  const rear=next.headDistanceMm-this.length(),previousRear=last.headDistanceMm-this.length();
  for(const [kind,oldValue,value,threshold] of [['HEAD_SPLIT',last.headDistanceMm,next.headDistanceMm,route.splitSmm],['TAIL_SPLIT',previousRear,rear,route.splitSmm],['TAIL_MERGE',previousRear,rear,route.mergeSmm]] as const){if(oldValue<threshold&&value>=threshold&&!this.events.some(e=>e.kind===kind))this.events.push({eventId:JSON.stringify([this.profile.geometryRef,this.profile.roundId,kind]),kind,previousSampleAtMs:last.atMs,observedAtMs:next.atMs});}
  this.samples.push(structuredClone(next));return this.snapshot();
 }
 snapshot(){const sample=this.samples.at(-1)!,physicalRoute=this.side();let offset=0;const vehicles=this.profile.vehicles.map(v=>{const front=pose(this.profile,physicalRoute,sample.headDistanceMm-offset),rear=pose(this.profile,physicalRoute,sample.headDistanceMm-offset-v.lengthMm);offset+=v.lengthMm+v.gapAfterMm;return {...v,front,rear};});return structuredClone({version:geometryVersion,profile:this.profile,initial:this.initial,selectedRoute:this.selectedRoute,selectedAtSequence:this.selectedAtSequence,physicalRoute,sample,samples:this.samples,vehicles,events:this.events,attackResult:this.attackResult,nextBranchAllowed:this.events.some(e=>e.kind==='TAIL_MERGE')});}
 observeAttack(){if(this.attackResult)return structuredClone(this.attackResult);const sample=this.samples.at(-1)!;if(sample.atMs!==this.profile.attack.impactAtMs)throw new Error('geometry_impact_sample_required');const hitVehicles=this.snapshot().vehicles.filter(v=>bodyHit(this.profile,this.side(),v.rear.sMm,v.front.sMm,v.halfWidthMm)).map(v=>v.id);this.attackResult={eventId:JSON.stringify([this.profile.geometryRef,this.profile.roundId,'ATTACK']),observedAtMs:sample.atMs,hit:hitVehicles.length>0,hitVehicles};return structuredClone(this.attackResult);}
 static restore(input:unknown){try{return RailGeometryMotion.restoreUnchecked(input);}catch(error){throw new Error('geometry_snapshot_invalid',{cause:error});}}
 private static restoreUnchecked(input:unknown){const shape=z.object({version:z.literal(geometryVersion),profile:z.unknown(),initial:geometrySampleSchema,selectedRoute:z.enum(['ENTRY','LEFT','RIGHT']),selectedAtSequence:z.number().int().nonnegative().nullable(),samples:z.array(geometrySampleSchema).min(1).max(36001),attackResult:z.unknown().nullable()}).parse(input);const m=new RailGeometryMotion(shape.profile,shape.initial);if(canonicalJson(shape.samples[0]!)!==canonicalJson(shape.initial))throw new Error('geometry_snapshot_invalid');
  for(let i=0;i<shape.samples.length;i++){const s=shape.samples[i]!;if(i)m.accept(s);if(shape.selectedAtSequence===s.sequence){if(shape.selectedRoute==='ENTRY')throw new Error('geometry_snapshot_invalid');m.selectRoute(shape.selectedRoute);}if(shape.attackResult&&s.atMs===m.profile.attack.impactAtMs)m.observeAttack();}
  if(canonicalJson(m.snapshot())!==canonicalJson(input as Parameters<typeof canonicalJson>[0]))throw new Error('geometry_snapshot_invalid');return m;
 }
}
export function bindGeometryResult(railInput:unknown,geometryInput:unknown){const rail=WorldBossRailAttempt.restore(railInput).snapshot(),g=RailGeometryMotion.restore(geometryInput).snapshot();const r=rail.results.find(r=>r.roundId===g.profile.roundId);const cfg=rail.definition.rounds.find(r=>r.roundId===g.profile.roundId);if(g.profile.geometryRef!==JSON.stringify([rail.attemptId,g.profile.roundId])||!r||!cfg||!g.attackResult||rail.definition.contentVersion!==g.profile.contentVersion||r.route!==g.selectedRoute||r.railSuccess===g.attackResult.hit||g.attackResult.observedAtMs>r.resolvedAt)throw new Error('geometry_result_conflict');const receipt=rail.receipts.find(record=>record.receipt.roundId===g.profile.roundId&&record.receipt.internalAction===null)?.receipt;
 const selected=g.selectedAtSequence===null?null:g.samples.find(s=>s.sequence===g.selectedAtSequence);
 if(g.attackResult.observedAtMs<rail.definition.startedAt+cfg.lockAt||(g.selectedRoute==='ENTRY'?(receipt!==undefined||selected!==null):(!receipt||!selected||receipt.acceptedAt!==selected.atMs||receipt.route!==g.selectedRoute)))throw new Error('geometry_result_conflict');
 return {version:geometryVersion,status:'LOCAL_PATH_VERIFIED' as const,result:r,geometryEvidence:{geometryRef:g.profile.geometryRef,roundId:g.profile.roundId,physicalRoute:g.physicalRoute,attack:g.attackResult,events:g.events,nextBranchAllowed:g.nextBranchAllowed}};}
/** Conservative head-distance forbidden intervals for every occupied vehicle body, not its center. */
function forbiddenHeadIntervals(p:GeometryProfile){
 const points=p.paths.RIGHT.points,ranges:{min:number;max:number}[]=[];let offset=0;
 for(const v of p.vehicles){
  for(let i=1;i<points.length;i++){const a=points[i-1]!,b=points[i]!;
   for(const area of p.attack.areas){const hit=interval(a,b,area,v.halfWidthMm);if(hit)ranges.push({min:a.sMm+(b.sMm-a.sMm)*hit.low+offset,max:a.sMm+(b.sMm-a.sMm)*hit.high+offset+v.lengthMm});}
  }
  offset+=v.lengthMm+v.gapAfterMm;
 }
 ranges.sort((a,b)=>a.min-b.min);const merged:{min:number;max:number}[]=[];
 for(const r of ranges){const last=merged.at(-1);if(last&&r.min<=last.max)last.max=Math.max(last.max,r.max);else merged.push({...r});}
 return merged;
}
/** Sufficient bounded safety proof for this exact pinned consist. It does not approve operating values. */
export function validateTraversalWindow(profileInput:unknown,bounds:{previewAtMs:number;lockAtMs:number;impactAtMs:number;nextPreviewAtMs:number;headAtPreviewMm:number;maxAllowedConsistLengthMm:number;guaranteedMinSpeedMmPerSecond:number;maxAllowedSpeedMmPerSecond:number;latencyMs:number}){
 const p=parseGeometryProfile(profileInput),scalar=z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER);
 const b=z.strictObject({previewAtMs:scalar,lockAtMs:scalar,impactAtMs:scalar,nextPreviewAtMs:scalar,headAtPreviewMm:scalar,maxAllowedConsistLengthMm:scalar,guaranteedMinSpeedMmPerSecond:scalar,maxAllowedSpeedMmPerSecond:scalar,latencyMs:scalar}).parse(bounds);
 if(Object.values(b).some(v=>!Number.isSafeInteger(v)||v<0))throw new Error('geometry_window_invalid');
 const reasons:string[]=[],length=p.vehicles.reduce((s,v)=>s+v.lengthMm+v.gapAfterMm,0),end=p.paths.RIGHT.points.at(-1)!.sMm;
 if(!(b.previewAtMs<b.lockAtMs&&b.lockAtMs<b.impactAtMs&&b.impactAtMs<b.nextPreviewAtMs)||b.impactAtMs!==p.attack.impactAtMs)reasons.push('WINDOW_ORDER');
 if(b.maxAllowedConsistLengthMm!==length||b.headAtPreviewMm<length)reasons.push('CONSIST_ENVELOPE');
 if(p.maxConsistLengthMm>=p.paths.RIGHT.splitSmm)reasons.push('PROFILE_APPROACH_CAPACITY');
 if(b.guaranteedMinSpeedMmPerSecond<=0||b.guaranteedMinSpeedMmPerSecond>b.maxAllowedSpeedMmPerSecond||b.maxAllowedSpeedMmPerSecond>p.maxSpeedMmPerSecond)reasons.push('NO_TRAVERSAL_GUARANTEE');
 const minimumHead=(at:number)=>b.headAtPreviewMm+b.guaranteedMinSpeedMmPerSecond*Math.max(0,at-b.previewAtMs-b.latencyMs)/1000;
 const maximumHead=(at:number)=>b.headAtPreviewMm+b.maxAllowedSpeedMmPerSecond*Math.max(0,at-b.previewAtMs+b.latencyMs)/1000;
 const headAtLockMax=maximumHead(b.lockAtMs),minimumHeadAtImpact=minimumHead(b.impactAtMs),maximumHeadAtImpact=maximumHead(b.impactAtMs);
 if(headAtLockMax>=p.paths.RIGHT.splitSmm)reasons.push('HEAD_BEFORE_CONFIRM_DEADLINE');
 const forbiddenHeadWindows=forbiddenHeadIntervals(p);
 if(forbiddenHeadWindows.some(w=>minimumHeadAtImpact<=w.max&&maximumHeadAtImpact>=w.min))reasons.push('INEVITABLE_RIGHT_TAIL_HIT_NOT_SUPPORTED');
 if(minimumHead(b.nextPreviewAtMs)-length<p.paths.RIGHT.mergeSmm)reasons.push('NEXT_BRANCH_BEFORE_TAIL_MERGE');
 if(maximumHeadAtImpact>end||maximumHead(b.nextPreviewAtMs)>end)reasons.push('PATH_ENVELOPE_EXHAUSTED');
 // Open boundaries: touching the inflated attack rectangle is a hit. Finite safe plateaus only.
 const safeHeadWindows=forbiddenHeadWindows.slice(0,-1).flatMap((w,i)=>{const next=forbiddenHeadWindows[i+1]!;
  const lower=Math.max(length,w.max),upper=Math.min(end,next.min);if(lower>=upper)return [];
  return [{minHeadExclusiveMm:lower,maxHeadExclusiveMm:upper,earliestImpactAtMs:b.guaranteedMinSpeedMmPerSecond>0?Math.floor(b.previewAtMs+b.latencyMs+(lower-b.headAtPreviewMm)*1000/b.guaranteedMinSpeedMmPerSecond)+1:null,latestImpactAtMs:b.maxAllowedSpeedMmPerSecond>0?Math.ceil(b.previewAtMs-b.latencyMs+(upper-b.headAtPreviewMm)*1000/b.maxAllowedSpeedMmPerSecond)-1:null}];
 });
 return {supported:reasons.length===0,impactSupported:maximumHeadAtImpact<=end&&reasons.every(r=>r==='NEXT_BRANCH_BEFORE_TAIL_MERGE'||r==='PATH_ENVELOPE_EXHAUSTED'),reasons,forbiddenHeadWindows,safeHeadWindows,headAtLockMax,minimumHeadAtImpact,maximumHeadAtImpact,minimumRearAtImpact:minimumHeadAtImpact-length,minimumRearAtNext:minimumHead(b.nextPreviewAtMs)-length};
}