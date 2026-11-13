import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {miraTargetIdentitySchema,miraEmissionSchema,miraProjectileSuffixSchema,miraProjectileWireVersion,readMiraEmission,readMiraProjectileSuffix,type BActiveProjectileSource} from './combat-b-active-projectile-source';
export const targetLifecycleVersion='combat.b-target-lifecycle-source.v1.20261003';
export const targetLifecycleWireVersion='combat.b-mira-lifecycle-wire.v2.20261003';
const safe=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),id=z.string().min(1).max(128),hash=z.string().regex(/^sha256:[a-f0-9]{64}$/);
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=async(x:unknown)=>'sha256:'+await sha256Hex(json(x));
export const targetReplacementSchema=z.strictObject({version:z.literal(targetLifecycleVersion),kind:z.literal('REPLACE_SAME_ID_FROM_ORIGINAL_PROFILE'),eventId:id,rootPacketHash:hash,contextId:id,expectedRevision:safe,originalTick:safe,originalSequence:safe.min(1),previous:miraTargetIdentitySchema,resetProfileHash:hash});
const receiptSchema=z.strictObject({version:z.literal(targetLifecycleVersion),eventId:id,rootPacketHash:hash,contextId:id,revision:safe.min(1),originalTick:safe,originalSequence:safe.min(1),previous:miraTargetIdentitySchema,current:miraTargetIdentitySchema,resetProfileHash:hash,previousJournalHash:hash,receiptHash:hash,runtimeReset:z.literal(false)});
export const targetLifecycleSnapshotSchema=z.strictObject({version:z.literal(targetLifecycleVersion),rootPacketHash:hash,contextId:id,revision:safe,currentTarget:miraTargetIdentitySchema,journalHash:hash,receipts:z.array(z.strictObject({input:targetReplacementSchema,receipt:receiptSchema})).max(1024)});
export type TargetLifecycleSnapshot=z.infer<typeof targetLifecycleSnapshotSchema>;
const lockSchema=z.strictObject({version:z.literal(targetLifecycleWireVersion),rootPacketHash:hash,contextId:id,commandId:id,acceptedTick:safe,acceptedSequence:safe.min(1),target:miraTargetIdentitySchema,lifecycleRevision:safe,receiptHash:hash});
export const lifecycleEmissionSchema=miraEmissionSchema.extend({version:z.literal(targetLifecycleWireVersion),lifecycleLock:lockSchema});
export const lifecycleSuffixSchema=miraProjectileSuffixSchema.extend({version:z.literal(targetLifecycleWireVersion),lifecycleLock:lockSchema.nullable(),shots:z.array(z.strictObject({emission:lifecycleEmissionSchema,status:z.enum(['IN_FLIGHT','RESOLVED_HIT','RESOLVED_MISS','CLEANED_TERMINAL']),resolution:z.strictObject({tick:safe,sequence:safe.min(1),result:z.enum(['RESOLVED_HIT','MISS_TARGET_INVALID','CLEANED_TERMINAL']),originalImpactReceiptId:id.nullable()}).nullable()})).max(1024)});
/** Trusted LOCAL fixture projection. Unity still owns actual entity lifetime, HP and event publication.
 * No clock/CD/buff state and no public mutation handler are owned by this journal.
 */
