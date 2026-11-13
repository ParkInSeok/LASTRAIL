import {afterEach,expect,it,vi} from "vitest";
import {adminApi} from "../../api/client";
import {createEventPreviewModel} from "./eventPreviewReadPort";
afterEach(()=>vi.restoreAllMocks());
it("binds the existing typed API preview once and keeps scope identity explicit",async()=>{const api=vi.spyOn(adminApi,"eventDefinitionPreview").mockRejectedValue(new Error("synthetic transport")),model=createEventPreviewModel({principalId:"synthetic",environment:"DEV",authorizationGeneration:"1"});model.setDraft({environment:"DEV",roundId:"round",revision:1,policyVersion:null,title:"Event",theme:"Theme",schedule:{publishAt:0,startAt:1,endAt:1209600001,claimUntil:null,giftOpenAt:null},rewards:[{rightId:"r",kind:"PARTICIPATION",condition:"original",producerId:null,threshold:null,itemId:null,quantity:1,perAccountCount:1,policyVersion:null}]});await expect(model.preview()).rejects.toThrow("synthetic transport");expect(api).toHaveBeenCalledTimes(1);model.dispose();});
