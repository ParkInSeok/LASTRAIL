-- LOCAL trial transport metadata only. Never install on DEV/PRD.
CREATE TABLE world_boss_authority_http_commands (
 attempt_id TEXT NOT NULL REFERENCES world_boss_rail_geometry_heads(attempt_id),
 account_id TEXT NOT NULL REFERENCES accounts(id),
 command_id TEXT NOT NULL,
 transport_version TEXT NOT NULL CHECK(transport_version='world-boss.authority-local-http.v1.20261002'),
 request_owner_revision INTEGER NOT NULL CHECK(request_owner_revision>0),
 command_json TEXT NOT NULL CHECK(json_valid(command_json)),
 PRIMARY KEY(attempt_id,account_id,command_id)
);
CREATE TABLE world_boss_authority_http_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER world_boss_authority_http_immutable_update BEFORE UPDATE ON world_boss_authority_http_commands BEGIN SELECT RAISE(ABORT,'authority_http_immutable'); END;
CREATE TRIGGER world_boss_authority_http_immutable_delete BEFORE DELETE ON world_boss_authority_http_commands BEGIN SELECT RAISE(ABORT,'authority_http_immutable'); END;