export class BTargetLifecycleFixture {
 private readonly original:BActiveProjectileSource;
 private readonly profile:unknown;
 private readonly profileHash:string;
 private readonly initialHash:string;
 private readonly records:TargetLifecycleSnapshot['receipts']=[];
 private readonly events=new Map<string,TargetLifecycleSnapshot['receipts'][number]>();
 private constructor(source:BActiveProjectileSource,profile:unknown,profileHash:string,initialHash:string){this.original=structuredClone(source);this.profile=structuredClone(profile);this.profileHash=profileHash;this.initialHash=initialHash;}
 static async create(input:BActiveProjectileSource){
 const trustedOriginal=structuredClone(input); // Own the root before hashing crosses an async boundary.
 const {packetHash,...payload}=trustedOriginal;
 if(await digest(payload)!==packetHash||trustedOriginal.version!=='combat.b-active-projectile-source.v1.20261003')throw new ActivityError(409,'combat_b_lifecycle_original_source_conflict');
 const target=trustedOriginal.initialTargetIdentity,entity=trustedOriginal.originalActiveSource.runtimeInput.entities.find(e=>e.id===target.targetId&&e.kind==='LOCAL_ENEMY');
 if(!entity)throw new ActivityError(503,'combat_b_lifecycle_profile_unavailable');
 const profile={definitionVersion:trustedOriginal.originalActiveSource.definition.definitionVersion,runtimeInputHash:trustedOriginal.originalActiveSource.runtimeInputHash,entity:structuredClone(entity),initialShields:structuredClone(trustedOriginal.originalActiveSource.runtimeInput.initialShields),statusPolicy:'PRISTINE_ORIGINAL_NO_PREVIOUS_TARGET_IMMUNITY_OR_STATUS_COPY'};
 const profileHash=await digest(profile),initialHash=await digest({version:targetLifecycleVersion,rootPacketHash:packetHash,contextId:trustedOriginal.contextId,initialTargetIdentity:target,resetProfileHash:profileHash});
 return new BTargetLifecycleFixture(trustedOriginal,profile,profileHash,initialHash);
 }
 get accountId(){return this.original.accountId;}
 get source(){return structuredClone(this.original);}
 get revision(){return this.records.length;}
 private current(){return this.records.at(-1)?.receipt.current??this.original.initialTargetIdentity;}
 private journalHash(){return this.records.at(-1)?.receipt.receiptHash??this.initialHash;}
 private replay(eventId:string,input:string){const old=this.events.get(eventId);if(!old)return null;if(json(old.input)!==input)throw new ActivityError(409,'combat_b_lifecycle_event_conflict');return structuredClone(old.receipt);}
 async replace(input:unknown,authorize:()=>Promise<void>=async()=>{}){
 const parsed=targetReplacementSchema.safeParse(input);if(!parsed.success)throw new ActivityError(400,'combat_b_lifecycle_request_invalid');const request=parsed.data,text=json(request);
 await authorize();const replay=this.replay(request.eventId,text);if(replay)return replay;
 const before=this.revision,previousHash=this.journalHash(),current=this.current();
 if(request.rootPacketHash!==this.original.packetHash||request.contextId!==this.original.contextId||request.expectedRevision!==before||json(request.previous)!==json(current)||request.resetProfileHash!==this.profileHash)throw new ActivityError(409,'combat_b_lifecycle_source_conflict');
 const last=this.records.at(-1)?.receipt;
 if(last&&(request.originalTick<last.originalTick||request.originalTick===last.originalTick&&request.originalSequence<=last.originalSequence))throw new ActivityError(409,'combat_b_lifecycle_order_conflict');
 if(before>=1024)throw new ActivityError(409,'combat_b_lifecycle_capacity');
 const generation='LOCAL_LIFECYCLE:'+await sha256Hex(json({rootPacketHash:this.original.packetHash,contextId:this.original.contextId,eventId:request.eventId,revision:before+1}));
 const body={version:targetLifecycleVersion,eventId:request.eventId,rootPacketHash:this.original.packetHash,contextId:this.original.contextId,revision:before+1,originalTick:request.originalTick,originalSequence:request.originalSequence,previous:structuredClone(current),current:{...structuredClone(current),generation},resetProfileHash:this.profileHash,previousJournalHash:previousHash,runtimeReset:false as const};
 const receipt=receiptSchema.parse({...body,receiptHash:await digest({input:request,receipt:body})});
 await authorize();const late=this.replay(request.eventId,text);if(late)return late;
 if(this.revision!==before||this.journalHash()!==previousHash)throw new ActivityError(409,'combat_b_lifecycle_source_conflict');
 // No fallible work after commit. There is no partially updated target/receipt/journal.
 const entry={input:structuredClone(request),receipt:structuredClone(receipt)};this.records.push(entry);this.events.set(request.eventId,entry);
 return structuredClone(receipt);
 }
 snapshot():TargetLifecycleSnapshot{return structuredClone({version:targetLifecycleVersion,rootPacketHash:this.original.packetHash,contextId:this.original.contextId,revision:this.revision,currentTarget:this.current(),journalHash:this.journalHash(),receipts:this.records});}
 static async restore(trustedOriginal:BActiveProjectileSource,input:unknown){
 const result=targetLifecycleSnapshotSchema.safeParse(input);if(!result.success)throw new ActivityError(400,'combat_b_lifecycle_restore_invalid');const snapshot=result.data,fixture=await BTargetLifecycleFixture.create(trustedOriginal);
 if(snapshot.rootPacketHash!==fixture.source.packetHash||snapshot.contextId!==fixture.source.contextId)throw new ActivityError(409,'combat_b_lifecycle_restore_conflict');
 for(const entry of snapshot.receipts){const receipt=await fixture.replace(entry.input);if(json(receipt)!==json(entry.receipt))throw new ActivityError(409,'combat_b_lifecycle_restore_conflict');}
 if(json(fixture.snapshot())!==json(snapshot))throw new ActivityError(409,'combat_b_lifecycle_restore_conflict');return fixture;
 }
 /** A lock projection requires the actual command owner's acceptance event; this does not accept a skill. */
 commandLock(input:unknown){
 const x=z.strictObject({commandId:id,acceptedTick:safe,acceptedSequence:safe.min(1)}).safeParse(input);if(!x.success)throw new ActivityError(400,'combat_b_lifecycle_lock_invalid');
 let target=this.original.initialTargetIdentity,revision=0,receiptHash=this.initialHash;
 for(const {receipt:r} of this.records){if(r.originalTick>x.data.acceptedTick||r.originalTick===x.data.acceptedTick&&r.originalSequence>x.data.acceptedSequence)break;
 if(r.originalTick===x.data.acceptedTick&&r.originalSequence===x.data.acceptedSequence)throw new ActivityError(409,'combat_b_lifecycle_order_conflict');
 target=r.current;revision=r.revision;receiptHash=r.receiptHash;}
 return structuredClone({version:targetLifecycleWireVersion,rootPacketHash:this.original.packetHash,contextId:this.original.contextId,...x.data,target,lifecycleRevision:revision,receiptHash});
 }
 async read(){const state=this.snapshot();const payload={version:targetLifecycleVersion,rootPacketHash:this.original.packetHash,contextId:this.original.contextId,accountId:this.original.accountId,originalSource:this.source,revision:state.revision,currentTarget:state.currentTarget,journalHash:state.journalHash,snapshot:state,resetProfile:structuredClone(this.profile),resetProfileHash:this.profileHash,runtimeReset:false,wireVersion:targetLifecycleWireVersion,wireSchemas:{replacement:z.toJSONSchema(targetReplacementSchema),snapshot:z.toJSONSchema(targetLifecycleSnapshotSchema),lock:z.toJSONSchema(lockSchema),emission:z.toJSONSchema(lifecycleEmissionSchema),suffix:z.toJSONSchema(lifecycleSuffixSchema)},ownership:'LOCAL_PROJECTION_UNITY_ORIGINAL_LIFETIME_HP_CLOCK_READY_BUFF_AUTHORITY',policy:{target:'SAME_ID_SAME_BATTLEFIELD_ONLY',generation:'DERIVED_FROM_VERIFIED_ORIGINAL_EVENT_NO_CALLER_NEW_GENERATION',newCommandLock:'JOURNAL_PREFIX_AT_ORIGINAL_ACCEPTANCE_TICK_SEQUENCE',oldCommandsAndShots:'KEEP_ORIGINAL_LOCK_GENERATION',reset:'ORIGINAL_TARGET_PROFILE_AND_PRISTINE_STATUS_ONLY',preserve:['ROOT_CONTEXT','ROOT_PACKET_HASH','ACTOR_PHYSICAL_STATS','CLOCK_DEBT','READY_CD','BUFF_STATE','OLD_COMMANDS_SHOTS_RECEIPTS'],journalCapacity:'1024_LOCAL_VALIDATION_BOUND_NOT_GAMEPLAY_SPAWN_CAP'},canExecute:false,startReady:false};return {...payload,packetHash:await digest(payload)};}
}
export class BTargetLifecycleRegistry {
 private readonly fixtures=new Map<string,BTargetLifecycleFixture>();
 find(accountId:string){return this.fixtures.get(accountId);}
 async bind(source:BActiveProjectileSource){const fixture=await BTargetLifecycleFixture.create(source);const old=this.fixtures.get(fixture.accountId);if(old)return old;this.fixtures.set(fixture.accountId,fixture);return fixture;}
 async restore(input:unknown){const entries=z.array(z.strictObject({originalSource:z.unknown(),snapshot:targetLifecycleSnapshotSchema})).max(2).parse(structuredClone(input));const staged=new Map<string,BTargetLifecycleFixture>();for(const entry of entries){const source=entry.originalSource as BActiveProjectileSource;if(!['a','b'].includes(source.accountId)||staged.has(source.accountId))throw new ActivityError(409,'combat_b_lifecycle_restore_conflict');staged.set(source.accountId,await BTargetLifecycleFixture.restore(source,entry.snapshot));}this.fixtures.clear();for(const [account,fixture] of staged)this.fixtures.set(account,fixture);}
 checkpoint(){return [...this.fixtures.values()].map(f=>({originalSource:f.source,snapshot:f.snapshot()}));}
 dispose(){this.fixtures.clear();}
}
/** Only a validated journal authorizes a scoped target identity view for the unchanged original kernel.
 * It is never published as a modified old source, and never creates a new run or clock.
 */
