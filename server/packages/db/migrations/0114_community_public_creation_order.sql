-- Creation order/owner/type are immutable; edits and visibility changes belong to current content.
-- Retirement/deletion remains a separate explicit owner; no new operating seed or purge policy.
CREATE TRIGGER community_creation_no_update BEFORE UPDATE ON community_content_creation BEGIN SELECT RAISE(ABORT,'community_creation_immutable'); END;
CREATE INDEX idx_community_creation_public_order ON community_content_creation(kind,created_at,content_id COLLATE BINARY);