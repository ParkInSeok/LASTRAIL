CREATE TABLE ordinary_native_geometry_definitions (
 definition_id TEXT NOT NULL, version TEXT NOT NULL, definition_hash TEXT NOT NULL,
 definition_json TEXT NOT NULL CHECK(json_valid(definition_json)),
 PRIMARY KEY(definition_id,version)
);
CREATE TRIGGER ordinary_native_geometry_no_update BEFORE UPDATE ON ordinary_native_geometry_definitions
BEGIN SELECT RAISE(ABORT,'ordinary_geometry_immutable'); END;
CREATE TRIGGER ordinary_native_geometry_no_delete BEFORE DELETE ON ordinary_native_geometry_definitions
BEGIN SELECT RAISE(ABORT,'ordinary_geometry_immutable'); END;