export async function readLifecycleMiraEmission(input:unknown,fixture:BTargetLifecycleFixture){
 const x=lifecycleEmissionSchema.safeParse(input);if(!x.success)throw new ActivityError(400,'combat_b_lifecycle_emission_invalid');const emission=x.data,lock=fixture.commandLock({commandId:emission.commandId,acceptedTick:emission.acceptedTick,acceptedSequence:emission.lifecycleLock.acceptedSequence});
 if(json(lock)!==json(emission.lifecycleLock)||json(emission.target)!==json(lock.target)||emission.sequence<=lock.acceptedSequence)throw new ActivityError(409,'combat_b_lifecycle_lock_conflict');
 const {lifecycleLock,...originalEmission}=emission;void lifecycleLock;
 await readMiraEmission({...originalEmission,version:miraProjectileWireVersion},{...fixture.source,initialTargetIdentity:lock.target});return structuredClone(emission);
}
export async function readLifecycleMiraSuffix(input:unknown,fixture:BTargetLifecycleFixture){
 const result=lifecycleSuffixSchema.safeParse(input);if(!result.success)throw new ActivityError(400,'combat_b_lifecycle_suffix_invalid');const suffix=result.data;
 let identity=fixture.source.initialTargetIdentity;
 if(suffix.commandId!==null){if(!suffix.lifecycleLock||suffix.acceptedTick===null)throw new ActivityError(409,'combat_b_lifecycle_lock_conflict');const expected=fixture.commandLock({commandId:suffix.commandId,acceptedTick:suffix.acceptedTick,acceptedSequence:suffix.lifecycleLock.acceptedSequence});if(json(expected)!==json(suffix.lifecycleLock)||json(suffix.lockedTarget)!==json(expected.target))throw new ActivityError(409,'combat_b_lifecycle_lock_conflict');identity=expected.target;}else if(suffix.lifecycleLock!==null)throw new ActivityError(409,'combat_b_lifecycle_lock_conflict');
 for(const entry of suffix.shots){await readLifecycleMiraEmission(entry.emission,fixture);if(json(entry.emission.lifecycleLock)!==json(suffix.lifecycleLock))throw new ActivityError(409,'combat_b_lifecycle_lock_conflict');}
 const {lifecycleLock,...oldSuffix}=suffix;void lifecycleLock;
 await readMiraProjectileSuffix({...oldSuffix,version:miraProjectileWireVersion,shots:suffix.shots.map(entry=>{const {lifecycleLock,...old}=entry.emission;void lifecycleLock;return {...entry,emission:{...old,version:miraProjectileWireVersion}};})},{...fixture.source,initialTargetIdentity:identity});return structuredClone(suffix);
}
/** expectedPacketHash must be captured from the actual authenticated supplier, never from untrusted input.
 * The trusted original run remains the original root; only this exact lifecycle envelope advances it.
 */
export async function readPinnedTargetLifecyclePacket(input:unknown,trustedOriginal:BActiveProjectileSource,expectedPacketHash:string){
 const x=z.object({packetHash:hash,snapshot:targetLifecycleSnapshotSchema}).safeParse(input);
 if(!x.success)throw new ActivityError(400,'combat_b_lifecycle_packet_invalid');
 if(x.data.packetHash!==expectedPacketHash)throw new ActivityError(409,'combat_b_lifecycle_packet_conflict');
 const fixture=await BTargetLifecycleFixture.restore(trustedOriginal,x.data.snapshot),expected=await fixture.read();
 let equal=false;try{equal=json(input)===json(expected);}catch{/* Invalid external data remains rejected. */}
 if(!equal||expected.packetHash!==expectedPacketHash)throw new ActivityError(409,'combat_b_lifecycle_packet_conflict');return fixture;
}
