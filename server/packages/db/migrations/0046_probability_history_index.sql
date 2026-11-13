-- Bounded environment-scoped immutable publication history. No source or runtime changes.
CREATE INDEX probability_disclosures_environment_id ON probability_disclosures(environment,disclosure_id);
