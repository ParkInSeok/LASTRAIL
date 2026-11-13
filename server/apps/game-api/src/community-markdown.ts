export const communityMarkdownVersion='community.markdown.v1.20261008' as const;
export class CommunityMarkdownError extends Error {}
function escapeHtml(value:string):string {
 return value.replace(/[&<>"']/gu,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]!));
}
/** Deliberately limited grammar. Emits only fixed tags and escaped text; never accepts HTML.
 * Approved link origins are trusted server policy, not request fields. Empty policy rejects new links.
 * No external fetch, raw HTML, images, embeds, heading/table plugins or arbitrary attributes. */
export function renderCommunityMarkdown(kind:'POST'|'COMMENT',raw:unknown,approvedLinkOrigins:readonly string[]=[]){
 const max=kind==='POST'?10000:1000,points=typeof raw==='string'?Array.from(raw):[];
 const invalidControl=points.some(char=>{const code=char.charCodeAt(0);return code===127||code<32&&![9,10,13].includes(code);});
 if(!['POST','COMMENT'].includes(kind)||typeof raw!=='string'||!raw.trim()||points.length>max||invalidControl)
  throw new CommunityMarkdownError('community_body_invalid');
 const approved=new Set(approvedLinkOrigins.map(origin=>{try{const u=new URL(origin);if(u.protocol!=='https:'||u.origin!==origin||u.username||u.password)throw new Error();return u.origin;}catch{throw new CommunityMarkdownError('community_link_policy_invalid');}}));
 function inline(text:string):string {
  let html='',closeBracket=text.indexOf(']');
  const linkTargets=new Map<number,{end:number;url:URL}|null>();
  for(let i=0;i<text.length;){
   const rest=text.slice(i);
   if(rest.startsWith('`')){const end=text.indexOf('`',i+1);if(end!==-1){html+='<code>'+escapeHtml(text.slice(i+1,end))+'</code>';i=end+1;continue;}}
   if(/^<(?:\/?[a-z]|!|\?)/iu.test(rest))throw new CommunityMarkdownError('community_html_forbidden');
   if(rest.startsWith('!['))throw new CommunityMarkdownError('community_image_forbidden');
   if(text[i]==='['){
    // Move the closer cursor forward only; cache each target rather than rescanning every '[' suffix.
    while(closeBracket!==-1&&closeBracket<=i)closeBracket=text.indexOf(']',closeBracket+1);
    if(closeBracket>i+1&&text[closeBracket+1]==='('){
     if(!linkTargets.has(closeBracket)){
      const end=text.indexOf(')',closeBracket+2),target=end===-1?'':text.slice(closeBracket+2,end);
      if(!target||/\s/u.test(target))linkTargets.set(closeBracket,null);
      else{let url:URL;try{url=new URL(target);}catch{throw new CommunityMarkdownError('community_link_forbidden');}
       if(url.protocol!=='https:'||url.username||url.password||!approved.has(url.origin))throw new CommunityMarkdownError('community_link_forbidden');
       linkTargets.set(closeBracket,{end,url});}
     }
     const link=linkTargets.get(closeBracket);
     if(link){html+='<a href="'+escapeHtml(link.url.href)+'" rel="nofollow noopener noreferrer">'+escapeHtml(text.slice(i+1,closeBracket))+'</a>';i=link.end+1;continue;}
    }
   }
   if(/^https?:\/\//iu.test(rest))throw new CommunityMarkdownError('community_link_forbidden');
   const marker=rest.startsWith('**')?'**':rest.startsWith('__')?'__':rest[0]==='*'?'*':rest[0]==='_'?'_':null;
   if(marker){const end=text.indexOf(marker,i+marker.length);if(end>i+marker.length){const tag=marker.length===2?'strong':'em';html+='<'+tag+'>'+inline(text.slice(i+marker.length,end))+'</'+tag+'>';i=end+marker.length;continue;}}
   html+=escapeHtml(text[i]!);i++;
  }
  return html;
 }
 const lines=raw.replace(/\r\n?/gu,'\n').trim().split('\n'),blocks:string[]=[];
 const fence=(line:string)=>/^```(?:[A-Za-z0-9_-]+)?\s*$/u.test(line);
 const list=(line:string)=>/^(?:[-*] |\d+[.)] )/u.test(line);
 for(let i=0;i<lines.length;){
  if(!lines[i]!.trim()){i++;continue;}
  if(fence(lines[i]!)){i++;const code:string[]=[];while(i<lines.length&&!/^```\s*$/u.test(lines[i]!))code.push(lines[i++]!);
   if(i===lines.length)throw new CommunityMarkdownError('community_markdown_invalid');i++;blocks.push('<pre><code>'+escapeHtml(code.join('\n'))+'</code></pre>');continue;}
  if(list(lines[i]!)){const ordered=/^\d/u.test(lines[i]!),tag=ordered?'ol':'ul',items:string[]=[];
   while(i<lines.length&&(ordered?/^\d+[.)] /u:/^[-*] /u).test(lines[i]!))items.push('<li>'+inline(lines[i++]!.replace(ordered?/^\d+[.)] /u:/^[-*] /u,''))+'</li>');
   blocks.push('<'+tag+'>'+items.join('')+'</'+tag+'>');continue;}
  const paragraph:string[]=[];
  while(i<lines.length&&lines[i]!.trim()&&!fence(lines[i]!)&&!list(lines[i]!))paragraph.push(inline(lines[i++]!));
  blocks.push('<p>'+paragraph.join('<br>')+'</p>');
 }
 return {rendererVersion:communityMarkdownVersion,sanitizedBody:blocks.join('\n')};
}


