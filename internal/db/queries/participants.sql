-- name: InsertParticipant :exec
INSERT INTO participants (id, tournament_id, identity_kind, identity_value, registered_by, status,
                          registered_at, checked_in_at)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8);

-- name: DeleteParticipant :exec
DELETE FROM participants WHERE id = $1;

-- name: GetParticipant :one
SELECT * FROM participants WHERE id = $1 AND tournament_id = $2;

-- name: ListParticipants :many
SELECT * FROM participants WHERE tournament_id = $1 ORDER BY registered_at, id;

-- name: ListParticipantsByIds :many
SELECT * FROM participants WHERE id = ANY(sqlc.arg(ids)::uuid[]);

-- name: CountRegistered :one
SELECT count(*)::int FROM participants WHERE tournament_id = $1 AND status IN ('registered', 'checked-in');

-- name: CheckInParticipant :exec
UPDATE participants SET status = 'checked-in', checked_in_at = $2 WHERE id = $1;

-- name: SetParticipantStatus :exec
UPDATE participants SET status = $2 WHERE id = $1;

-- name: DropNotCheckedIn :exec
UPDATE participants SET status = 'not-checked-in' WHERE tournament_id = $1 AND status = 'registered';

-- name: ActivateParticipants :many
UPDATE participants SET status = 'active'
WHERE tournament_id = $1 AND status IN ('registered', 'checked-in')
RETURNING id;

-- name: SetFinalPlacement :exec
UPDATE participants SET final_from = $2, final_to = $3 WHERE id = $1;

-- name: ListFinalPlacements :many
SELECT * FROM participants
WHERE tournament_id = $1 AND final_from IS NOT NULL
ORDER BY final_from, final_to, id;
