import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { hashPassword } from '../../apps/game-api/src/password-auth';
import { publishPublicReleaseIndex, stagePublicCatalogSnapshot } from '../../apps/game-api/src/public-release-store';
import { setup } from '../helpers/departure-fixture';

const origin = 'https://dev.lastrail.app';

describe('browser DEV public reads', () => {
  it('uses the web cookie with exact active tester version and preserves game Bearer reads', async () => {
    const f = await setup(false, true, null);
    const hash = await hashPassword('TestOnlyStrongPassword-123!');
    f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,
      password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,
      [hash.passwordHash, hash.algorithm, hash.paramsJson, 'tester@example.invalid']);
    f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)", [Date.now() + 60000]);
    const env = { ...f.env, WEB_ALLOWED_ORIGIN: origin, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
    const login = await app.request('/v1/web/sessions/password', { method: 'POST',
      headers: { Origin: origin, 'Content-Type': 'application/json' },
      body: JSON.stringify({ loginId: 'tester', password: 'TestOnlyStrongPassword-123!' }) }, env);
    expect(login.status).toBe(200);
    const cookie = login.headers.get('Set-Cookie')!.split(';')[0]!;
    const headers = { Origin: origin, Cookie: cookie, 'X-LastRail-App-Version-Code': '123' };
    for (const path of ['/v1/public/notices?locale=ko', '/v1/public/probability-disclosures']) {
      const response = await app.request(path, { headers }, env);
      expect(response.status).toBe(200);
      expect(response.headers.get('Access-Control-Allow-Origin')).toBe(origin);
      expect(response.headers.get('Access-Control-Allow-Credentials')).toBe('true');
      expect(response.headers.get('Cache-Control')).toBe('no-store');
      expect((await app.request(path, { headers: { ...headers, 'X-LastRail-App-Version-Code': '124' } }, env)).status).toBe(403);
      expect((await app.request(path, { headers: { ...headers, Origin: 'https://evil.example' } }, env)).status).toBe(403);
    }
    expect((await app.request('/v1/public/releases/current?channel=PUBLIC&versionCode=123', { headers }, env)).status).toBe(404);
    const synthetic = { environment: 'DEV' as const, channel: 'PUBLIC', appVersionCode: '123',
      contentReleaseId: 'synthetic-release-1', sourceBalanceVersion: 'synthetic-balance-1',
      sourceDigest: `sha256:${'a'.repeat(64)}`, publicSchemaVersion: 1 as const,
      templateVersion: 'synthetic-template-1', localeVersion: 'synthetic-ko-1',
      items: [{ id: 'synthetic-aug-1', kind: 'AUGMENT' as const, name: 'Fixture Augment',
        description: 'Synthetic browser roundtrip only', category: 'combat', unit: 'count',
        values: [], conditions: [], stackLimit: null, compatibleWith: [], acquisition: [], iconPath: null }] };
    const snapshot = await stagePublicCatalogSnapshot(f.env.DB, synthetic, { verify: async () => synthetic });
    const index = await publishPublicReleaseIndex(f.env.DB, { environment: 'DEV', channel: 'PUBLIC',
      appVersionCode: '123', releaseId: synthetic.contentReleaseId, snapshotId: snapshot.snapshotId,
      disclosureIds: [], publishedAt: 100 }, null);
    const current = await app.request('/v1/public/releases/current?channel=PUBLIC&versionCode=123', { headers }, env);
    expect(current.status).toBe(200);
    expect(current.headers.get('Access-Control-Allow-Origin')).toBe(origin);
    expect((await current.json() as { index: { indexId: string } }).index.indexId).toBe(index.indexId);
    const fixed = await app.request(`/v1/public/releases/${index.indexId}`, { headers }, env);
    expect(fixed.status).toBe(200);
    expect((await fixed.json() as { index: { snapshotId: string } }).index.snapshotId).toBe(snapshot.snapshotId);
    const catalog = await app.request(`/v1/public/catalogs/${snapshot.snapshotId}`, { headers }, env);
    expect(catalog.status).toBe(200);
    expect((await catalog.json() as { snapshot: { items: { id: string }[] } }).snapshot.items[0]?.id).toBe('synthetic-aug-1');
    const preflight = await app.request('/v1/public/notices', { method: 'OPTIONS',
      headers: { Origin: origin, 'Access-Control-Request-Method': 'GET',
        'Access-Control-Request-Headers': 'x-lastrail-app-version-code' } }, env);
    expect(preflight.status).toBe(204);
    expect(preflight.headers.get('Access-Control-Allow-Headers')).toContain('X-LastRail-App-Version-Code');
    expect((await app.request('/v1/public/notices?locale=ko', { headers: {
      ...f.headers, 'X-LastRail-App-Version-Code': '123' } }, env)).status).toBe(200);
    f.sql.run("UPDATE account_tester_entitlements SET revoked_at=1 WHERE account_id='a'");
    expect((await app.request('/v1/public/notices?locale=ko', { headers }, env)).status).toBe(403);
  });
});
