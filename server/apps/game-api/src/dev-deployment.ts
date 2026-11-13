import app from './index';
import {isDevDeploymentRunning,deploymentPausedResponse,type DevDeploymentEnvironment} from '../../../packages/common/src/dev-deployment';
export default {
  fetch(request: Request, env: Env & DevDeploymentEnvironment, ctx: ExecutionContext) {
    if (!isDevDeploymentRunning(env)) return deploymentPausedResponse();
    return app.fetch(request,env,ctx);
  },
};
export {RealtimeCoopRoom} from './dev-deployment-room';
