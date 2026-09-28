package tournament

// Tournament statuses.
const (
	Draft            = "draft"
	RegistrationOpen = "registration-open"
	CheckIn          = "check-in"
	Running          = "running"
	Completed        = "completed"
	Cancelled        = "cancelled"
)

// Participant statuses.
const (
	Registered   = "registered"
	CheckedIn    = "checked-in"
	NotCheckedIn = "not-checked-in"
	Active       = "active"
	Withdrawn    = "withdrawn"
	Disqualified = "disqualified"
	Eliminated   = "eliminated"
)

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

// hasLeft reports whether a Participant status means they left the Tournament.
func hasLeft(status string) bool { return status == Withdrawn || status == Disqualified }

// open reports whether a Match can still receive results.
func open(status string) bool {
	return status == MatchReady || status == MatchAllocating || status == MatchInProgress || status == MatchStalled
}
