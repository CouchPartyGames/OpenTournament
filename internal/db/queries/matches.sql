-- name: InsertMatch :exec
INSERT INTO matches (id, tournament_id, stage_id, group_id, key, round, bracket, status)
VALUES ($1, $2, $3, $4, $5, $6, $7, 'pending');

-- name: GetMatch :one
SELECT * FROM matches WHERE id = $1;

-- name: ListMatchesOfGroup :many
SELECT * FROM matches WHERE group_id = $1 ORDER BY round, key;

-- name: ListMatchesOfTournament :many
SELECT * FROM matches WHERE tournament_id = $1 ORDER BY stage_id, group_id, round, key;

-- name: ListMatchesByIds :many
SELECT * FROM matches WHERE id = ANY(sqlc.arg(ids)::uuid[]);

-- name: ListOpenMatchesOfParticipant :many
SELECT m.* FROM matches m JOIN match_slots s ON s.match_id = m.id
WHERE s.participant_id = $1 AND m.status IN ('ready', 'allocating', 'in-progress', 'stalled');

-- name: ListOpenMatchesOfTournament :many
SELECT * FROM matches
WHERE tournament_id = $1 AND status NOT IN ('completed', 'cancelled');

-- name: ListMatchesOnServers :many
SELECT * FROM matches WHERE status IN ('allocating', 'in-progress') AND server_name IS NOT NULL;

-- name: SetMatchStatus :exec
UPDATE matches SET status = $2 WHERE id = $1;

-- name: SetMatchReady :exec
UPDATE matches SET status = 'ready', ready_at = $2, result_deadline = $3 WHERE id = $1 AND status = 'pending';

-- name: CompleteMatch :exec
UPDATE matches SET status = 'completed', result = $2, winner_id = $3, completed_at = $4 WHERE id = $1;

-- name: CancelOpenMatches :many
UPDATE matches SET status = 'cancelled'
WHERE tournament_id = $1 AND status NOT IN ('completed', 'cancelled')
RETURNING *;

-- name: RequestAllocation :one
-- Starts an allocation attempt for a Match waiting for a Game Server.
UPDATE matches SET status = 'allocating', allocation_id = $2,
                   server_name = NULL, server_address = NULL, server_port = NULL
WHERE id = $1 AND status IN ('ready', 'allocating') AND server_name IS NULL
RETURNING *;

-- name: ConfirmAllocation :one
UPDATE matches SET server_name = $3, server_address = $4, server_port = $5
WHERE id = $1 AND allocation_id = $2 AND status = 'allocating' AND server_name IS NULL
RETURNING *;

-- name: MarkMatchStarted :execrows
UPDATE matches SET status = 'in-progress', started_at = COALESCE(started_at, $3)
WHERE id = $1 AND allocation_id = $2 AND status IN ('allocating', 'in-progress');

-- name: AbortMatch :one
-- Aborts a Match only if it still runs on the given allocation, so exactly
-- one replica acts on a failed Game Server.
UPDATE matches SET status = 'allocating', aborts = aborts + 1, allocation_id = NULL,
                   server_name = NULL, server_address = NULL, server_port = NULL
WHERE id = $1 AND allocation_id = $2 AND server_name IS NOT NULL AND status IN ('allocating', 'in-progress')
RETURNING *;

-- name: ClearAllocation :exec
UPDATE matches SET allocation_id = NULL, server_name = NULL, server_address = NULL, server_port = NULL
WHERE id = $1 AND allocation_id = $2;

-- name: InsertSlot :exec
INSERT INTO match_slots (match_id, slot, participant_id) VALUES ($1, $2, $3);

-- name: DeleteSlots :exec
DELETE FROM match_slots WHERE match_id = $1;

-- name: ListSlotsOfGroup :many
SELECT s.* FROM match_slots s JOIN matches m ON m.id = s.match_id
WHERE m.group_id = $1 ORDER BY s.match_id, s.slot;

-- name: ListSlotsOfTournament :many
SELECT s.* FROM match_slots s JOIN matches m ON m.id = s.match_id
WHERE m.tournament_id = $1 ORDER BY s.match_id, s.slot;

-- name: ListSlotsOfMatches :many
SELECT * FROM match_slots WHERE match_id = ANY(sqlc.arg(match_ids)::uuid[]) ORDER BY match_id, slot;

-- name: InsertBoutResult :exec
INSERT INTO bout_results (match_id, bout, participant_id, won, forfeited, placement, points, recorded_at)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8);

-- name: ListBoutsOfGroup :many
SELECT b.* FROM bout_results b JOIN matches m ON m.id = b.match_id
WHERE m.group_id = $1 ORDER BY b.match_id, b.bout, b.participant_id;

-- name: ListBoutsOfTournament :many
SELECT b.* FROM bout_results b JOIN matches m ON m.id = b.match_id
WHERE m.tournament_id = $1 ORDER BY b.match_id, b.bout, b.participant_id;

-- name: ListBoutsOfMatch :many
SELECT * FROM bout_results WHERE match_id = $1 ORDER BY bout, participant_id;
