-- Authenticated source bytes only. No keys, approvals, runtime or geometry seed.
CREATE TABLE b_physical_publications (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 publication_id TEXT NOT NULL,
 key_id TEXT NOT NULL,
 body_json TEXT NOT NULL,
 body_hash TEXT NOT NULL,
 signature TEXT NOT NULL,
 PRIMARY KEY(environment,publication_id)
);
CREATE TABLE b_physical_publication_revocations (
 environment TEXT NOT NULL,
 publication_id TEXT NOT NULL,
 reason TEXT NOT NULL,
 PRIMARY KEY(environment,publication_id),
 FOREIGN KEY(environment,publication_id) REFERENCES b_physical_publications(environment,publication_id)
);
CREATE TRIGGER b_physical_publications_no_update BEFORE UPDATE ON b_physical_publications
 BEGIN SELECT RAISE(ABORT,'physical_publication_immutable'); END;
CREATE TRIGGER b_physical_publications_no_delete BEFORE DELETE ON b_physical_publications
 BEGIN SELECT RAISE(ABORT,'physical_publication_immutable'); END;
CREATE TRIGGER b_physical_revocations_no_update BEFORE UPDATE ON b_physical_publication_revocations
 BEGIN SELECT RAISE(ABORT,'physical_revocation_immutable'); END;
CREATE TRIGGER b_physical_revocations_no_delete BEFORE DELETE ON b_physical_publication_revocations
 BEGIN SELECT RAISE(ABORT,'physical_revocation_immutable'); END;
