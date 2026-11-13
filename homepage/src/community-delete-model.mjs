import {CommunityCommandModel} from './community-command-model.mjs';
import {communityCommand} from './community-contracts.mjs';
export class CommunityDeleteModel {
 #command;
 constructor(api,session,options={}){this.#command=new CommunityCommandModel(session,{...options,normalize:input=>communityCommand(input,'delete'),send:(input,options)=>api.deleteCommunityContent(input,options),progress:'deleting',complete:'deleted'});}
 get state(){return this.#command.state;}
 subscribe(fn){return this.#command.subscribe(fn);}
 execute(input){return this.#command.execute(input);}
 retry(){return this.#command.retry();}
 clear(){return this.#command.clear();}
 dispose(){this.#command.dispose();}
}
