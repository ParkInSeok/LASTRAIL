/** DEV rollout entrypoints only. Absence/typos never resume traffic. */
export type DevDeploymentEnvironment = { ENVIRONMENT?: string; DEPLOYMENT_STATE?: string };
export function isDevDeploymentRunning(env: DevDeploymentEnvironment): boolean {
  return env.ENVIRONMENT === 'DEV' && env.DEPLOYMENT_STATE === 'RUNNING';
}
export function deploymentPausedResponse(): Response {
  return Response.json({error:'dev_deployment_maintenance',retryable:true},
    {status:503,headers:{'Cache-Control':'no-store','Retry-After':'60'}});
}
