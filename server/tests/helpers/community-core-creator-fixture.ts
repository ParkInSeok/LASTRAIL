import {CommunityContentCreator as ActualCreator} from '../../apps/game-api/src/community-content-creator';
/** Existing domain/render/rate fixtures only: explicit isolated eligibility port.
 * Never app-imported. WEB tests instead use synthetic published documents plus the actual consent service.
 * This bypass is not evidence of current consent or release eligibility. */
class CoreFixtureCreator extends ActualCreator{
 constructor(...args:ConstructorParameters<typeof ActualCreator>){args[5]??=async()=>({guards:[],assertCurrent:async()=>{}});super(...args);}
}
export {CoreFixtureCreator as CommunityContentCreator,CoreFixtureCreator as CommunityCommentCreator};
export * from '../../apps/game-api/src/community-content-creator';