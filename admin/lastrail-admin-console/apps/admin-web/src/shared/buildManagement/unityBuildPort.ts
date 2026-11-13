import {adminApi} from '../api/client';import type {UnityBuildPort} from './UnityBuildManagement';
/** Exact current partial producer methods; no runner, shell or download execution. */
export function unityBuildPort():UnityBuildPort{return {capabilities:()=>adminApi.unityBuildCapabilities(),list:after=>adminApi.unityBuilds(after),detail:id=>adminApi.unityBuild(id),request:(input,key)=>adminApi.requestUnityBuild(input,key),cancel:(id,input,key)=>adminApi.cancelUnityBuild(id,input,key),logs:(id,after)=>adminApi.unityBuildLogs(id,after),artifacts:id=>adminApi.unityBuildArtifacts(id)};}
