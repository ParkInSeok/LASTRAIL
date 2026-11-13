import {adminApi} from '../api/client';
import type {RewardDraftPort} from './rewardDraftModel';
export function rewardDraftReadWritePort():RewardDraftPort{return {read:batchId=>adminApi.rewardDraft(batchId),save:(batchId,command,key)=>adminApi.saveRewardDraft(batchId,command,key)};}
