import {adminApi} from '../api/client';
import type {RewardPreviewPort} from './RewardPreviewModel';
/** Existing typed client keeps authentication and original SUPPORT_READ boundary. */
export function rewardPreviewReadPort():RewardPreviewPort{return {capabilities:()=>adminApi.rewardTrackingCapabilities(),preview:input=>adminApi.rewardReviewPreview(input)};}
