import { Hono } from 'hono';
import { describe,expect,it } from 'vitest';
import { readBoundedBody } from '../../apps/game-api/src/activity-routes';
import { ActivityError } from '../../apps/game-api/src/activity-policy';
function app(limit?:number){const server=new Hono<{Bindings:Env}>();server.post('/',async c=>{try{return c.text(await readBoundedBody(c,limit));}catch(error){if(error instanceof ActivityError)return c.json({error:error.code},error.status);throw error;}});return server;}
describe('finite body transport and unchanged legacy default',()=>{
 it('preserves exactly 18,000 bytes for default callers',async()=>{const server=app();expect((await server.request('/',{method:'POST',body:'x'.repeat(18000)})).status).toBe(200);expect((await server.request('/',{method:'POST',body:'x'.repeat(18001)})).status).toBe(400);});
 it('counts UTF-8 bytes rather than JavaScript characters',async()=>{const server=app();expect((await server.request('/',{method:'POST',body:'😀'.repeat(4500)})).status).toBe(200);expect((await server.request('/',{method:'POST',body:'😀'.repeat(4501)})).status).toBe(400);});
 it('enforces the explicit 128,000 byte edit envelope without loosening default callers',async()=>{const server=app(128000);expect((await server.request('/',{method:'POST',body:'x'.repeat(128000)})).status).toBe(200);expect((await server.request('/',{method:'POST',body:'x'.repeat(128001)})).status).toBe(400);});
 it.each([0,-1,Infinity,NaN,1.5])('fails closed for invalid server limits %s',async limit=>{expect((await app(limit).request('/',{method:'POST',body:'x'})).status).toBe(503);});
});
