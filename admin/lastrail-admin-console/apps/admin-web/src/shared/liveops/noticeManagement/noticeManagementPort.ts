import {adminApi} from '../../api/client';import type {NoticeManagementPort} from './NoticeManagement';
/** Reuses exact verified management routes and existing client error/auth handling. */
export function noticeManagementPort():NoticeManagementPort{return {capabilities:()=>adminApi.liveOpsCapabilities(),list:after=>adminApi.liveOpsNotices(after),detail:id=>adminApi.liveOpsNotice(id),history:(id,before)=>adminApi.liveOpsNoticeHistory(id,before),command:(id,input,key)=>adminApi.liveOpsNoticeCommand(id,input,key)};}
