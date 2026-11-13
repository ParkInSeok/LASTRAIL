import {afterEach,describe,expect,it,vi} from 'vitest';
import * as entry from '../../apps/game-api/src/dev-deployment';
import {RealtimeCoopRoom as Original,createRealtimeCoopRejected} from '../../apps/game-api/src/realtime-coop';
afterEach(()=>vi.restoreAllMocks());
describe('retained DEV Durable Object deployment export',()=>{
 it('retains the existing Cloudflare class name',()=>expect(entry).toHaveProperty('RealtimeCoopRoom'));
 it.each([{}, {ENVIRONMENT:'DEV'}, {ENVIRONMENT:'DEV',DEPLOYMENT_STATE:'PAUSED'}, {ENVIRONMENT:'DEV',DEPLOYMENT_STATE:'running'}, {ENVIRONMENT:'PRD',DEPLOYMENT_STATE:'RUNNING'}])('blocks storage and all room entry points when closed: %j',async(env)=>{
  const socket={close:vi.fn(),deserializeAttachment:()=>{throw Error('paused attachment access');}};
  const state=new Proxy({getWebSockets:()=>[socket]},{get(target,key){if(key in target)return Reflect.get(target,key);throw Error('paused state access '+String(key));}});
  const Room=(entry as unknown as {RealtimeCoopRoom:new(ctx:unknown,env:unknown)=>Original}).RealtimeCoopRoom;
  const room=new Room(state,env);
  expect(socket.close).toHaveBeenCalledWith(1012,'DEV deployment maintenance');
  expect((await room.fetch(new Request('https://test/'))).status).toBe(503);
  const response=await room.createRoom({runId:'run-test',hostAccountId:'account-test',idempotencyKey:'key-test',requestId:'request-test'});
  expect(response.accepted).toBe(false);expect(response.errorCode).toBe('dev_deployment_maintenance');
  await room.webSocketMessage(socket as unknown as WebSocket,'{}');
  await room.webSocketClose(socket as unknown as WebSocket);
  expect(socket.close).toHaveBeenCalledTimes(2);
 });
 it('RUNNING delegates every existing public room operation exactly once',async()=>{
  const blockConcurrencyWhile=vi.fn(),getWebSockets=vi.fn();
  const fetch=vi.spyOn(Original.prototype,'fetch').mockResolvedValue(new Response('original'));
  const createRoom=vi.spyOn(Original.prototype,'createRoom').mockResolvedValue(createRealtimeCoopRejected('existing','existing'));
  const message=vi.spyOn(Original.prototype,'webSocketMessage').mockResolvedValue();
  const close=vi.spyOn(Original.prototype,'webSocketClose').mockResolvedValue();
  const Room=(entry as unknown as {RealtimeCoopRoom:new(ctx:unknown,env:unknown)=>Original}).RealtimeCoopRoom;
  const room=new Room({blockConcurrencyWhile,getWebSockets},{ENVIRONMENT:'DEV',DEPLOYMENT_STATE:'RUNNING'});
  expect(blockConcurrencyWhile).toHaveBeenCalledTimes(1);expect(getWebSockets).not.toHaveBeenCalled();
  const request=new Request('https://test/'),input={runId:'run-test',hostAccountId:'account-test',idempotencyKey:'key-test',requestId:'request-test'},socket={} as WebSocket;
  expect(await (await room.fetch(request)).text()).toBe('original');
  expect((await room.createRoom(input)).errorCode).toBe('existing');
  await room.webSocketMessage(socket,'payload');await room.webSocketClose(socket);
  expect(fetch).toHaveBeenCalledExactlyOnceWith(request);expect(createRoom).toHaveBeenCalledExactlyOnceWith(input);
  expect(message).toHaveBeenCalledExactlyOnceWith(socket,'payload');expect(close).toHaveBeenCalledExactlyOnceWith(socket);
 });
});
