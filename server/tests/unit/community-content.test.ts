import { describe, expect, it } from 'vitest';
import { CommunityContent, CommunityContentError } from '../../packages/common/src/community-content';

const post = () => CommunityContent.restore({ kind: 'POST', id: 'post-1', parentId: null,
  accountId: 'owner', publicAuthorId: 'author-1', visibility: 'VISIBLE', commentLocked: false,
  revision: 1, title: 'Title', sanitizedBody: 'Safe rendered body' });
const comment = () => CommunityContent.restore({ kind: 'COMMENT', id: 'comment-1', parentId: 'post-1',
  accountId: 'reader', publicAuthorId: 'author-2', visibility: 'VISIBLE', commentLocked: false,
  revision: 1, title: null, sanitizedBody: 'Safe comment' });
const moderator = { principalId: 'moderator', canModerate: true };

describe('approved community content state and access boundary', () => {
  it('projects public content without private owner IDs or mutable state', () => {
    const value = post(), dto = value.read({ accountId: null, blockedAuthors: [], parent: null });
    expect(dto).toMatchObject({ id: 'post-1', publicAuthorId: 'author-1', sanitizedBody: 'Safe rendered body' });
    expect(JSON.stringify(dto)).not.toContain('owner');
    const copy = value.snapshot(); copy.sanitizedBody = 'Changed copy';
    expect(value.snapshot().sanitizedBody).toBe('Safe rendered body');
  });
  it('rejects another member edit and stale revision with unchanged content', () => {
    const value = post();
    expect(() => value.edit('reader', 1, { title: 'Other', sanitizedBody: 'Other' })).toThrow('community_forbidden');
    expect(() => value.edit('owner', 0, { title: 'Other', sanitizedBody: 'Other' })).toThrow('community_revision_conflict');
    expect(value.snapshot().revision).toBe(1);
  });
  it.each(['HIDDEN', 'REVIEW_PENDING'] as const)('keeps %s content nonpublic after its owner edits', visibility => {
    const value = CommunityContent.restore({ ...post().snapshot(), visibility });
    value.edit('owner', 1, { title: 'New', sanitizedBody: 'Changed' });
    expect(value.snapshot()).toMatchObject({ revision: 2, visibility });
    expect(value.read({ accountId: 'reader', blockedAuthors: [], parent: null })).toBeNull();
    expect(value.read({ accountId: 'owner', blockedAuthors: [], parent: null })).toMatchObject({ visibility });
  });
  it('erases author-deleted content and prevents owner or operator restoration', () => {
    const value = post(); value.delete('owner', 1);
    expect(value.snapshot()).toMatchObject({ visibility: 'AUTHOR_DELETED', title: null, sanitizedBody: null, revision: 2 });
    expect(value.read({ accountId: 'owner', blockedAuthors: [], parent: null })).toMatchObject({ sanitizedBody: null });
    expect(() => value.edit('owner', 2, { title: 'Resurrect', sanitizedBody: 'Body' })).toThrow('community_deleted');
    expect(() => value.moderate(moderator, 2, 'RESTORE')).toThrow('community_deleted');
  });
  it('hides direct-ID comments when their parent is not public, including from the comment owner', () => {
    const value = comment(), parent = post(); parent.moderate(moderator, 1, 'HIDE');
    expect(value.read({ accountId: 'reader', blockedAuthors: [], parent: parent.snapshot() })).toBeNull();
    expect(value.read({ accountId: null, blockedAuthors: [], parent: null })).toBeNull();
  });
  it('requires the exact parent and preserves independent comment locks', () => {
    const parent = post(); parent.moderate(moderator, 1, 'LOCK_COMMENTS');
    expect(parent.snapshot()).toMatchObject({ visibility: 'VISIBLE', commentLocked: true });
    expect(() => parent.requireCommentCreation()).toThrow('community_comments_locked');
    const value = comment();
    expect(value.read({ accountId: null, blockedAuthors: [], parent: { ...parent.snapshot(), id: 'other' } })).toBeNull();
    value.delete('reader', 1); // Lock does not revoke the member's deletion right.
    expect(value.snapshot().visibility).toBe('AUTHOR_DELETED');
    parent.moderate(moderator, 2, 'UNLOCK_COMMENTS');
    expect(() => parent.requireCommentCreation()).not.toThrow();
  });
  it('filters blocked authors only for the requesting viewer', () => {
    const value = post();
    expect(value.read({ accountId: 'reader', blockedAuthors: ['author-1'], parent: null })).toBeNull();
    expect(value.read({ accountId: null, blockedAuthors: [], parent: null })).not.toBeNull();
  });
  it('requires explicit moderation authority and does not let a comment lock masquerade as hiding', () => {
    const value = post();
    expect(() => value.moderate({ principalId: 'reader', canModerate: false }, 1, 'HIDE')).toThrow('community_forbidden');
    expect(() => comment().moderate(moderator, 1, 'LOCK_COMMENTS')).toThrow('community_action_invalid');
    expect(value.snapshot().revision).toBe(1);
  });
  it('requires a public matching parent before restoring a hidden comment', () => {
    const value = comment(); value.moderate(moderator, 1, 'HIDE');
    expect(() => value.moderate(moderator, 2, 'RESTORE')).toThrow('community_parent_unavailable');
    const parent = post(); parent.moderate(moderator, 1, 'HIDE');
    expect(() => value.moderate(moderator, 2, 'RESTORE', parent.snapshot())).toThrow('community_parent_unavailable');
    expect(value.snapshot()).toMatchObject({ visibility: 'HIDDEN', revision: 2 });
    value.moderate(moderator, 2, 'RESTORE', post().snapshot());
    expect(value.snapshot()).toMatchObject({ visibility: 'VISIBLE', revision: 3 });
  });
  it.each([
    { ...post().snapshot(), revision: 0 },
    { ...post().snapshot(), kind: 'COMMENT', parentId: null },
    { ...post().snapshot(), visibility: 'AUTHOR_DELETED' },
    { ...post().snapshot(), accountId: '' },
  ])('rejects corrupt persisted boundary data', raw => {
    expect(() => CommunityContent.restore(raw)).toThrow(CommunityContentError);
  });
  it('checks invalid replacement before changing its revision', () => {
    const value = post();
    expect(() => value.edit('owner', 1, { title: '', sanitizedBody: '' })).toThrow('community_content_invalid');
    expect(value.snapshot()).toMatchObject({ revision: 1, title: 'Title' });
  });
  it('rejects additional fields that attempt to change ownership or publish hidden content', () => {
    const value = CommunityContent.restore({ ...post().snapshot(), visibility: 'HIDDEN' });
    const input = { title: 'Title', sanitizedBody: 'Body', accountId: 'reader', visibility: 'VISIBLE' };
    expect(() => value.edit('owner', 1, input)).toThrow('community_content_invalid');
    expect(value.snapshot()).toMatchObject({ accountId: 'owner', visibility: 'HIDDEN', revision: 1 });
  });
});
