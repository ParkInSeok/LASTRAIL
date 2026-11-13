// Website navigation contract only. These are not server URLs or response fields.
export const readingSections = ['wiki','probabilities','notices'];
const token = /^[A-Za-z0-9][A-Za-z0-9._~-]{0,119}$/;
export function validReadingQuery(q) {
 return !!q && readingSections.includes(q.section) &&
  (q.version===null || typeof q.version==='string'&&token.test(q.version)) &&
  (q.entry===null || typeof q.entry==='string'&&token.test(q.entry)&&q.version!==null);
}
export function parseReadingLocation(url) {
 const qa=url.pathname.match(/^\/__qa\/reading\/(wiki|probabilities|notices)\/(loading|empty|failure|retry|unavailable)$/);
 const section=qa?qa[1]:url.pathname.slice(1);
 if(!readingSections.includes(section))return {query:null,invalid:false,qa:null,path:url.pathname};
 const query={section,version:url.searchParams.get('version'),entry:url.searchParams.get('entry')};
 const invalid=!validReadingQuery(query)||['version','entry'].some(k=>url.searchParams.getAll(k).length>1);
 return {query:Object.freeze(query),invalid,qa:qa?.[2]??null,path:'/'+section};
}
export function readingHref(query) {
 if(!validReadingQuery(query))throw new TypeError('Invalid reading location');
 const params=new URLSearchParams();
 if(query.version!==null)params.set('version',query.version);
 if(query.entry!==null)params.set('entry',query.entry);
 return '/'+query.section+(params.size?'?'+params:'');
}

