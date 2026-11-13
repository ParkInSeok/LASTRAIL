import { describe, it, expect } from 'vitest';
import { Hono } from 'hono';
import { commanderFixture } from '../helpers/commander-db-fixture';
import { registerCommanderRoutes } from '../../apps/game-api/src/commander-routes';
async function fixture() { const f = await commanderFixture(), app = new Hono<{
    Bindings: Env;
}>(); registerCommanderRoutes(app, async (c) => c.req.header('Authorization') ? { accountId: c.req.header('Authorization')! } : { status: 401, errorCode: 'unauthorized', message: 'auth' }, f.definition.handlers); const save = {expectedPresetRevision:f.save.expectedPresetRevision,expectedProgressionRevision:f.save.expectedProgressionRevision,rulesVersion:f.save.rulesVersion,traitDefinitionVersion:f.save.traitDefinitionVersion,ranks:f.save.ranks}; const request = (path: string, body?: unknown, account = 'a') => app.request(path, { method: body === undefined ? 'GET' : 'POST', headers: { Authorization: account, 'Content-Type': 'application/json', 'Idempotency-Key': 'same' }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) }, { DB: f.db } as Env); return { ...f, app, request, save }; }
describe('S1 HTTP contracts', () => {
    it('saves without a preferred tag and reads only its own original receipt', async () => { const f = await fixture(); const r = await f.request('/v1/commander/traits/presets/1/save', f.save); expect(r.status).toBe(200); expect(r.headers.get('Cache-Control')).toBe('no-store'); const body = await r.json(); expect(await (await f.request('/v1/commander/traits/commands/same')).json()).toEqual(body); expect((await f.request('/v1/commander/traits/commands/same', undefined, 'b')).status).toBe(404); });
    it('rejects injected context, preferred tags, query and invalid slots', async () => { const f = await fixture(); for (const extra of [{ preferredSynergyTagId: 'x' }, { accountId: 'b' }, { slotId: 2 }])
        expect((await f.request('/v1/commander/traits/presets/1/save', { ...f.save, ...extra })).status).toBe(400); expect((await f.request('/v1/commander/traits/presets/4/save', f.save)).status).toBe(400); expect((await f.request('/v1/commander/traits?all=true')).status).toBe(400); expect(f.count('commander_trait_presets')).toBe(0); });
    it('keeps read-only slots empty and exposes actual support rather than fake effects', async () => { const f = await fixture(); expect(await (await f.request('/v1/commander/traits')).json()).toMatchObject({ contractVersion: 'commander-s1-v2', slots: [{ slotId: 1, presetRevision: 0 }, { slotId: 2, presetRevision: 0 }, { slotId: 3, presetRevision: 0 }], activeSelection: { slotId: null, selectionRevision: 0 }, currentRunApplied: { status: 'NONE' } }); expect(f.count('commander_trait_presets')).toBe(0); });
});

it('requires authentication and reports unsupported production effects',async()=>{
 const f=await commanderFixture(),app=new Hono<{Bindings:Env}>();registerCommanderRoutes(app,async()=>({accountId:'a'}));
 const body={expectedPresetRevision:0,expectedProgressionRevision:1,rulesVersion:f.save.rulesVersion,traitDefinitionVersion:f.save.traitDefinitionVersion,ranks:f.save.ranks};
 const response=await app.request('/v1/commander/traits/presets/1/save',{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':'unsupported'},body:JSON.stringify(body)},{DB:f.db} as Env);expect(response.status).toBe(503);expect(await response.json()).toEqual({error:'commander_definition_unsupported'});expect(f.count('commander_trait_presets')).toBe(0);
 const unauth=await fixture();expect((await unauth.request('/v1/commander/traits',undefined,'')).status).toBe(401);
});

it('rejects duplicate JSON rank keys including escaped aliases',async()=>{
 const f=await fixture();for(const ranks of ['{"TR01":0,"TR01":1}','{"TR01":0,"TR0\\u0031":1}']){
 const body=JSON.stringify(f.save).replace('"ranks":{"TR01":1}', '"ranks":'+ranks);
 const response=await f.app.request('/v1/commander/traits/presets/1/save',{method:'POST',headers:{Authorization:'a','Content-Type':'application/json','Idempotency-Key':'duplicate'},body},{DB:f.db} as Env);expect(response.status).toBe(400);
 }expect(f.count('commander_trait_presets')).toBe(0);
});
