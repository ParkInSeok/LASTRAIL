import app,{maintenanceInitializationApp} from './index';
import {maintenanceInitializePath} from './maintenance-initialization';
import {isDevDeploymentRunning,deploymentPausedResponse,type DevDeploymentEnvironment} from '../../../packages/common/src/dev-deployment';
export default {
  fetch(request: Request, env: Env & DevDeploymentEnvironment, ctx: ExecutionContext) {
    if(new URL(request.url).pathname===maintenanceInitializePath){
      if(request.method==='POST'&&env.ENVIRONMENT==='DEV'&&env.DEPLOYMENT_STATE==='PAUSED')return maintenanceInitializationApp.fetch(request,env,ctx);
      return deploymentPausedResponse();
    }
    if (!isDevDeploymentRunning(env)) return deploymentPausedResponse();
    return app.fetch(request,env,ctx);
  },
};
