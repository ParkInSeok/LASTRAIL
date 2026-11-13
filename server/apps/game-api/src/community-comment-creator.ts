// Compatibility entry for supplied COMMENT consumers. One creation owner now handles POST and COMMENT.
// Remove this alias after existing imports migrate to community-content-creator; no duplicate state or rules.
export {CommunityContentCreator,CommunityContentCreator as CommunityCommentCreator,CommunityRateError,
 communityCommentCreateVersion,communityCommentCreateRequestSchema,communityCommentCreateReceiptSchema} from './community-content-creator';