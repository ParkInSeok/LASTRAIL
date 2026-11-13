type Feature='unity-build'|'notice-management'|'ops-errors';
/** Lifetime registry only: each feature's own Model/Controller still owns state, rules and requests. */
export class AdminSessionResources {
 private owners=new Map<Feature,{dispose():void}>();private listeners=new Set<()=>void>();private alive=true;
 constructor(readonly generation:string){if(!generation)throw new Error('admin_session_generation_required');}
 get active(){return this.alive;}
 acquire<T extends {dispose():void}>(key:Feature,create:()=>T):T{if(!this.alive)throw new Error('admin_session_inactive');let owner=this.owners.get(key);if(!owner){owner=create();this.owners.set(key,owner);}return owner as T;}
 subscribe(fn:()=>void){if(!this.alive)return()=>{};this.listeners.add(fn);return()=>{this.listeners.delete(fn);};}
 revoke(){if(!this.alive)return;this.alive=false;for(const owner of this.owners.values())owner.dispose();this.owners.clear();for(const listener of this.listeners)listener();this.listeners.clear();}
 dispose(){this.revoke();}
}
