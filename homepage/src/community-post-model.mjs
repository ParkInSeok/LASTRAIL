import {CommunityCommandModel} from './community-command-model.mjs';
import {postCommand} from './community-contracts.mjs';
export class CommunityPostModel {
 #command;
 constructor(api,session,options={}){this.#command=new CommunityCommandModel(session,{...options,normalize:postCommand,send:(input,options)=>api.createCommunityPost(input,options),progress:'creating',complete:'created'});}
 get state(){return this.#command.state;}
 subscribe(fn){return this.#command.subscribe(fn);}
 execute(input){return this.#command.execute(input);}
 retry(){return this.#command.retry();}
 clear(){return this.#command.clear();}
 dispose(){this.#command.dispose();}
}
