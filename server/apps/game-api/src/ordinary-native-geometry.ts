import { firstEnemyTrainBodyDefinition, firstEnemyTrainBodyId } from './combat-first-enemy-spatial';
import { z } from 'zod';
import { sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';

const id = z.string().min(1).max(200), hash = z.string().regex(/^[a-f0-9]{64}$/u);
const path = z.string().min(1).max(1000).refine(s => s.startsWith('Assets/') && !s.includes('\\') && !s.split('/').includes('..'));
const point = z.tuple([z.number().finite(), z.number().finite(), z.number().finite()]);
const size = point.refine(p => p.every(n => n > 0));
const anchor = z.strictObject({ id, role: z.enum(['FOOT', 'EMISSION', 'HIT']), positionMm: point, abilityId: id.optional() })
  .refine(a => (a.role === 'EMISSION') === (a.abilityId !== undefined));
const shape = z.discriminatedUnion('kind', [
  z.strictObject({ id, role: z.enum(['HIT_VOLUME', 'OCCLUDER']), kind: z.literal('BOX'), centerMm: point, sizeMm: size }),
  z.strictObject({ id, role: z.enum(['HIT_VOLUME', 'OCCLUDER']), kind: z.literal('CAPSULE_Y'), centerMm: point,
    radiusMm: z.number().positive().finite(), heightMm: z.number().positive().finite() })
    .refine(s => s.heightMm >= 2 * s.radiusMm),
]);
const localLayout = z.strictObject({ originMm: point, basis: z.tuple([point, point, point]), relativeToInitialFrontMm: point,
  carOrder: z.number().int().nonnegative().optional(), formationSlot: z.number().int().nonnegative().nullable().optional(),
  gunnerOrdinal: z.number().int().nonnegative().nullable().optional() });
const boardingSocket = z.strictObject({ socketId: id, framePath: id,
  approachPathMm: z.array(point).min(2).max(128), climbPathMm: z.array(point).min(2).max(128) })
  .refine(s => s.approachPathMm.at(-1)!.every((n, i) => n === s.climbPathMm[0]![i]));
const boardingGeometry = z.strictObject({ version: z.literal('ordinary.authored-boarding-geometry.v1.20261107'),
  scope: z.literal('LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION'),
  coordinateSpace: z.literal('CARRIAGE_FRAME_LOCAL'), units: z.literal('MILLIMETERS'),
  cars: z.array(z.strictObject({ carOrder: z.number().int().nonnegative(), authoringId: id, framePath: id,
    roofMinMm: point, roofMaxMm: point, gunnerAnchorsMm: z.array(point).min(1).max(32),
    sockets: z.strictObject({ left: boardingSocket, right: boardingSocket }) })).min(1).max(16),
  bridges: z.array(z.never()).length(0),
  enemyTemplates: z.array(z.strictObject({ definitionRef: z.enum(['enemy_raider_scout', 'enemy_spike_cart_gunner']),
    framePath: id, assetPath: path, assetHash: hash, anchors: z.array(anchor).min(3).max(128),
    shapes: z.array(shape).min(1).max(128) })).length(2),
}).refine(b => new Set(b.cars.map(c => c.carOrder)).size === b.cars.length
  && new Set(b.cars.flatMap(c => [c.sockets.left.socketId, c.sockets.right.socketId])).size === b.cars.length * 2
  && new Set(b.enemyTemplates.map(t => t.definitionRef)).size === 2);
const formationGeometry = z.strictObject({
  status: z.literal('LOCAL_AUTHORING_INITIAL_PLACEMENT_PENDING_SERVER_START_PIN'),
  physicalCarCount: z.literal(6), operatorCarCount: z.literal(5),
  bodyLengthMm: z.number().int().positive().safe(), gapMm: z.number().int().nonnegative().safe(),
  totalLengthMm: z.number().int().positive().safe(), initialFrontArcMm: z.number().int().safe(), initialTailArcMm: z.number().int().nonnegative().safe(),
  operatorFootprints: z.array(z.strictObject({ operatorId: id, carOrder: z.number().int().min(1).max(5),
    formationSlot: z.number().int().min(0).max(4), footLocalMm: point })).length(5),
  physicalIdentityBinding: z.literal('SERVER_ORIGINAL_PHYSICAL_IDS_BY_CAR_ORDER_REQUIRED'),
  equipmentRightsBinding: z.literal('SERVER_SELECTED_TYPES_RANKS_REVISIONS_REQUIRED'),
  enemyPlacement: z.literal('ORIGINAL_RELATIVE_TO_FRONT_PLUS_10000_MM'),
  obstaclePlacement: z.literal('ORIGINAL_RELATIVE_TO_FRONT_PLUS_280000_MM'),
});
const localSource = z.strictObject({ scope: z.literal('LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION'),
  scenePath: path, sceneHash: hash, prefabPath: path, prefabHash: hash, originalWorldToArcOffsetMm: z.number().int().safe(),
  originalAssetHashes: z.record(path, hash), liveLayout: z.record(id, localLayout),
  boardingGeometry: boardingGeometry.optional(),
  formationGeometry: formationGeometry.optional(),
  obstacle: z.strictObject({ targetXmm: z.number().int().safe(), zoneStart: z.number().int().safe(), zoneEnd: z.number().int().safe(),
    fallStartMs: z.number().int().nonnegative().safe(), fallSpeedMmPerSecond: z.number().int().positive().safe(),
    landedCenterY: z.number().int().safe(), initialFront: z.number().int().safe(), trackLength: z.number().int().positive().safe(),
    initialCenterMm: point, sizeMm: size, muzzleWorldMm: point, muzzleRelativeToInitialFrontMm: point }) });
const definition = z.strictObject({ version: z.literal('ordinary.native-geometry-definition.v1.20261031'), definitionId: id,
  units: z.literal('MILLIMETERS'), millimetersPerUnit: z.number().positive().finite(),
  axis: z.literal('X_FORWARD_Y_UP_Z_SIDE'), coordinateSpace: z.literal('BOUND_FRAME_LOCAL'),
  source: z.union([z.strictObject({ scenePath: path, sceneHash: hash }), localSource]),
  entities: z.array(z.strictObject({ authoringId: id, role: z.enum(['OPERATOR', 'ENEMY', 'TRAIN', 'DEVICE', 'WORLD']),
    definitionRef: id, framePath: id, assetPath: path, assetHash: hash,
    anchors: z.array(anchor).max(128), shapes: z.array(shape).max(128) })
    .refine(e => e.anchors.length + e.shapes.length > 0
      && new Set(e.anchors.map(a => a.id)).size === e.anchors.length
      && new Set(e.shapes.map(s => s.id)).size === e.shapes.length))
    .min(1).max(128).refine(es => new Set(es.map(e => e.authoringId)).size === es.length),
}).refine(d => {
  if (!('scope' in d.source)) return true;
  const s = d.source, f = s.formationGeometry;
  if (!f) return d.definitionId !== 'ordinary-five-car-boarding-v1';
  const cars = s.boardingGeometry?.cars, same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b);
  if (d.definitionId !== 'ordinary-five-car-boarding-v1' || d.millimetersPerUnit !== 1000
    || !cars || cars.length !== 6 || new Set(cars.map(c => c.authoringId)).size !== 6
    || f.totalLengthMm !== 6 * f.bodyLengthMm + 5 * f.gapMm
    || f.initialFrontArcMm - f.initialTailArcMm !== f.totalLengthMm
    || s.obstacle.initialFront !== f.initialFrontArcMm
    || s.obstacle.targetXmm !== f.initialFrontArcMm + 280000
    || !s.liveLayout.Enemy || !same(s.liveLayout.Enemy.originMm, [f.initialFrontArcMm + 10000, 0, 0])
    || !same(s.liveLayout.Enemy.relativeToInitialFrontMm, [10000, 0, 0])
    || new Set(f.operatorFootprints.map(p => p.operatorId)).size !== 5
    || new Set(f.operatorFootprints.map(p => p.carOrder)).size !== 5
    || d.entities.filter(e => e.role === 'OPERATOR').length !== 5) return false;
  for (const [i, car] of cars.entries()) {
    const e = d.entities.find(e => e.authoringId === car.authoringId), l = s.liveLayout[car.authoringId];
    const x = f.initialFrontArcMm - f.bodyLengthMm / 2 - i * (f.bodyLengthMm + f.gapMm);
    if (car.carOrder !== i || !e || e.role !== 'TRAIN' || e.framePath !== car.framePath || !l || l.carOrder !== i
      || !same(l.originMm, [x, 0, 0]) || !same(l.relativeToInitialFrontMm, [x - f.initialFrontArcMm, 0, 0])
      || !same(l.basis, [[1,0,0],[0,1,0],[0,0,1]])
      || car.roofMinMm.some((n, j) => n > car.roofMaxMm[j]!)
      || [car.sockets.left, car.sockets.right].some(socket => !socket.framePath.startsWith(car.framePath + '/'))
      || !e.shapes.some(b => b.kind === 'BOX' && b.role === 'OCCLUDER' && b.sizeMm[0] === f.bodyLengthMm && b.centerMm[0] === 0)) return false;
  }
  return f.operatorFootprints.every(p => {
    const car = cars[p.carOrder], e = d.entities.find(e => e.role === 'OPERATOR' && e.definitionRef === p.operatorId);
    const l = e && s.liveLayout[e.authoringId], c = car && s.liveLayout[car.authoringId];
    return p.formationSlot + 1 === p.carOrder && !!e && !!l && !!c && !!car
      && e.framePath.startsWith(car.framePath + '/') && l.carOrder === p.carOrder && l.formationSlot === p.formationSlot
      && p.footLocalMm.every((n, i) => n >= car.roofMinMm[i]! && n <= car.roofMaxMm[i]!)
      && d.entities.find(e => e.authoringId === car.authoringId)!.anchors.some(a => a.role === 'FOOT' && same(a.positionMm, p.footLocalMm))
      && same(l.basis, c.basis) && same(l.originMm, c.originMm.map((n, i) => n + p.footLocalMm[i]!))
      && same(l.relativeToInitialFrontMm, l.originMm.map((n, i) => n - (i === 0 ? f.initialFrontArcMm : 0)))
      && e.anchors.some(a => a.role === 'FOOT' && same(a.positionMm, [0,0,0]));
  });
}, {message: 'five_car_formation_geometry_conflict'});
const reference = z.strictObject({ definitionId: id, version: z.literal('ordinary.native-geometry-definition.v1.20261031'), definitionHash: hash });
export const ordinaryGeometryReferenceSchema = reference;
export const ordinaryGeometryDefinitionSchema = definition;
type Reference = z.infer<typeof reference>;
const fail = (reason: string): never => { throw new ActivityError(409, 'ordinary_geometry_' + reason); };

