-- Preserve immutable v1 receipts; permit opt-in static application payloads.
DROP TRIGGER equipment_health_link_insert_guard;
CREATE TRIGGER equipment_health_link_insert_guard BEFORE INSERT ON run_equipment_health_links
BEGIN
  SELECT CASE WHEN COALESCE(json_extract(NEW.response_json,'$.version'),'') NOT IN
      ('equipment.health-placement.v1','equipment.health-placement.v2.20261019')
    OR json_extract(NEW.response_json,'$.commandId') IS NOT NEW.command_id
    OR NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=NEW.run_id AND q.command_id=NEW.command_id
      AND q.command_kind='EQUIP_OR_MOVE' AND json(q.response_json)=json_extract(NEW.response_json,'$.placement'))
    OR COALESCE(json_array_length(NEW.response_json,'$.health'),0) NOT BETWEEN 1 AND 2
    OR EXISTS(SELECT 1 FROM json_each(NEW.response_json,'$.health') h WHERE NOT EXISTS(
      SELECT 1 FROM combat_damage_receipts r WHERE r.run_id=NEW.run_id
        AND r.event_id=json_extract(h.value,'$.eventId') AND json(r.response_json)=json(h.value)))
    THEN RAISE(ABORT,'equipment_health_link_invalid') END;
  SELECT CASE WHEN json_extract(NEW.response_json,'$.version')='equipment.health-placement.v2.20261019' AND (
    json_extract(NEW.response_json,'$.application.applicationId') IS NOT NEW.command_id
    OR json_extract(NEW.response_json,'$.application.status') IS NOT 'SERVER_COMMITTED_CLIENT_APPLY_REQUIRED'
    OR json_extract(NEW.response_json,'$.application.inventoryRevision') IS NOT json_extract(NEW.response_json,'$.placement.inventoryRevision')
    OR json_extract(NEW.response_json,'$.application.healthRevision') IS NOT json_extract(NEW.response_json,'$.health[#-1].revision')
    OR json_extract(NEW.response_json,'$.application.configuration.sourceVersion') IS NOT json_extract(NEW.response_json,'$.statVersion')
    OR COALESCE(length(json_extract(NEW.response_json,'$.application.configuration.sourceHash')),0)<>64
    OR COALESCE(json_array_length(NEW.response_json,'$.application.configuration.actors'),0) NOT BETWEEN 1 AND 2
    OR json_array_length(NEW.response_json,'$.application.configuration.actors') IS NOT json_array_length(NEW.response_json,'$.health')
  ) THEN RAISE(ABORT,'equipment_health_application_invalid') END;
END;
