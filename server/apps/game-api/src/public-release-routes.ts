import type { Context, Hono } from 'hono';
import { ActivityError } from './activity-policy';
import { readBoundedBody, type ActivityAuthResult } from './activity-routes';
import { publicReadAccess } from './public-read-access';
import { readCurrentPublicReleaseIndex, readPublicCatalogSnapshot, readPublicReleaseIndex } from './public-release-store';

type PublicContext = Context<{ Bindings: Env }>;
const selector = /^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$/u;
const versionCode = /^[1-9][0-9]{0,9}$/u;
const indexId = /^pri_[a-f0-9]{64}$/u;
const snapshotId = /^pcs_[a-f0-9]{64}$/u;

export function isPublicReleaseRead(method: string, path: string): boolean {
  return method === 'GET' && (path === '/v1/public/releases/current' ||
    /^\/v1\/public\/releases\/pri_[a-f0-9]{64}$/u.test(path) ||
    /^\/v1\/public\/catalogs\/pcs_[a-f0-9]{64}$/u.test(path));
}

export function registerPublicReleaseRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: PublicContext) => Promise<ActivityAuthResult>) {
  async function read(c: PublicContext, kind: 'current' | 'index' | 'catalog') {
    c.header('Cache-Control', 'no-store');
    try {
      const environment = c.env.ENVIRONMENT;
      if (environment !== 'DEV' && environment !== 'PRD') return c.json({ error: 'public_release_environment_unavailable' }, 503);
      const denied = await publicReadAccess(c, environment, authenticate);
      if (denied) return denied;
      if ((await readBoundedBody(c)) !== '') return c.json({ error: 'contract_violation' }, 400);
      const params = new URL(c.req.url).searchParams;
      if (kind === 'current') {
        const channel = params.get('channel');
        const code = params.get('versionCode');
        if (params.size !== 2 || !channel || !selector.test(channel) || channel === 'INTERNAL' ||
            !code || !versionCode.test(code) || Number(code) > 2147483647) return c.json({ error: 'contract_violation' }, 400);
        if (environment === 'DEV' && code !== c.req.header('X-LastRail-App-Version-Code')) return c.json({ error: 'tester_forbidden' }, 403);
        const index = await readCurrentPublicReleaseIndex(c.env.DB, environment, channel, code);
        return index ? c.json({ index }) : c.json({ error: 'public_release_not_found' }, 404);
      }
      if (params.size) return c.json({ error: 'contract_violation' }, 400);
      if (kind === 'index') {
        const id = c.req.param('indexId');
        if (!id || !indexId.test(id)) return c.json({ error: 'contract_violation' }, 400);
        const index = await readPublicReleaseIndex(c.env.DB, environment, id);
        if (!index || environment === 'DEV' && index.appVersionCode !== c.req.header('X-LastRail-App-Version-Code'))
          return c.json({ error: 'public_release_not_found' }, 404);
        return c.json({ index });
      }
      const id = c.req.param('snapshotId');
      if (!id || !snapshotId.test(id)) return c.json({ error: 'contract_violation' }, 400);
      const snapshot = await readPublicCatalogSnapshot(c.env.DB, environment, id);
      if (!snapshot || environment === 'DEV' && snapshot.appVersionCode !== c.req.header('X-LastRail-App-Version-Code'))
        return c.json({ error: 'public_catalog_not_found' }, 404);
      return c.json({ snapshot });
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ error: error.code }, error.status);
      return c.json({ error: 'public_release_unavailable' }, 503);
    }
  }
  app.get('/v1/public/releases/current', (c) => read(c, 'current'));
  app.get('/v1/public/releases/:indexId', (c) => read(c, 'index'));
  app.get('/v1/public/catalogs/:snapshotId', (c) => read(c, 'catalog'));
}
