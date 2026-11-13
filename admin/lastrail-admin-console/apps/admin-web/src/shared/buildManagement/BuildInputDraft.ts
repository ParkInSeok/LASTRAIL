export const buildInputLabels = {platform:'플랫폼',profile:'빌드 프로필',environment:'대상 환경',commit:'정확한 소스 commit',contentVersion:'콘텐츠 version',appVersion:'앱 version',buildNumber:'설치 build 번호',note:'변경 메모'} as const;
export type BuildInput = Record<keyof typeof buildInputLabels,string>;
const emptyInput=():BuildInput=>({platform:'',profile:'',environment:'',commit:'',contentVersion:'',appVersion:'',buildNumber:'',note:''});
/** Local review state only. No request, authorization, runner or response contract. */
export class BuildInputDraft {
 private input=emptyInput(); private generation=0; private listeners=new Set<()=>void>();
 constructor(private scope:string){}
 snapshot(){return Object.freeze({scope:this.scope,generation:this.generation,input:Object.freeze({...this.input}),execution:'unavailable' as const});}
 change(field:keyof BuildInput,value:string){if(!Object.prototype.hasOwnProperty.call(buildInputLabels,field))throw new Error('Unknown build input');if(this.input[field]===value)return;this.input={...this.input,[field]:value};this.emit();}
 reset(scope:string){this.scope=scope;this.generation++;this.input=emptyInput();this.emit();}
 subscribe(listener:()=>void){this.listeners.add(listener);return()=>{this.listeners.delete(listener);};}
 private emit(){for(const listener of this.listeners)listener();}
 review(){const issues:string[]=[];for(const key of Object.keys(buildInputLabels) as (keyof BuildInput)[])if(!this.input[key].trim())issues.push(`${buildInputLabels[key]} 필요`);
 if(!['DEV','PRD'].includes(this.input.environment))issues.push('DEV 또는 PRD 선택 필요');
 if(!/^(?:[a-f\d]{40}|[a-f\d]{64})$/i.test(this.input.commit))issues.push('정확한 소스 commit 필요');
 if(!/^[1-9]\d*$/.test(this.input.buildNumber))issues.push('양의 정수 build 번호 필요');
 return {ready:issues.length===0,issues};}
}

