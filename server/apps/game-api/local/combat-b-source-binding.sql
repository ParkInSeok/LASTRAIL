-- LOCAL isolated supplier extension only. Not an operating migration or physical issuance.
-- Inventory is captured once from explicit original fixture identities; no generation on selection save.
CREATE TABLE combat_b_local_physical_inventory(account_id TEXT NOT NULL,instance_id TEXT NOT NULL,carriage_id TEXT NOT NULL,PRIMARY KEY(account_id,instance_id));
INSERT INTO combat_b_local_physical_inventory SELECT account_id,json_extract(j.value,'$.instanceId'),json_extract(j.value,'$.carriageId') FROM combat_b_fixture_refits,json_each(source_json,'$.instances') j;
CREATE TRIGGER combat_b_local_physical_inventory_no_update BEFORE UPDATE ON combat_b_local_physical_inventory BEGIN SELECT RAISE(ABORT,'LOCAL physical inventory immutable'); END;
CREATE TRIGGER combat_b_local_selection_source_update AFTER UPDATE ON combat_b_selections
WHEN NEW.revision<>OLD.revision BEGIN
 SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM combat_b_fixture_refits WHERE account_id=NEW.account_id AND selection_revision=OLD.revision AND json_extract(source_json,'$.status')='ISOLATED_LOCAL_FIXTURE') THEN RAISE(ABORT,'CHECK constraint failed: valid=1') END;
 UPDATE combat_b_fixture_refits SET source_json=json_set(source_json,'$.instances',json((
 WITH placements AS(SELECT json_extract(value,'$.slot') slot,json_extract(value,'$.carriageId') carriage FROM json_each(NEW.selection_json,'$.placements')),
 matched AS(SELECT p.slot,p.carriage,json_extract(i.value,'$.instanceId') instance FROM placements p JOIN json_each(source_json,'$.instances') i ON json_extract(i.value,'$.slot')=p.slot AND json_extract(i.value,'$.carriageId')=p.carriage JOIN combat_b_local_physical_inventory owned ON owned.account_id=NEW.account_id AND owned.instance_id=json_extract(i.value,'$.instanceId') AND owned.carriage_id=p.carriage),
 missing AS(SELECT p.slot,p.carriage,row_number() OVER(PARTITION BY p.carriage ORDER BY p.slot) n FROM placements p WHERE NOT EXISTS(SELECT 1 FROM matched m WHERE m.slot=p.slot)),
 available AS(SELECT instance_id instance,carriage_id carriage,row_number() OVER(PARTITION BY carriage_id ORDER BY instance_id) n FROM combat_b_local_physical_inventory inv WHERE account_id=NEW.account_id AND NOT EXISTS(SELECT 1 FROM matched m WHERE m.instance=inv.instance_id)),
 mapped AS(SELECT slot,carriage,instance FROM matched UNION ALL SELECT m.slot,m.carriage,a.instance FROM missing m JOIN available a ON a.carriage=m.carriage AND a.n=m.n)
 SELECT json_group_array(json_object('slot',slot,'instanceId',instance,'carriageId',carriage)) FROM (SELECT * FROM mapped ORDER BY slot)
 ))),selection_revision=NEW.revision WHERE account_id=NEW.account_id;
 SELECT CASE WHEN (SELECT json_array_length(source_json,'$.instances') FROM combat_b_fixture_refits WHERE account_id=NEW.account_id)<>json_array_length(NEW.selection_json,'$.placements') THEN RAISE(ABORT,'CHECK constraint failed: valid=1') END;
END;