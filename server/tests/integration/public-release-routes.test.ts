import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { publishPublicReleaseIndex, stagePublicCatalogSnapshot } from '../../apps/game-api/src/public-release-store';
import { setup } from '../helpers/departure-fixture';

const release = {
  environment: 'DEV' as const, channel: 'PUBLIC', appVersionCode: '123',
  contentReleaseId: 'release-1', sourceBalanceVersion: 'balance-1',
  sourceDigest: `sha256:${'a'.repeat(64)}`, publicSchemaVersion: 1 as const,
  templateVersion: 'template-1', localeVersion: 'ko-1',
  items: [{ id: 'aug-1', kind: 'AUGMENT' as const, name: 'Augment',
    description: 'Public description', category: 'combat', unit: 'count',
    values: [], conditions: [], stackLimit: null, compatibleWith: [],
    acquisition: [], iconPath: null }],
};

describe('public release HTTP reads', () => {
  it('reads the current index and immutable IDs, with no-store and no invented empty release', async () => {
    const f = await setup(false, true, null);
    const env = { ...f.env, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
    const headers = { ...f.headers, 'X-LastRail-App-Version-Code': '123' };
    f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)", [Date.now() + 60000]);
    const read = (path: string) => app.request(path, { headers }, env);
    expect((await read('/v1/public/releases/current?channel=PUBLIC&versionCode=123')).status).toBe(404);
    const snapshot = await stagePublicCatalogSnapshot(f.env.DB, release, { verify: async () => release });
    const index = await publishPublicReleaseIndex(f.env.DB, {
      environment: 'DEV', channel: 'PUBLIC', appVersionCode: '123', releaseId: release.contentReleaseId,
      snapshotId: snapshot.snapshotId, disclosureIds: [], publishedAt: 100,
    }, null);
    const current = await read('/v1/public/releases/current?channel=PUBLIC&versionCode=123');
    expect(current.status).toBe(200);
    expect(current.headers.get('Cache-Control')).toBe('no-store');
    expect(await current.json()).toMatchObject({ index: { indexId: index.indexId, snapshotId: snapshot.snapshotId } });
    expect(await (await read(`/v1/public/releases/${index.indexId}`)).json()).toMatchObject({ index: { indexId: index.indexId } });
    expect(await (await read(`/v1/public/catalogs/${snapshot.snapshotId}`)).json()).toMatchObject({ snapshot: { snapshotId: snapshot.snapshotId } });
    expect((await read(`/v1/public/releases/pri_${'0'.repeat(64)}`)).status).toBe(404);
  });

  it('keeps DEV reads bound to the exact active tester version and rejects hostile selectors', async () => {
    const f = await setup(false, true, null);
    const env = { ...f.env, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
    const path = '/v1/public/releases/current?channel=PUBLIC&versionCode=123';
    const headers = { ...f.headers, 'X-LastRail-App-Version-Code': '123' };
    expect((await app.request(path, {}, env)).status).toBe(401);
    expect((await app.request(path, { headers }, env)).status).toBe(403);
    f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)", [Date.now() + 60000]);
    expect((await app.request(path.replace('123', '124'), { headers }, env)).status).toBe(403);
    expect((await app.request(path + '&channel=BETA', { headers }, env)).status).toBe(400);
    expect((await app.request(path.replace('PUBLIC', 'INTERNAL'), { headers }, env)).status).toBe(400);
    f.sql.run("UPDATE account_tester_entitlements SET revoked_at=1");
    expect((await app.request(path, { headers }, env)).status).toBe(403);
  });
});
