import {describe,it,expect} from "vitest";
import {newNoticeDocument,noticeDocumentSchema,noticeCommandSchema,publicationStatus} from "./noticeContract";
const document={...newNoticeDocument(),title:"공지",summary:"안내",localizedBody:{ko:"내용",en:"Body"}};
describe("notice boundary",()=>{
 it("preserves all locales and target conditions",()=>expect(noticeDocumentSchema.parse({...document,channel:"beta",minVersionCode:5}).localizedBody).toEqual({ko:"내용",en:"Body"}));
 it.each([{title:"<b>hello</b>"},{localizedBody:{}},{localizedBody:{ko:""}},{endsAt:0},{startsAt:null},{priority:1001},{minVersionCode:5,maxVersionCode:4},{imageAssetId:"image"},{popupMode:"AUTO"},{unexpected:true}])("rejects invalid document %j",change=>expect(noticeDocumentSchema.safeParse({...document,...change}).success).toBe(false));
 it("requires reason and forbids document on publish",()=>{
  const command={environment:"DEV",expectedRevision:1,reason:"검토",command:"PUBLISH"};
  expect(noticeCommandSchema.safeParse(command).success).toBe(true);
  expect(noticeCommandSchema.safeParse({...command,reason:" "}).success).toBe(false);
  expect(noticeCommandSchema.safeParse({...command,document}).success).toBe(false);
 });
 it("derives publication separately from draft status",()=>{
  const published={noticeId:"n",revision:1,status:"PUBLISHED" as const,document:{...document,startsAt:100,endsAt:200},updatedAt:0};
  expect(publicationStatus(null,100)).toBe("게시본 없음");expect(publicationStatus(published,50)).toBe("예약");expect(publicationStatus(published,100)).toBe("게시 중");expect(publicationStatus(published,200)).toBe("게시 종료");
 });
});
