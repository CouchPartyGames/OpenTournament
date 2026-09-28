package tournament

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
