import {RealtimeCoopRoom as Room,createRealtimeCoopRejected,type RealtimeCoopRoomCreateInput} from './realtime-coop';
import {isDevDeploymentRunning,deploymentPausedResponse,type DevDeploymentEnvironment} from '../../../packages/common/src/dev-deployment';
const maintenanceCloseCode=1012;
const maintenanceReason='DEV deployment maintenance';
/** Preserve the deployed class identity without constructing its storage owner while paused. */
export class RealtimeCoopRoom {
  private readonly room:Room|undefined;
  constructor(ctx:DurableObjectState,env:Env & DevDeploymentEnvironment){
    if(isDevDeploymentRunning(env))this.room=new Room(ctx,env);
    else for(const socket of ctx.getWebSockets())socket.close(maintenanceCloseCode,maintenanceReason);
  }
  async fetch(request:Request):Promise<Response>{
    return this.room ? this.room.fetch(request) : deploymentPausedResponse();
  }
  async createRoom(input:RealtimeCoopRoomCreateInput){
    return this.room ? this.room.createRoom(input) : createRealtimeCoopRejected('dev_deployment_maintenance',maintenanceReason);
  }
  async webSocketMessage(socket:WebSocket,message:string|ArrayBuffer):Promise<void>{
    if(this.room)return this.room.webSocketMessage(socket,message);
    socket.close(maintenanceCloseCode,maintenanceReason);
  }
  async webSocketClose(socket:WebSocket):Promise<void>{
    if(this.room)return this.room.webSocketClose(socket);
  }
}
