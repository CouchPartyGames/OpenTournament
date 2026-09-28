-- +goose Up
-- The Tournament Manifest a declared Tournament came from, as
-- tournament/<namespace>/<name> (ADR-0005). Unique, so declaring the same
-- Manifest again, or from several replicas at once, never creates a second
-- Tournament.
ALTER TABLE tournaments ADD COLUMN manifest text UNIQUE;

-- +goose Down
ALTER TABLE tournaments DROP COLUMN manifest;
