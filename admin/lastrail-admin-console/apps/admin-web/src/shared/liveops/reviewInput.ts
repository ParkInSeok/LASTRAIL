import {parseKst} from './eventScheduleDraft';
export function parseOptionalKst(value:string):number|null{if(value==='')return null;const normalized=/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)?value+':00':value;const parsed=parseKst(normalized);if(parsed===null)throw new Error('날짜와 시각을 KST 형식으로 확인해 주세요.');return parsed;}
