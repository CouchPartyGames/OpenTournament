-- name: ScheduleJob :exec
INSERT INTO jobs (kind, tournament_id, match_id, due_at) VALUES ($1, $2, $3, $4)
ON CONFLICT (kind, tournament_id, match_id) DO UPDATE SET due_at = EXCLUDED.due_at, attempts = 0;

-- name: UnscheduleJob :exec
DELETE FROM jobs WHERE kind = $1 AND tournament_id = $2 AND match_id IS NOT DISTINCT FROM sqlc.narg(match_id);

-- name: UnscheduleTournamentJobs :exec
DELETE FROM jobs WHERE tournament_id = $1;

-- name: ClaimDueJobs :many
-- Leases due jobs by pushing their due time forward, so a crashed worker's
-- jobs become due again once the lease runs out.
UPDATE jobs SET due_at = sqlc.arg(lease_until), attempts = attempts + 1
WHERE id IN (
    SELECT id FROM jobs WHERE jobs.due_at <= sqlc.arg(now)
    ORDER BY jobs.due_at LIMIT sqlc.arg(max_jobs) FOR UPDATE SKIP LOCKED
)
RETURNING *;

-- name: FinishJob :exec
DELETE FROM jobs WHERE id = $1 AND due_at = $2;

-- name: RetryJob :exec
UPDATE jobs SET due_at = sqlc.arg(retry_at) WHERE id = $1 AND due_at = sqlc.arg(leased_until);

-- name: NextJobDue :one
SELECT due_at FROM jobs ORDER BY due_at LIMIT 1;
