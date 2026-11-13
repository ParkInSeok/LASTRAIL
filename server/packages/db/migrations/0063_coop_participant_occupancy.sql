-- Occupancy account is distinct from immutable run ownership.
-- Old host contexts lacking activeAccountIds retain both original participants.
DROP VIEW active_combat_runs;
CREATE VIEW active_combat_runs AS
SELECT r.id,CAST(member.value AS TEXT) AS account_id,r.account_id AS owner_account_id,
 r.preview_id,r.mutation_id,r.balance_config_version,r.status,r.started_at,r.finished_at,r.build_id
FROM runs r LEFT JOIN combat_host_report_states h ON h.run_id=r.id
JOIN json_each(CASE WHEN h.run_id IS NULL THEN json_array(r.account_id)
 ELSE COALESCE(json_extract(h.context_json,'$.activeAccountIds'),
  (SELECT json_group_array(json_extract(p.value,'$.accountId')) FROM json_each(h.context_json,'$.participants') p)) END) member
WHERE r.status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')
AND NOT EXISTS(SELECT 1 FROM combat_review_releases x JOIN combat_result_reviews v ON v.run_id=x.run_id WHERE x.run_id=r.id);
