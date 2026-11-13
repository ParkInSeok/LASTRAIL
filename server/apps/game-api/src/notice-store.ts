import {z} from 'zod';
import {NoticeError,noticeDocumentSchema,noticeIdSchema,noticeStateSchema,type StoredNotice,type NoticeQuery} from '../../../packages/common/src/notice-policy';

type Row={notice_id:string;revision:number;status:string;document_json:string;updated_at:number};
const rowSchema=z.strictObject({notice_id:noticeIdSchema,revision:z.number().int().positive().safe(),
  status:noticeStateSchema,document_json:z.string(),updated_at:z.number().int().nonnegative().safe()});
function decode(row:Row):StoredNotice {
  try{
    const r=rowSchema.parse(row);
    return {noticeId:r.notice_id,revision:r.revision,status:r.status,document:noticeDocumentSchema.parse(JSON.parse(r.document_json)),updatedAt:r.updated_at};
  }catch{throw new NoticeError(503,'notice_storage_invalid');}
}
const select='SELECT r.notice_id,r.revision,r.status,r.document_json,r.updated_at FROM notice_heads h JOIN notice_revisions r ON r.environment=h.environment AND r.notice_id=h.notice_id AND r.revision=';
export class NoticeStore {
  constructor(private readonly db:D1Database,private readonly environment:string){}
  async read(id:string,published=false){
    const row=await this.db.prepare(select+(published?'h.published_revision':'h.revision')+' WHERE h.environment=? AND h.notice_id=?').bind(this.environment,id).first<Row>();
    return row?decode(row):null;
  }
  async adminList(after:string,limit:number){
    const rows=await this.db.prepare(select+'h.revision WHERE h.environment=? AND h.notice_id>? ORDER BY h.notice_id LIMIT ?')
      .bind(this.environment,after,limit).all<Row>();
    return rows.results.map(decode);
  }
  async publicList(q:NoticeQuery,now:number){
    const rows=await this.db.prepare(select+`h.published_revision WHERE h.environment=? AND h.notice_id>?
      AND r.status IN ('PUBLISHED','SCHEDULED')
      AND json_extract(r.document_json,'$.visibility')='PUBLIC'
      AND json_extract(r.document_json,'$.surfaces') IN ('WEB','BOTH')
      AND json_extract(r.document_json,'$.startsAt')<=?
      AND (json_extract(r.document_json,'$.endsAt') IS NULL OR json_extract(r.document_json,'$.endsAt')>?)
      AND (json_extract(r.document_json,'$.channel') IS NULL OR json_extract(r.document_json,'$.channel')=?)
      AND (json_extract(r.document_json,'$.platform') IS NULL OR json_extract(r.document_json,'$.platform')=?)
      AND (json_extract(r.document_json,'$.minVersionCode') IS NULL OR json_extract(r.document_json,'$.minVersionCode')<=?)
      AND (json_extract(r.document_json,'$.maxVersionCode') IS NULL OR json_extract(r.document_json,'$.maxVersionCode')>=?)
      AND json_type(r.document_json,?)='text'
      ORDER BY h.notice_id LIMIT ?`)
      .bind(this.environment,q.after,now,now,q.channel,q.platform,q.versionCode,q.versionCode,
        '$.localizedBody."'+q.locale+'"',q.limit+1).all<Row>();
    return rows.results.map(decode);
  }
}
