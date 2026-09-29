-- name: InsertTournament :exec
INSERT INTO tournaments (id, game_id, name, organizer, status, starts_at, registration_opens_at,
                         capacity, minimum_participants, check_in_enabled, check_in_seconds,
                         created_at, updated_at, manifest)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $12, $13);

-- name: GetDeclaredTournament :one
SELECT * FROM tournaments WHERE manifest = sqlc.arg(manifest)::text;

-- name: ListDeclaredTournaments :many
-- Tournament Manifests (tournament/<namespace>/<name>) and Occurrences
-- (recurring/<namespace>/<name>/<start>) both record their namespace second.
SELECT * FROM tournaments
WHERE split_part(manifest, '/', 2) = ANY(sqlc.arg(namespaces)::text[])
ORDER BY id;

-- name: ListOccurrenceTournaments :many
-- The Tournaments one Recurring Tournament declared, by the prefix
-- recurring/<namespace>/<name>/ of their Occurrences.
SELECT * FROM tournaments
WHERE starts_with(manifest, sqlc.arg(prefix)::text)
ORDER BY starts_at, id;

-- name: DeleteTournament :exec
DELETE FROM tournaments WHERE id = $1;

-- name: UpdateTournamentSettings :exec
UPDATE tournaments
SET name = $2, starts_at = $3, registration_opens_at = $4, capacity = $5,
    minimum_participants = $6, check_in_enabled = $7, check_in_seconds = $8, updated_at = $9
WHERE id = $1;

-- name: LockTournament :one
SELECT * FROM tournaments WHERE id = $1 FOR UPDATE;

-- name: GetTournament :one
SELECT * FROM tournaments WHERE id = $1;

-- name: ListTournaments :many
-- An empty status lists Tournaments in every status. Comparing with the
-- status column first types the parameter as a TournamentStatus.
SELECT * FROM tournaments
WHERE (status = sqlc.arg(status) OR sqlc.arg(status) = '')
  AND (sqlc.narg(game_id)::text IS NULL OR game_id = sqlc.narg(game_id))
ORDER BY starts_at DESC, id
LIMIT sqlc.arg(max_results) OFFSET sqlc.arg(skip);

-- name: SetTournamentStatus :exec
UPDATE tournaments SET status = $2, updated_at = $3 WHERE id = $1;

-- name: CompleteTournament :exec
UPDATE tournaments SET status = 'completed', updated_at = $2, completed_at = $2 WHERE id = $1;

-- name: NextEventSeq :one
UPDATE tournaments SET last_event_seq = last_event_seq + 1 WHERE id = $1 RETURNING last_event_seq;

-- name: RegistrationCounts :many
SELECT tournament_id,
       count(*) FILTER (WHERE status <> 'not-checked-in')::int AS registered,
       count(*) FILTER (WHERE checked_in_at IS NOT NULL)::int AS checked_in
FROM participants
WHERE tournament_id = ANY(sqlc.arg(tournament_ids)::uuid[])
GROUP BY tournament_id;
