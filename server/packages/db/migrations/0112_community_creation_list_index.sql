-- Stable keyset lookup shared by approved own lists; no data/migration activation.
CREATE INDEX idx_community_creation_own_list ON community_content_creation(account_id,kind,created_at,content_id COLLATE BINARY);
