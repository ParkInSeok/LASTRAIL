-- Complete original v2 journal fragments; no retention or combat authority change.
CREATE TABLE b_result_journal_chunks(run_id TEXT NOT NULL,evidence_ref TEXT NOT NULL,ordinal INTEGER NOT NULL CHECK(ordinal>=0 AND ordinal<32),text_json TEXT NOT NULL,PRIMARY KEY(run_id,evidence_ref,ordinal),FOREIGN KEY(run_id,evidence_ref) REFERENCES b_result_reports(run_id,evidence_ref));
CREATE TRIGGER b_result_journal_no_update BEFORE UPDATE ON b_result_journal_chunks BEGIN SELECT RAISE(ABORT,'b_result_journal_immutable'); END;
CREATE TRIGGER b_result_journal_no_delete BEFORE DELETE ON b_result_journal_chunks BEGIN SELECT RAISE(ABORT,'b_result_journal_immutable'); END;
CREATE UNIQUE INDEX b_result_terminal_command_once ON b_result_reports(run_id,json_extract(report_json,'$.nodeSession.node.nodeId'),json_extract(receipt_json,'$.originalJournal.terminal.commandId')) WHERE json_extract(receipt_json,'$.originalJournal') IS NOT NULL;
CREATE UNIQUE INDEX b_result_terminal_ref_once ON b_result_reports(run_id,json_extract(report_json,'$.nodeSession.node.nodeId'),json_extract(receipt_json,'$.originalJournal.terminal.eventRef')) WHERE json_extract(receipt_json,'$.originalJournal') IS NOT NULL;
