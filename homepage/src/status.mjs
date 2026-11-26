const keys = ['revision','game','web','updatedAt','validUntil','summary'];
export function validateStatus(value, now=Date.now()) {
  if (!value || typeof value !== 'object' || Object.keys(value).some(k=>!keys.includes(k))) return null;
  if (!keys.every(k=>typeof value[k]==='string' && value[k].length<=1000)) return null;
  if (!value.revision || !['NORMAL','DRAINING','MAINTENANCE'].includes(value.game) || !['NORMAL','READ_ONLY','MAINTENANCE'].includes(value.web)) return null;
  const updated=Date.parse(value.updatedAt), until=Date.parse(value.validUntil);
  if (!Number.isFinite(updated)||!Number.isFinite(until)||updated>now||until<=updated) return null;
  return Object.freeze({...value});
}
export function statusPresentation(value,now=Date.now()) {
  const data=validateStatus(value,now);
  if (!data) return {game:'상태 미확인',web:'상태 미확인',summary:'최신 서비스 상태를 확인할 수 없습니다.',stale:false,blockPage:false,canWrite:false,updatedAt:null};
  const stale=Date.parse(data.validUntil)<=now;
  const labels={NORMAL:'정상',DRAINING:'새 작전 시작 제한',MAINTENANCE:'점검 중',READ_ONLY:'읽기 전용'};
  return {game:stale?'갱신 지연 · 현재 미확인':labels[data.game],web:stale?'갱신 지연 · 현재 미확인':labels[data.web],summary:data.summary,stale,blockPage:data.web==='MAINTENANCE',canWrite:!stale&&data.web==='NORMAL',updatedAt:data.updatedAt};
}
