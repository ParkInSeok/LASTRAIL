import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,readdirSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {startCommunityLoopback,createLoopbackBrowserTransport,syntheticLocalPassword} from 'file:///D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/community-loopback-http.ts';
const root='D:/Project/LASTRAIL',home=root+'/homepage',server=root+'/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server';
const checks=[];
function check(name,ok){if(!ok)throw new Error('check_failed_'+name);checks.push({name,passed:true});}
async function denied(name,status,action){try{await action();throw new Error('missing_expected_status');}catch(e){check(name,e.status===status);}}
let fixture,transport,session,models=[],failure=null,delayed=null;const gateReleases=new Set();
const password=syntheticLocalPassword(); // Generated only in memory. Never emitted or persisted.
let baseUrlUsed=null,adapterCalls=0,listenerClosed=false,databaseClosed=false,cookieCleared=false;
function holdResponse(path){let enter,release;const entered=new Promise(r=>enter=r),released=new Promise(r=>release=r);delayed={path,enter,released};const releaseOwned=()=>{release();gateReleases.delete(releaseOwned);};gateReleases.add(releaseOwned);return {entered,release:releaseOwned};}
try{
 fixture=await startCommunityLoopback({password,lifetimeMs:120000});baseUrlUsed=fixture.baseUrl;
 transport=await createLoopbackBrowserTransport(fixture);
 const [{WebApi},{WebSessionModel},{CommunityConsentModel},{CommunityPostModel},{CommunityCommentModel},{CommunityEditModel},{CommunityDeleteModel},{CommunityReadModel},{CommunityPublicReadModel},{CommunityBlockModel,CommunityBlockReadModel},{CommunityReportModel,CommunityReportReadModel}]=await Promise.all(['web-api','web-session-model','community-consent-model','community-post-model','community-comment-model','community-edit-model','community-delete-model','community-read-model','community-public-read-model','community-block-model','community-report-model'].map(n=>import(pathToFileURL(home+'/src/'+n+'.mjs').href)));
 const actualFetch=async(input,options)=>{const response=await transport.fetch(input,options);const gate=delayed;if(gate&&new URL(input).pathname===gate.path){delayed=null;gate.enter({signal:options.signal,status:response.status});await gate.released;}return response;};
 const api=new WebApi({origin:fixture.apiOrigin,environment:'DEV',versionCode:123,fetch:actualFetch});
 await denied('initial_actual_cookie_auth401',401,()=>api.me());
 session=new WebSessionModel(api);
 const own=new CommunityReadModel(api,session),block=new CommunityBlockModel(api,session),publicRead=new CommunityPublicReadModel(api,session,{blocks:block}),post=new CommunityPostModel(api,session),comment=new CommunityCommentModel(api,session),edit=new CommunityEditModel(api,session),deletion=new CommunityDeleteModel(api,session),consent=new CommunityConsentModel(api,session,{creationModels:[post,comment]}),blocks=new CommunityBlockReadModel(api,session),report=new CommunityReportModel(api,session,{targetRead:publicRead}),reportRead=new CommunityReportReadModel(api,session);
 models=[own,block,publicRead,post,comment,edit,deletion,consent,blocks,report,reportRead];
 await session.loginPassword('community-local-a',password);check('actual_session_model_password_login',session.state.phase==='authenticated'&&session.state.accountId==='a');
 check('actual_me_server_identity',(await api.me()).accountId==='a');
 await consent.open();check('actual_synthetic_source_not_auto_accepted',consent.current.consent?.accepted===false);
 const postInput={commandId:'home_tcp_post',boardId:'free',title:'SYNTHETIC Homepage TCP',markdown:'Synthetic consumer integration only.'};
 await post.execute(postInput);check('actual_model_creation_403_without_consent',post.state.error?.code==='community_consent_required');
 await consent.open();const policy=consent.current.consent.policy;
 await consent.accept({commandId:'home_tcp_accept',...policy});await consent.open();check('actual_explicit_accept_current_provenance',consent.state.phase==='accepted'&&consent.current.consent?.accepted===true);
 check('accept_did_not_automatically_create',post.state.receipt===null);
 post.clear();await post.execute(postInput);const originalPost=post.state.receipt;check('actual_post_model_creation',post.state.phase==='created'&&!!originalPost.contentId);
 post.clear();await post.execute(postInput);check('actual_original_post_receipt_replay',JSON.stringify(post.state.receipt)===JSON.stringify(originalPost));
 const ratePost=new CommunityPostModel(api,session);models.push(ratePost);await ratePost.execute({...postInput,commandId:'home_tcp_actual_rate'});check('actual_post_rolling_rate429_verified_delay',ratePost.state.phase==='rate-limited'&&ratePost.state.error.code==='community_rate_limited'&&Number.isSafeInteger(ratePost.state.retryAt));const rateCalls=transport.calls();await ratePost.retry();check('actual_rate_model_early_retry_sends_zero',transport.calls()===rateCalls);ratePost.dispose();
 await own.openPosts('free');check('actual_own_posts_model',own.state.items.some(r=>r.id===originalPost.contentId));
 await own.openContent(originalPost.contentId);check('actual_own_current_updated_at_preserved',Number.isSafeInteger(own.state.content.updatedAt));
 await publicRead.openContent(originalPost.contentId);check('actual_public_creation_time_equal',publicRead.state.content.updatedAt===publicRead.state.content.createdAt);
 const editInput={commandId:'home_tcp_edit',contentId:originalPost.contentId,expectedRevision:1,title:'SYNTHETIC edited',markdown:'Synthetic edit.'};
 await edit.execute(editInput);check('actual_owner_edit_model',edit.state.receipt?.revision===2);
 await publicRead.refresh();const currentTime=publicRead.state.content.updatedAt;check('actual_edit_current_updated_at',publicRead.state.content.revision===2&&currentTime>=publicRead.state.content.createdAt);
 edit.clear();await edit.execute(editInput);await publicRead.refresh();check('actual_edit_original_receipt_no_time_rewrite',publicRead.state.content.updatedAt===currentTime&&edit.state.receipt.revision===2);
 edit.clear();await edit.execute({...editInput,commandId:'home_tcp_stale',expectedRevision:1});check('actual_stale_edit409',edit.state.error?.status===409);
 await comment.execute({commandId:'home_tcp_comment',postId:originalPost.contentId,markdown:'Synthetic comment'});const commentReceipt=comment.state.receipt;check('actual_comment_model_creation',comment.state.phase==='created'&&!!commentReceipt.contentId);
 await publicRead.openComments(originalPost.contentId);check('actual_public_comments_model',publicRead.state.items.some(r=>r.id===commentReceipt.contentId));
 await own.openList();check('actual_own_comments_model',own.state.items.some(r=>r.id===commentReceipt.contentId));
 await publicRead.openContent(fixture.fixturePostId);const otherAuthor=publicRead.state.content.publicAuthorId;
 await block.execute({commandId:'home_tcp_block',publicAuthorId:otherAuthor,blocked:true,expectedRevision:0});await publicRead.openContent(fixture.fixturePostId);check('actual_block_requery_target404',block.state.phase==='applied'&&publicRead.state.phase==='not-found');
 await blocks.openList();check('actual_current_block_read_model',blocks.state.items.some(r=>r.publicAuthorId===otherAuthor));
 const reportInput={commandId:'home_tcp_report',targetType:'POST',targetId:fixture.fixturePostId,expectedRevision:1,reasonCode:'SPAM'};
 await report.execute(reportInput);const originalReport=report.state.receipt;check('actual_report_blocked_target_allowed',report.state.phase==='submitted'&&!!originalReport.caseId);
 report.clear();await report.execute(reportInput);check('actual_report_original_receipt',JSON.stringify(report.state.receipt)===JSON.stringify(originalReport));
 report.clear();await report.execute({...reportInput,commandId:'home_tcp_report_open_duplicate'});check('actual_open_target_original_case',report.state.receipt.caseId===originalReport.caseId&&report.state.receipt.receiptId===originalReport.receiptId&&report.state.receipt.submittedAt===originalReport.submittedAt);
 await reportRead.openReport(originalReport.caseId);check('actual_own_report_minimal_projection',reportRead.state.report.reporterFacingStatus==='RECEIVED'&&Object.keys(reportRead.state.report).sort().join(',')==='caseId,receiptId,reporterFacingStatus,submittedAt,targetId,targetType');
 block.clear();await block.execute({commandId:'home_tcp_unblock',publicAuthorId:otherAuthor,blocked:false,expectedRevision:1});await publicRead.openContent(fixture.fixturePostId);check('actual_unblock_current_source',block.state.phase==='applied'&&publicRead.state.phase==='ready');
 await denied('actual_bad_csrf403',403,()=>api.deleteCommunityContent({commandId:'home_bad_csrf',contentId:originalPost.contentId,expectedRevision:2},{csrfToken:'invalid'}));
 for(const [name,header,status] of [['actual_wrong_version409',{'X-Community-Public-Version':'old'},409],['actual_wrong_exact_tester403',{'X-LastRail-App-Version-Code':'124'},403]]){const r=await transport.fetch(fixture.apiOrigin+'/v1/web/community/contents/'+fixture.fixturePostId,{credentials:'include',headers:{'X-Community-Public-Version':'community.public-read.v1.20261008','X-LastRail-App-Version-Code':'123',...header}});check(name,r.status===status);}
 const badOrigin=await fetch(fixture.baseUrl+'/v1/web/sessions/me',{headers:{Origin:'https://wrong.invalid'}});check('actual_wrong_origin403',badOrigin.status===403);
 await session.loginPassword('community-local-b',password);check('actual_account_transition_clears_data',session.state.accountId==='b'&&own.state.content===null&&own.state.items.length===0&&report.state.receipt===null&&consent.current.consent===null);
 await reportRead.openReport(originalReport.caseId);check('actual_other_account_report404',reportRead.state.phase==='not-found');
 await deletion.execute({commandId:'home_other_delete',contentId:originalPost.contentId,expectedRevision:2});check('actual_other_account_delete403',deletion.state.error?.status===403);
 await session.logout();check('actual_logout_confirmed',session.state.phase==='anonymous'&&session.state.logoutConfirmed===true);
 await denied('actual_server_logout_revokes_cookie401',401,()=>api.me());
 await session.loginPassword('community-local-a',password);
 const held=holdResponse('/v1/web/community/own/contents/'+originalPost.contentId);const reading=own.openContent(originalPost.contentId);const heldState=await held.entered;check('delayed_read_is_actual200',heldState.status===200);await session.logout();check('logout_aborts_delayed_read_request_signal',heldState.signal.aborted);held.release();await reading;check('real_tcp_response_delayed_after_logout_discarded',own.state.content===null&&own.state.phase==='authentication-required');
 await session.loginPassword('community-local-a',password);
 const delayedInput=postInput;post.clear();const writeGate=holdResponse('/v1/web/community/posts');const pending=post.execute(delayedInput);const receiptState=await writeGate.entered;check('delayed_original_receipt_is_actual200',receiptState.status===200);await session.logout();check('logout_aborts_delayed_receipt_request_signal',receiptState.signal.aborted);writeGate.release();await pending;check('real_original_receipt_late_after_logout_discarded',post.state.receipt===null);
 await session.loginPassword('community-local-a',password);await post.execute(delayedInput);const recovered=post.state.receipt;check('explicit_original_command_recovers_real_receipt',post.state.phase==='created'&&!!recovered.contentId);
 post.clear();await post.execute(delayedInput);check('recovered_receipt_same_original',JSON.stringify(post.state.receipt)===JSON.stringify(recovered));
 await deletion.execute({commandId:'home_delete_comment',contentId:commentReceipt.contentId,expectedRevision:1});check('actual_delete_comment_model',deletion.state.phase==='deleted');deletion.clear();
 const deletionInput={commandId:'home_delete_post',contentId:originalPost.contentId,expectedRevision:2};await deletion.execute(deletionInput);const deleteReceipt=deletion.state.receipt;check('actual_delete_post_model',deletion.state.phase==='deleted');deletion.clear();await deletion.execute(deletionInput);check('actual_delete_original_receipt',JSON.stringify(deletion.state.receipt)===JSON.stringify(deleteReceipt));
 await own.openContent(originalPost.contentId);check('actual_deleted_own_body_and_time_null',own.state.content.sanitizedBody===null&&own.state.content.updatedAt===null);
 await publicRead.openContent(originalPost.contentId);check('actual_deleted_public404',publicRead.state.phase==='not-found');
 transport.clear();cookieCleared=true;await denied('consumer_cookie_jar_explicit_clear401',401,()=>api.me());await session.restore();check('actual_restore_without_cookie_expires_model',session.state.phase==='expired');
 await session.loginPassword('community-local-a',password);await publicRead.openContent(fixture.fixturePostId);check('actual_current_before_connection_stop',publicRead.state.phase==='ready');
 await fixture.close();databaseClosed=fixture.databaseClosed();await publicRead.refresh();check('actual_connection_refused_clears_current_body',publicRead.state.phase==='error'&&publicRead.state.error.kind==='network'&&publicRead.state.content===null);
 post.clear();await post.execute({commandId:'home_disconnected_new',boardId:'free',title:'SYNTHETIC closed connection',markdown:'Cannot reach stopped listener.'});check('actual_closed_connection_unknown_command_not_success',post.state.phase==='uncertain'&&post.state.receipt===null);
}catch(e){failure=e?.message?.startsWith('check_failed_')?e.message:'integration_execution_failed';}
finally{for(const release of gateReleases)release();for(const m of models.reverse())m.dispose();session?.dispose();transport?.clear();cookieCleared=!!transport;if(fixture){await fixture.close();databaseClosed=fixture.databaseClosed();await fixture.closed;}adapterCalls=transport?.calls()??0;}
if(baseUrlUsed){try{await fetch(baseUrlUsed+'/v1/web/sessions/me',{signal:AbortSignal.timeout(2000)});}catch{listenerClosed=true;}}
const paths=['homepage/tests/community-loopback-verification.ts',...['web-api','api-http','community-contracts','community-consent-contracts','community-report-contracts','web-session-model','support-read-model','community-command-model','community-consent-model','community-post-model','community-comment-model','community-edit-model','community-delete-model','community-read-model','community-public-read-model','community-block-model','community-report-model'].map(n=>'homepage/src/'+n+'.mjs'),'Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/community-loopback-http.ts','Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/community-loopback-http-v1.md'];
const sourcePins=paths.map(path=>({path,sha256:createHash('sha256').update(readFileSync(root+'/'+path)).digest('hex')}));
const migrationPins=readdirSync(server+'/packages/db/migrations').filter(n=>/^\d+_.+\.sql$/.test(n)).sort().map(n=>({path:'packages/db/migrations/'+n,sha256:createHash('sha256').update(readFileSync(server+'/packages/db/migrations/'+n)).digest('hex')}));
const result={task:'H-04@community-v1',unit:'consumer real loopback TCP integration',contractVersion:'community.loopback-http.v1.20261008',executedAt:new Date().toISOString(),checks,passed:checks.length,failure,actualTcp:true,adapterCalls,baseUrlUsed,listenerClosed,databaseClosed,cookieJarCleared:cookieCleared,modelsDisposed:models.length,sessionDisposed:session?.state.phase==='disposed',sourcePins,migrationPins,actualBrowser:false,actualTLS:false,actualRemoteD1:false,operatingWrites:false,limitations:['Actual game app and RAM sql.js/D1 source fixtures; not operating accounts/terms approval','Node adapter forwards actual server cookie/Origin; no browser Secure/HttpOnly/SameSite/CORS validation','Delayed-read/committed-receipt probes gate actual TCP responses in consumer transport, not server latency','RAM-only DB discarded at close; fresh restart is not persistence recovery','No final View/bootstrap/art connection, moderation/purge/retirement validation']};
writeFileSync(root+'/QA/Homepage/community-loopback-20261008.json',JSON.stringify(result,null,2));
console.log('Homepage real TCP consumer checks: '+checks.length+' passed; cleanup listener='+listenerClosed+' RAM_DB='+databaseClosed+' cookie_jar='+cookieCleared+'.');
if(failure||!listenerClosed||!databaseClosed||!cookieCleared)throw new Error(failure??'cleanup_failed');
