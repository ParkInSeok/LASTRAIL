import {it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {RewardTrackingPreview} from './RewardTrackingPreview';
import {RewardReviewOwner} from './rewardTracking';
it('disconnected view separates processing completion from payment and receipt with no actions',()=>{
 const owner=new RewardReviewOwner();const generation=owner.beginContext('operator/DEV');
 owner.accept({environment:'DEV',batchReference:'batch',sourceVersion:'v1',revision:'r1',policyVersion:'p1',targetSnapshot:null,targetCount:1,targetCountKind:'ESTIMATED',totalQuantity:null,aggregateScope:'CURRENT_PAGE',aggregatedAt:'2026-10-20T00:00:00Z',rights:[{reference:'<unsafe>',kind:'ALL_PLAYER',processing:'EXCLUDED',receipt:'UNKNOWN'}]},generation,null);
 const html=renderToStaticMarkup(<RewardTrackingPreview state={owner.snapshot()}/>);
 for(const text of ['연결 전','실행 불가','현재 페이지','추정','정책상제외','미확인','원거래·원키','&lt;unsafe&gt;','표시 범위 처리 완료: 예','표시 범위 모든 대상 지급 성공: 아니오'])expect(html).toContain(text);
 expect(html).not.toContain('<button');expect(html).not.toContain('<form');expect(html).not.toContain('<unsafe>');expect(html).not.toContain('지급했습니다');
});
it('requires refreshed producer input after a local edit before reviewing again',()=>{
 const owner=new RewardReviewOwner();const g=owner.beginContext('scope');owner.accept({environment:'DEV',batchReference:'batch',sourceVersion:'s1',revision:'r1',policyVersion:'p1',targetSnapshot:'t1',targetCount:0,targetCountKind:'CONFIRMED',totalQuantity:0,aggregateScope:'ALL_TARGETS',aggregatedAt:'now',rights:[]},g,null);owner.completeReview();owner.edit();expect(()=>owner.completeReview()).toThrow();
});
