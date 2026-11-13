import {z} from 'zod';
const stateSchema=z.enum(['PAUSED','RUNNING']);
const serviceSchema=z.enum(['game-api','admin-api','async-worker']);
const object=z.record(z.string(),z.unknown());
const configSchema=object.and(z.object({env:z.object({dev:object.and(z.object({name:z.string(),vars:object.and(z.object({ENVIRONMENT:z.literal('DEV')}))}))})}));
const observationSchema=z.object({environment:z.literal('DEV'),queueName:z.literal('lastrail-admin-jobs-dev'),deliveryPaused:z.literal(true),checkedAt:z.number().int().nonnegative()});
/** A caller-supplied observation is preparation evidence, NOT an atomic live lock. */
export function createDevDeploymentConfig(config:unknown,service:unknown,state:unknown,observation:unknown,now:number=Date.now()) {
  const input=configSchema.parse(config),target=serviceSchema.parse(service),mode=stateSchema.parse(state),evidence=observationSchema.parse(observation);
  if(!Number.isSafeInteger(now)||evidence.checkedAt>now||now-evidence.checkedAt>300000)throw Error('deployment_queue_observation_stale');
  if(input.env.dev.name!==`lastrail-${target}-dev`)throw Error('deployment_target_mismatch');
  // This entry does not gate Durable Object RPC/WebSockets. Refuse configurations
  // that would expose them instead of pretending the HTTP gate covers all writes.
  if(input.env.dev.durable_objects)throw Error('deployment_durable_objects_not_supported');
  const root:Record<string,unknown>=structuredClone(input);
  delete root.durable_objects;delete root.migrations;
  const result={...root,main:'src/dev-deployment.ts',env:{dev:{...structuredClone(input.env.dev),main:'src/dev-deployment.ts',vars:{...input.env.dev.vars,DEPLOYMENT_STATE:mode}}}};
  // Top-level DO migrations are not used by this DEV-only configuration.

  return result;
}
