import {createServer,type IncomingMessage,type ServerResponse} from 'node:http';
import {Buffer} from 'node:buffer';
/** One loopback TCP resource owner. No public bind/forwarded host or credential endpoint. */
export async function startWorldBossLoopback(handle:(request:Request)=>Promise<Response>,port=0,options:{bodyLimit?:number}={}){
 const bodyLimit=options.bodyLimit??18000;if(!Number.isSafeInteger(bodyLimit)||bodyLimit<1||bodyLimit>8000000)throw new Error('world_socket_options_invalid');
 let origin='',closing:Promise<void>|undefined;const active=new Set<Promise<void>>();let finish!:()=>void;const closed=new Promise<void>(resolve=>{finish=resolve;});
 const error=(res:ServerResponse,status:number,code:string)=>{if(!res.destroyed&&!res.writableEnded){res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({accepted:false,errorCode:code}));}};
 const execute=async(req:IncomingMessage,res:ServerResponse)=>{
  const host=new URL(origin).host,hostCount=req.rawHeaders.filter((v,i)=>i%2===0&&v.toLowerCase()==='host').length;
  if(req.headers.host!==host||hostCount!==1||req.socket.remoteAddress!=='127.0.0.1'){error(res,403,'world_socket_host_forbidden');return;}
  if(req.headers.origin&&req.headers.origin!==origin){error(res,403,'world_socket_origin_forbidden');return;}
  if(!req.url?.startsWith('/')||req.url.startsWith('//')){error(res,404,'world_socket_route_not_found');return;}
  const url=new URL(req.url,origin);if(url.origin!==origin){error(res,403,'world_socket_host_forbidden');return;}
  const chunks:Buffer[]=[];let size=0;for await(const chunk of req){const part=Buffer.from(chunk);size+=part.length;if(size>bodyLimit){error(res,413,'world_socket_body_too_large');return;}chunks.push(part);}
  const method=req.method??'GET';if(['GET','HEAD'].includes(method)&&size){error(res,400,'contract_violation');return;}
  const headers=new Headers();for(const [key,value] of Object.entries(req.headers)){if(typeof value==='string')headers.set(key,value);}
  const response=await handle(new Request(url,{method,headers,...(!['GET','HEAD'].includes(method)?{body:Buffer.concat(chunks)}:{})}));
  if(!res.destroyed&&!res.writableEnded){res.writeHead(response.status,Object.fromEntries(response.headers));res.end(Buffer.from(await response.arrayBuffer()));}
 };
 const server=createServer({requestTimeout:5000,headersTimeout:5000,maxHeaderSize:16384},(req,res)=>{const task=execute(req,res).catch(()=>error(res,503,'world_socket_request_failed'));active.add(task);void task.then(()=>active.delete(task));});
 const close=()=>{if(!closing)closing=(async()=>{if(server.listening){const stopped=new Promise<void>((resolve,reject)=>server.close(err=>err?reject(err):resolve()));server.closeAllConnections();await stopped;}await Promise.allSettled([...active]);finish();})();return closing;};
 try{await new Promise<void>((resolve,reject)=>{server.once('error',reject);server.listen(port,'127.0.0.1',()=>{server.off('error',reject);resolve();});});const address=server.address();if(!address||typeof address==='string')throw new Error('world_socket_bind_failed');origin=`http://127.0.0.1:${address.port}`;return {origin,close,closed};}catch(error){await close();throw error;}
}