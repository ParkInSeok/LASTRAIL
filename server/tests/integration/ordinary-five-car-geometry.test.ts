import {readFileSync} from 'node:fs';
import {expect, it} from 'vitest';
import {sha256Hex} from '@lastrail/common';
import {ordinaryGeometryDefinitionSchema, OrdinaryNativeGeometryRegistry} from '../../apps/game-api/src/ordinary-native-geometry';
import {verifyOrdinaryLocalGeometry, prepareOrdinaryLocalSession} from '../../scripts/local/ordinary-session-bootstrap';
import {commanderFixture} from '../helpers/commander-db-fixture';
import {OrdinaryLocalNodeRuntime} from '../../apps/game-api/src/ordinary-local-entry';

const root='D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Content/Battle/';
const path=root+'OrdinaryFiveCarV01/definition.json', raw=readFileSync(path,'utf8');
const pin='e32639b12619b00e47cbc93abfefb0f4ec3db6fbb9d8910aa4a37cbc2737866e';
const candidate=()=>JSON.parse(raw);

it('blocks new legacy boarding preparation before account seeding or runtime source assembly', async()=>{
  const oldPath=root+'OrdinaryBoardingV01/definition.json', bytes=readFileSync(oldPath,'utf8');
  await expect(prepareOrdinaryLocalSession('D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/five-car-geometry-v01/no-session-issued',oldPath,await sha256Hex(bytes),{}))
    .rejects.toThrow('ordinary_train_support_definition_unavailable');
  let reads=0;
  const db={prepare:()=>{reads++;throw new Error('unexpected source read');}} as unknown as D1Database;
  await expect(new OrdinaryLocalNodeRuntime(db).prepare({geometry:{definition:JSON.parse(bytes)}} as never))
    .rejects.toThrow('ordinary_train_support_definition_unavailable');
  expect(reads).toBe(0);
});

it('verifies and registers new six-body geometry and both historical three-body definitions without admission',async()=>{
  const f=await commanderFixture();
  try {
    for(const [bytes,hash] of [[raw,pin], ...['OrdinaryLiveGeometryV02','OrdinaryBoardingV01'].map(p=>[readFileSync(root+p+'/definition.json','utf8'),''])]) {
      const digest=hash||await sha256Hex(bytes!);
      const value=await verifyOrdinaryLocalGeometry(bytes!,digest);
      const registry=new OrdinaryNativeGeometryRegistry(f.db,{definitionId:value.definitionId,version:value.version,definitionHash:digest});
      expect((await registry.register(bytes!)).executionVerified).toBe(false);
      expect((await registry.read()).definition).toEqual(value);
    }
    await expect(prepareOrdinaryLocalSession('D:/Project/LASTRAIL/LocalTemp',path,pin,{})).rejects.toThrow('local_five_car_ownership_binding_unavailable');
  } finally {f.sql.close();}
});

it('rejects inconsistent count, order, occupants, lengths, coordinates, body and foot geometry',()=>{
  const changes=[
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.physicalCarCount=5,
    (x: ReturnType<typeof candidate>)=>x.source.boardingGeometry.cars.reverse(),
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.operatorFootprints[1].operatorId='op_kyle',
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.operatorFootprints[1].carOrder=1,
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.totalLengthMm++,
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.initialTailArcMm++,
    (x: ReturnType<typeof candidate>)=>x.source.liveLayout.Car0.originMm[0]++,
    (x: ReturnType<typeof candidate>)=>x.source.liveLayout.Enemy.originMm[0]++,
    (x: ReturnType<typeof candidate>)=>x.entities[1].shapes[0].sizeMm[0]++,
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.operatorFootprints[0].footLocalMm[0]++,
    (x: ReturnType<typeof candidate>)=>delete x.source.formationGeometry,
    (x: ReturnType<typeof candidate>)=>x.source.formationGeometry.unknown=true,
    (x: ReturnType<typeof candidate>)=>x.source.boardingGeometry.cars[1].sockets.left.socketId=x.source.boardingGeometry.cars[0].sockets.left.socketId,
  ];
  for(const mutate of changes){const value=candidate();mutate(value);expect(ordinaryGeometryDefinitionSchema.safeParse(value).success).toBe(false);}
});

it('rejects missing, substituted, duplicated historical roles and altered asset or original entity hashes',async()=>{
  const original='Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingV01/definition.json';
  const changes=[
    (x: ReturnType<typeof candidate>)=>delete x.source.originalAssetHashes[original],
    (x: ReturnType<typeof candidate>)=>x.source.originalAssetHashes[original]='0'.repeat(64),
    (x: ReturnType<typeof candidate>)=>x.source.originalAssetHashes['Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/definition.json']=pin,
    (x: ReturnType<typeof candidate>)=>x.entities[2].assetHash='0'.repeat(64),
    (x: ReturnType<typeof candidate>)=>x.entities[2].shapes[0].radiusMm++,
    (x: ReturnType<typeof candidate>)=>x.source.boardingGeometry.enemyTemplates[0].shapes[0].radiusMm++,
  ];
  for(const mutate of changes){const value=candidate();mutate(value);const bytes=JSON.stringify(value);await expect(verifyOrdinaryLocalGeometry(bytes,await sha256Hex(bytes))).rejects.toThrow();}
  await expect(verifyOrdinaryLocalGeometry(raw+' ',pin)).rejects.toThrow('local_geometry_pin_conflict');
});
