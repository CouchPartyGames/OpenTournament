-- name: InsertEvent :exec
INSERT INTO events (tournament_id, seq, type, data, private, recipients, created_at)
VALUES ($1, $2, $3, $4, $5, $6, $7);

-- name: ListEventsAfter :many
SELECT * FROM events WHERE tournament_id = $1 AND seq > $2 ORDER BY seq LIMIT $3;

-- name: LastEventSeq :one
SELECT last_event_seq FROM tournaments WHERE id = $1;

-- name: PruneEvents :execrows
DELETE FROM events WHERE created_at < $1;
