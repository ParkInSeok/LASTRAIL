import worker from './index';
import {isDevDeploymentRunning,type DevDeploymentEnvironment} from '../../../packages/common/src/dev-deployment';
export default {
  scheduled(event: ScheduledController, env: Env & DevDeploymentEnvironment, ctx: ExecutionContext) {
    if (!isDevDeploymentRunning(env)) return;
    return worker.scheduled(event,env,ctx);
  },
  queue(batch: MessageBatch, env: Env & DevDeploymentEnvironment, ctx: ExecutionContext) {
    // Delivery must be paused at the queue. A failure here is an alarm/fallback,
    // not durable buffering: provider retry limits and retention still apply.
    if (!isDevDeploymentRunning(env)) throw new Error('deployment_queue_delivery_not_paused');
    return worker.queue(batch,env,ctx);
  },
};
