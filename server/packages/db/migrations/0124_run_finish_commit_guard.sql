-- Fail the entire original finish transaction when its observed authority changed.
CREATE TABLE run_finish_write_guard (valid INTEGER NOT NULL CHECK(valid=1));
