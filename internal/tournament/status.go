package tournament

// Match statuses.
const (
	MatchPending    = "pending"
	MatchReady      = "ready"
	MatchAllocating = "allocating"
	MatchInProgress = "in-progress"
	MatchStalled    = "stalled"
	MatchCompleted  = "completed"
	MatchCancelled  = "cancelled"
)

// Match results.
const (
	ResultWin           = "win"
	ResultDoubleForfeit = "double-forfeit"
	ResultBye           = "bye"
	ResultEmpty         = "empty"
	ResultFreeForAll    = "free-for-all"
)

// Stage and Group statuses.
const (
	StagePending   = "pending"
	StageRunning   = "running"
	StageCompleted = "completed"
)

// Job kinds.
const (
	JobOpenRegistration = "open-registration"
	JobOpenCheckIn      = "open-check-in"
	JobStart            = "start"
	JobAllocate         = "allocate"
	JobResultDeadline   = "result-deadline"
)

// open reports whether a Match can still receive results.
func open(status string) bool {
	return status == MatchReady || status == MatchAllocating || status == MatchInProgress || status == MatchStalled
}
