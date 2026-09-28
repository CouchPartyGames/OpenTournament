-- name: InsertStage :exec
INSERT INTO stages (id, tournament_id, position, format, group_count, advancement, best_of, bouts,
                    swiss_rounds, result_deadline_seconds, status)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11);

-- name: DeleteStages :exec
DELETE FROM stages WHERE tournament_id = $1;

-- name: ListStages :many
SELECT * FROM stages WHERE tournament_id = $1 ORDER BY position;

-- name: ListStagesOfTournaments :many
SELECT * FROM stages WHERE tournament_id = ANY(sqlc.arg(tournament_ids)::uuid[]) ORDER BY tournament_id, position;

-- name: SetStageStatus :exec
UPDATE stages SET status = $2 WHERE id = $1;

-- name: InsertGroup :exec
INSERT INTO groups (id, tournament_id, stage_id, position, status) VALUES ($1, $2, $3, $4, $5);

-- name: GetGroup :one
SELECT * FROM groups WHERE id = $1;

-- name: ListGroups :many
SELECT * FROM groups WHERE tournament_id = $1 ORDER BY position;

-- name: ListGroupsOfStage :many
SELECT * FROM groups WHERE stage_id = $1 ORDER BY position;

-- name: SetGroupStatus :exec
UPDATE groups SET status = $2 WHERE id = $1;

-- name: InsertGroupParticipant :exec
INSERT INTO group_participants (group_id, participant_id, seed, lot) VALUES ($1, $2, $3, $4);

-- name: ListGroupParticipants :many
SELECT e.group_id, e.participant_id, e.seed, e.lot, e.advanced, p.status
FROM group_participants e JOIN participants p ON p.id = e.participant_id
WHERE e.group_id = $1
ORDER BY e.seed;

-- name: ListGroupParticipantsOfTournament :many
SELECT e.group_id, e.participant_id, e.seed, e.lot, e.advanced, p.status
FROM group_participants e
JOIN groups g ON g.id = e.group_id
JOIN participants p ON p.id = e.participant_id
WHERE g.tournament_id = $1
ORDER BY e.group_id, e.seed;

-- name: MarkAdvanced :exec
UPDATE group_participants SET advanced = true WHERE group_id = $1 AND participant_id = $2;
