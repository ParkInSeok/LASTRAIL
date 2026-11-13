-- Approved solo result consistency evidence. No legacy upgrades or retention policy.
CREATE TABLE b_result_reports(run_id TEXT NOT NULL REFERENCES runs(id),account_id TEXT NOT NULL REFERENCES accounts(id),request_id TEXT NOT NULL,evidence_ref TEXT NOT NULL,request_hash TEXT NOT NULL,report_json TEXT NOT NULL CHECK(json_valid(report_json)),receipt_json TEXT NOT NULL CHECK(json_valid(receipt_json)),origin_state_json TEXT NOT NULL CHECK(json_valid(origin_state_json)),origin_revision INTEGER NOT NULL CHECK(origin_revision>0),PRIMARY KEY(run_id,request_id),UNIQUE(run_id,evidence_ref));
CREATE TABLE b_result_report_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER b_result_report_no_update BEFORE UPDATE ON b_result_reports BEGIN SELECT RAISE(ABORT,'b_result_report_immutable'); END;
CREATE TRIGGER b_result_report_no_delete BEFORE DELETE ON b_result_reports BEGIN SELECT RAISE(ABORT,'b_result_report_immutable'); END;
CREATE TABLE b_result_node_links(run_id TEXT NOT NULL,node_id TEXT NOT NULL,evidence_ref TEXT NOT NULL,map_revision INTEGER NOT NULL CHECK(map_revision>0),PRIMARY KEY(run_id,node_id),FOREIGN KEY(run_id,evidence_ref) REFERENCES b_result_reports(run_id,evidence_ref));
CREATE TRIGGER b_result_link_no_update BEFORE UPDATE ON b_result_node_links BEGIN SELECT RAISE(ABORT,'b_result_link_immutable'); END;
CREATE TRIGGER b_result_link_no_delete BEFORE DELETE ON b_result_node_links BEGIN SELECT RAISE(ABORT,'b_result_link_immutable'); END;

CREATE UNIQUE INDEX b_result_cursor_once ON b_result_reports(run_id,json_extract(report_json,'$.nodeSession.node.nodeId'),json_array_length(report_json,'$.nodeSession.runtime.commands'));
CREATE UNIQUE INDEX b_result_event_once ON b_result_reports(run_id,json_extract(report_json,'$.nodeSession.node.nodeId'),json_extract(report_json,'$.nodeSession.runtime.commands[#-1].eventId'));