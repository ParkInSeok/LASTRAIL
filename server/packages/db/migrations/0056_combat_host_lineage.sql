-- Preserve only observed assignments; unavailable prior generations are not reconstructed.
CREATE TABLE combat_host_lineage (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 host_generation INTEGER NOT NULL CHECK(host_generation BETWEEN 1 AND 9007199254740991),
 previous_generation INTEGER,
 provenance TEXT NOT NULL CHECK(provenance IN ('INITIAL','RESUME','MIGRATION_CURRENT')),
 context_json TEXT NOT NULL CHECK(json_valid(context_json)),
 PRIMARY KEY(run_id,host_generation),
 FOREIGN KEY(run_id,previous_generation) REFERENCES combat_host_lineage(run_id,host_generation),
 CHECK(previous_generation IS NULL OR previous_generation=host_generation-1)
);
INSERT INTO combat_host_lineage(run_id,host_generation,previous_generation,provenance,context_json)
 SELECT run_id,json_extract(context_json,'$.hostGeneration'),NULL,'MIGRATION_CURRENT',context_json
 FROM combat_host_report_states;
CREATE TRIGGER combat_host_lineage_no_update BEFORE UPDATE ON combat_host_lineage BEGIN SELECT RAISE(ABORT,'immutable_host_lineage'); END;
CREATE TRIGGER combat_host_lineage_no_delete BEFORE DELETE ON combat_host_lineage BEGIN SELECT RAISE(ABORT,'immutable_host_lineage'); END;
