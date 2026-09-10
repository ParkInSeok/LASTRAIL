-- Stable offer identity survives wallet policy replacement. No product seed.
CREATE TABLE shop_daily_ticket_claims (
 account_id TEXT NOT NULL REFERENCES accounts(id),
 offer_family TEXT NOT NULL CHECK(offer_family='COMMAND_TICKET_DAILY'),
 day_start INTEGER NOT NULL,
 operation_id TEXT NOT NULL UNIQUE REFERENCES crystal_operations(id),
 PRIMARY KEY(account_id,offer_family,day_start)
);
CREATE TABLE shop_day_guard(valid INTEGER NOT NULL CHECK(valid=1));
