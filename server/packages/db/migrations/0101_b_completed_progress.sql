-- No history rewrite, counter replay, reward or admission changes.
ALTER TABLE objective_events ADD COLUMN b_completed_nodes INTEGER CHECK(b_completed_nodes BETWEEN 0 AND 12);
ALTER TABLE objective_events ADD COLUMN b_progress_policy_version TEXT;
DROP TRIGGER b_objective_version_guard;
CREATE TRIGGER b_objective_version_guard BEFORE INSERT ON objective_events
WHEN
 (NEW.b_completed_waves IS NOT NULL AND (NEW.mode<>'STORY_B_12' OR NEW.binding_version IS NOT 'operation.b-story12-binding.v1.20261004' OR NEW.kind<>'FINISH' OR NEW.waves<>0))
 OR (NEW.mode='STORY_B_12' AND NEW.b_completed_waves IS NULL)
 OR (NEW.b_progress_policy_version IS NOT NULL AND (NEW.mode<>'STORY_B_12' OR NEW.b_progress_policy_version IS NOT 'operation.b-completed-progress.v1.20261004' OR NEW.b_completed_nodes IS NULL))
 OR (NEW.b_completed_nodes IS NOT NULL AND NEW.b_progress_policy_version IS NULL)
 OR (NEW.status='COMPLETED_PROGRESS_ELIGIBLE' AND (NEW.b_progress_policy_version IS NOT 'operation.b-completed-progress.v1.20261004' OR NEW.ends<>0))
 OR (NEW.b_completed_waves IS NOT NULL AND NEW.status NOT IN ('ELIGIBLE','COMPLETED_PROGRESS_ELIGIBLE') AND (NEW.b_completed_waves<>0 OR COALESCE(NEW.b_completed_nodes,0)<>0))
BEGIN SELECT RAISE(ABORT,'b objective version conflict'); END;