/** Internal publication boundary. The pin must come from independently approved server configuration,
 * never the submitted artifact/client. Registration is NOT gameplay/shape approval or execution readiness.
 * No public endpoint, signing key provisioning or candidate auto-approval is provided. */
export class OrdinaryNativeGeometryRegistry {
  private readonly pin: Reference;
  constructor(private readonly db: D1Database, approvedReference: unknown) { this.pin = reference.parse(approvedReference); }
  private async verified(bytes: string) {
    if (new TextEncoder().encode(bytes).byteLength > 524288) return fail('definition_too_large');
    let value: z.infer<typeof definition>;
    try { value = definition.parse(JSON.parse(bytes)); } catch { return fail('definition_invalid'); }
    if (value.definitionId !== this.pin.definitionId || value.version !== this.pin.version
      || await sha256Hex(bytes) !== this.pin.definitionHash) return fail('definition_conflict');
    if ('scope' in value.source && value.source.boardingGeometry) {
      const layouts = value.source.liveLayout;
      for (const car of value.source.boardingGeometry.cars) {
        const entity = value.entities.find(e => e.authoringId === car.authoringId);
        if (!entity || entity.role !== 'TRAIN' || entity.framePath !== car.framePath
          || !layouts[car.authoringId]
          || (layouts[car.authoringId]!.carOrder !== undefined && layouts[car.authoringId]!.carOrder !== car.carOrder))
          return fail('boarding_car_binding_conflict');
      }
    }
    const bodies = value.entities.filter(e => e.definitionRef === firstEnemyTrainBodyId);
    if (bodies.length) {
      const body = await firstEnemyTrainBodyDefinition();
      const source = value.source;
      const retainedLocalCopy = (e: typeof bodies[number]) => 'scope' in source && !!source.boardingGeometry
        && source.originalAssetHashes[body.assetPath] === body.assetHash
        && e.assetPath === source.prefabPath && e.assetHash === source.prefabHash;
      if (bodies.length !== 1 || bodies.some(e => e.role !== 'TRAIN'
        || (!(e.assetPath === body.assetPath && e.assetHash === body.assetHash) && !retainedLocalCopy(e))
        || e.anchors.some(a => a.role !== 'FOOT') || !e.shapes.length
        || e.shapes.some(s => s.role !== 'OCCLUDER'))) return fail('enemy_body_source_conflict');
    }
    return value;
  }
  private row() {
    return this.db.prepare('SELECT definition_hash,definition_json FROM ordinary_native_geometry_definitions WHERE definition_id=? AND version=?')
      .bind(this.pin.definitionId, this.pin.version).first<{ definition_hash: string; definition_json: string }>();
  }
  async register(definitionJson: string) {
    await this.verified(definitionJson);
    const replay = async () => {
      const row = await this.row(); if (!row) return false;
      if (row.definition_hash !== this.pin.definitionHash || row.definition_json !== definitionJson) return fail('publication_conflict');
      return true;
    };
    if (!await replay()) {
      try { await this.db.prepare('INSERT INTO ordinary_native_geometry_definitions VALUES(?,?,?,?)')
        .bind(this.pin.definitionId, this.pin.version, this.pin.definitionHash, definitionJson).run(); }
      catch (error) { if (!await replay()) throw error; }
    }
    return { ...this.pin, executionVerified: false as const };
  }
  async read() {
    const row = await this.row(); if (!row) return fail('definition_unavailable');
    if (row.definition_hash !== this.pin.definitionHash) return fail('definition_conflict');
    const value = await this.verified(row.definition_json);
    return { reference: { ...this.pin }, definition: value, definitionJson: row.definition_json,
      assertion: this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
        SELECT 1 FROM ordinary_native_geometry_definitions WHERE definition_id=? AND version=? AND definition_hash=? AND definition_json=?)
        THEN 1 ELSE 0 END`).bind(this.pin.definitionId, this.pin.version, this.pin.definitionHash, row.definition_json) };
  }
}
