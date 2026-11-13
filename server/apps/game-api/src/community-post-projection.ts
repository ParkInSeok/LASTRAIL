import {canonicalJson,sha256Hex} from '@lastrail/common';
import {CommunityStoreError} from './community-content-store';
import {communityPostCreateReceiptSchema,communityPostCreateRequestSchema,communityPostDetails} from './community-post-contract';
/** Exact persisted descriptor/creation binding shared by own and public projections. */
export async function readCommunityPostDetails(row:{response_json:string;details_json:string;content_id:string;created_at:number}){
 try{const receipt=communityPostCreateReceiptSchema.parse(JSON.parse(row.response_json)),rawDetails=JSON.parse(row.details_json);
  const details=communityPostDetails(communityPostCreateRequestSchema.parse({...rawDetails,commandId:'projection',title:'Projection',markdown:'Projection'}));
  if(receipt.contentId!==row.content_id||receipt.createdAt!==row.created_at||receipt.boardId!==details.boardId||canonicalJson(details)!==row.details_json||await sha256Hex(row.details_json)!==receipt.detailsHash)throw new Error();return details;
 }catch{throw new CommunityStoreError(503,'community_storage_invalid');}
}