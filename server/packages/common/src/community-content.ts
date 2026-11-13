import { z } from 'zod';

export const COMMUNITY_CONTENT_VERSION = 'community.content-state.v1' as const;
export class CommunityContentError extends Error {}
const id = z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
const title = z.string().trim().min(1).refine(value => Array.from(value).length <= 100);
// Rendering/sanitizing is owned by the server input adapter, not this state model.
// A nonempty string here proves no sanitizer support: routes must supply their verified output.
const renderedBody = z.string().min(1).max(100000);
const replacementSchema = z.strictObject({ title: title.nullable(), sanitizedBody: renderedBody });
const stateSchema = z.strictObject({
  kind: z.enum(['POST', 'COMMENT']), id, parentId: id.nullable(), accountId: id,
  publicAuthorId: id, visibility: z.enum(['REVIEW_PENDING', 'VISIBLE', 'HIDDEN', 'AUTHOR_DELETED']),
  commentLocked: z.boolean(), revision: z.number().int().positive().safe(),
  title: title.nullable(), sanitizedBody: renderedBody.nullable(),
}).superRefine((state, ctx) => {
  if ((state.kind === 'POST') !== (state.parentId === null) ||
      state.kind === 'COMMENT' && (state.title !== null || state.commentLocked) ||
      (state.visibility === 'AUTHOR_DELETED'
        ? state.title !== null || state.sanitizedBody !== null
        : state.sanitizedBody === null || state.kind === 'POST' && state.title === null))
    ctx.addIssue({ code: 'custom', message: 'invalid community content state' });
});
export type CommunityContentSnapshot = z.infer<typeof stateSchema>;
type Moderation = 'HIDE' | 'RESTORE' | 'LOCK_COMMENTS' | 'UNLOCK_COMMENTS';
type Viewer = { accountId: string | null; blockedAuthors: readonly string[]; parent: CommunityContentSnapshot | null };

/** Owns content state only. HTTP auth, renderer, persistence/CAS, audit and retention are external boundaries. */
export class CommunityContent {
  private constructor(private state: CommunityContentSnapshot) {}

  static restore(raw: unknown): CommunityContent {
    const parsed = stateSchema.safeParse(raw);
    if (!parsed.success) throw new CommunityContentError('community_content_invalid');
    return new CommunityContent(parsed.data);
  }

  snapshot(): CommunityContentSnapshot { return { ...this.state }; }

  read(viewer: Viewer) {
    const state = this.state;
    if (!this.parentIsPublic(viewer.parent)) return null;
    if (viewer.blockedAuthors.includes(state.publicAuthorId)) return null;
    if (state.visibility !== 'VISIBLE' && viewer.accountId !== state.accountId) return null;
    const visible = state.visibility === 'VISIBLE';
    return { contractVersion: COMMUNITY_CONTENT_VERSION, kind: state.kind, id: state.id,
      parentId: state.parentId, publicAuthorId: state.publicAuthorId, revision: state.revision,
      visibility: state.visibility, commentLocked: state.commentLocked,
      title: visible ? state.title : null, sanitizedBody: visible ? state.sanitizedBody : null };
  }

  edit(accountId: string, expectedRevision: number, replacement: { title: string | null; sanitizedBody: string }): void {
    this.requireOwner(accountId); this.requireRevision(expectedRevision); this.requireNotDeleted();
    const parsed = replacementSchema.safeParse(replacement);
    if (!parsed.success) throw new CommunityContentError('community_content_invalid');
    this.replace({ ...this.state, ...parsed.data, revision: this.state.revision + 1 });
  }

  delete(accountId: string, expectedRevision: number): void {
    this.requireOwner(accountId); this.requireRevision(expectedRevision); this.requireNotDeleted();
    this.replace({ ...this.state, visibility: 'AUTHOR_DELETED', title: null, sanitizedBody: null,
      revision: this.state.revision + 1 });
  }

  moderate(actor: { principalId: string; canModerate: boolean }, expectedRevision: number, action: Moderation,
    parent: CommunityContentSnapshot | null = null): void {
    if (!id.safeParse(actor.principalId).success || actor.canModerate !== true)
      throw new CommunityContentError('community_forbidden');
    this.requireRevision(expectedRevision); this.requireNotDeleted();
    const next = { ...this.state, revision: this.state.revision + 1 };
    if (action === 'HIDE') next.visibility = 'HIDDEN';
    else if (action === 'RESTORE') {
      if (!this.parentIsPublic(parent)) throw new CommunityContentError('community_parent_unavailable');
      next.visibility = 'VISIBLE';
    }
    else if ((action === 'LOCK_COMMENTS' || action === 'UNLOCK_COMMENTS') && next.kind === 'POST')
      next.commentLocked = action === 'LOCK_COMMENTS';
    else throw new CommunityContentError('community_action_invalid');
    this.replace(next);
  }

  requireCommentCreation(): void {
    if (this.state.kind !== 'POST' || this.state.visibility !== 'VISIBLE')
      throw new CommunityContentError('community_parent_unavailable');
    if (this.state.commentLocked) throw new CommunityContentError('community_comments_locked');
  }

  private replace(raw: unknown): void { this.state = CommunityContent.restore(raw).snapshot(); }
  private parentIsPublic(parent: CommunityContentSnapshot | null): boolean {
    return this.state.kind === 'POST' || !!parent && parent.kind === 'POST' &&
      parent.id === this.state.parentId && parent.visibility === 'VISIBLE';
  }
  private requireOwner(accountId: string): void {
    if (!accountId || accountId !== this.state.accountId) throw new CommunityContentError('community_forbidden');
  }
  private requireRevision(revision: number): void {
    if (!Number.isSafeInteger(revision) || revision !== this.state.revision)
      throw new CommunityContentError('community_revision_conflict');
  }
  private requireNotDeleted(): void {
    if (this.state.visibility === 'AUTHOR_DELETED') throw new CommunityContentError('community_deleted');
  }
}
