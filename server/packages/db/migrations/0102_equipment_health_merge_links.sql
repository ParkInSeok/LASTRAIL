-- Explicit new internal merge/HP link contract. Old placement receipt guards remain unchanged.
CREATE TABLE run_equipment_health_merge_links (
 run_id TEXT NOT NULL,command_id TEXT NOT NULL,
 request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 response_hash TEXT NOT NULL CHECK(length(response_hash)=64),
 PRIMARY KEY(run_id,command_id),
 FOREIGN KEY(run_id,command_id) REFERENCES run_equipment_receipts(run_id,command_id)
);
CREATE TRIGGER equipment_health_merge_link_no_update BEFORE UPDATE ON run_equipment_health_merge_links
BEGIN SELECT RAISE(ABORT,'equipment_health_merge_link_immutable'); END;
CREATE TRIGGER equipment_health_merge_link_no_delete BEFORE DELETE ON run_equipment_health_merge_links
BEGIN SELECT RAISE(ABORT,'equipment_health_merge_link_immutable'); END;
CREATE TRIGGER equipment_health_merge_link_insert_guard BEFORE INSERT ON run_equipment_health_merge_links
BEGIN
 SELECT CASE WHEN json_extract(NEW.response_json,'$.version') IS NOT 'equipment.health-merge.v1.20261005'
  OR json_extract(NEW.response_json,'$.commandId') IS NOT NEW.command_id
  OR NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=NEW.run_id AND q.command_id=NEW.command_id
   AND q.command_kind='MERGE_EQUIPMENT' AND json(q.response_json)=json_extract(NEW.response_json,'$.merge'))
  OR json_type(NEW.response_json,'$.health') IS NOT 'array'
  OR json_array_length(NEW.response_json,'$.health') NOT BETWEEN 1 AND 2
  OR json_type(NEW.response_json,'$.targets') IS NOT 'array'
  OR json_array_length(NEW.response_json,'$.targets') IS NOT json_array_length(NEW.response_json,'$.health')
  OR EXISTS(SELECT 1 FROM json_each(NEW.response_json,'$.health') h WHERE NOT EXISTS(
   SELECT 1 FROM combat_damage_receipts r WHERE r.run_id=NEW.run_id
    AND r.event_id=json_extract(h.value,'$.eventId') AND json(r.response_json)=json(h.value)))
  THEN RAISE(ABORT,'equipment_health_merge_link_invalid') END;
END;