import {mediaAssets} from './src/media-config.mjs';
import {createServer} from 'node:http';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {resolve} from 'node:path';
import {routes} from './src/content.mjs';
import {staticSurface} from './src/static-status.mjs';
import {scenarioStatus} from './qa/scenarios.mjs';
const root=fileURLToPath(new URL('.',import.meta.url));
const modules=['api-http','api-contracts','web-api','web-session-model','support-model','support-read-model','maintenance-login-model','community-delete-model','community-edit-model','community-command-model','community-contracts','community-comment-model','community-read-model','community-post-model','community-block-model','community-public-read-model','community-consent-model','community-consent-contracts','community-report-contracts','community-report-model','media-config','media-view','media-playback','media-controller','hero-view','faq-search','html','reading-route','reading-model','reading-view','bootstrap','controller','content','read-source','status','views'];
const assets=new Map([...mediaAssets(),['/art/title-emblem-v01-576.webp',['public/art/title-emblem-v01-576.webp','image/webp']],...['1280','1840','mobile-480','mobile-960'].map(size=>['/art/lastrail-keyvisual-v01-'+size+'.webp',['public/art/lastrail-keyvisual-v01-'+size+'.webp','image/webp']]),['/art/lastrail-keyvisual-v01.png',['public/art/lastrail-keyvisual-v01.png','image/png']],['/art/title_emblem-v01.png',['public/art/title_emblem-v01.png','image/png']],['/art/armored-train-v03-wing.png',['public/art/armored-train-v03-wing.png','image/png']],['/art/rail-yard-v01.png',['public/art/rail-yard-v01.png','image/png']],['/art/reina-hero-v04.png',['public/art/reina-hero-v04.png','image/png']],['/styles.css',['public/styles.css','text/css']],...modules.map(n=>['/src/'+n+'.mjs',['src/'+n+'.mjs','text/javascript']])]);
export function createPreviewServer(){return createServer((req,res)=>{
 const common={'Cache-Control':'no-store','X-Content-Type-Options':'nosniff','Referrer-Policy':'no-referrer','Content-Security-Policy':"default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; media-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'"};
 const send=(code,type,body,extra={})=>{res.writeHead(code,{...common,'Content-Type':type+'; charset=utf-8',...extra});res.end(req.method==='HEAD'?undefined:body);};
 if(!/^(127\.0\.0\.1|localhost)(:\d+)?$/.test(req.headers.host??''))return send(403,'text/plain','Loopback host required');
 if(!['GET','HEAD'].includes(req.method))return send(405,'text/plain','Read-only local preview',{'Allow':'GET, HEAD'});
 let path;try{path=new URL(req.url,'http://localhost').pathname;}catch{return send(400,'text/plain','Bad request');}
  if(path==='/__qa/reading-source.mjs')return send(200,'text/javascript',readFileSync(resolve(root,'qa/reading-source.mjs')));
 if(/^\/__qa\/reading\/(wiki|probabilities|notices)\/(loading|empty|failure|retry|unavailable)$/.test(path)){
  const html=readFileSync(resolve(root,'index.html'),'utf8').replace('<body>','<body><aside class="qa-label" aria-label="QA 안내">LOCAL QA · 읽기 상태 검증 · 실제 공개 자료가 아닙니다</aside>');
  return send(200,'text/html',html);
 }
 const asset=assets.get(path);if(asset)return send(200,asset[1],readFileSync(resolve(root,asset[0])));
 const match=path.match(/^\/__qa\/([a-z-]+)(\/.*)?$/);
 const scenario=match?.[1], localPath=match?(match[2]??'/'):path;
 if(localPath==='/robots.txt')return send(200,'text/plain','User-agent: *\nDisallow: /\n');
 if(match){const status=scenarioStatus(scenario);if(!status||!routes.includes(localPath))return send(404,'text/plain','Not found');return send(status.web==='MAINTENANCE'&&localPath!=='/status'?503:200,'text/html',staticSurface(localPath,status,scenario),status.web==='MAINTENANCE'&&localPath!=='/status'?{'Retry-After':'120'}:{});}
 if(path==='/status')return send(200,'text/html',staticSurface('/status',null));
 if(routes.includes(path))return send(200,'text/html',readFileSync(resolve(root,'index.html')));
 return send(404,'text/html',staticSurface('/404',null));
 });}
if(process.argv[1]&&resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const port=Number(process.env.HOMEPAGE_PORT??4173);
 const server=createPreviewServer();
 server.listen(port,'127.0.0.1',()=>process.stdout.write('LAST RAIL preview http://127.0.0.1:'+port+'\n'));
}
















