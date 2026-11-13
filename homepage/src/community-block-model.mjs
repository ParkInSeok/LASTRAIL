import {CommunityCommandModel} from './community-command-model.mjs';
import {SupportReadModel} from './support-read-model.mjs';
import {blockCommand} from './community-contracts.mjs';
// Mutation owns historical receipt only; current relation reads are a separate owner.
export class CommunityBlockModel {
 #command;
 constructor(api,session,options={}){this.#command=new CommunityCommandModel(session,{...options,normalize:blockCommand,send:(input,options)=>api.setCommunityBlock(input,options),progress:'applying',complete:'applied'});}
 get state(){return this.#command.state;}
 subscribe(fn){return this.#command.subscribe(fn);}
 execute(input){return this.#command.execute(input);}
 retry(){return this.#command.retry();}
 clear(){return this.#command.clear();}
 dispose(){this.#command.dispose();}
}
export class CommunityBlockReadModel {
 #read;
 constructor(api,session,options={}){this.#read=new SupportReadModel(api,session,{...options,list:(filters,options)=>api.communityBlocks(filters,options),restartListOnConflict:['community_block_revision_conflict']});}
 get state(){return this.#read.state;}
 subscribe(fn){return this.#read.subscribe(fn);}
 openList(){return this.#read.openList();}
 nextPage(){return this.#read.nextPage();}
 refresh(){return this.#read.refresh();}
 retry(){return this.#read.retry();}
 dispose(){this.#read.dispose();}
}
