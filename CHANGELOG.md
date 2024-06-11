# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="0.1.3"></a>
## 0.1.3 (2024-06-11)

<a name="0.1.2"></a>
## 0.1.2 (2024-06-11)

<a name="0.1.1"></a>
## 0.1.1 (2024-06-11)

### Bug Fixes

* remove dead variable

<a name="0.1.0"></a>
## 0.1.0 (2024-06-11)

### Features

* add activity source for opentelemetry traces
* add all single draws to dependency injection
* add aspire dashboard
* add auth based on resource conditions
* add authentication handler for tests
* add authorization policies
* add authorization to endpoints
* add better primary key for registration
* add bye for participant
* add complete match feature
* add configuration options for opentelemetry
* add create tournament endpoint
* add creator timestamp
* add delete tournament feature
* add description in swagger
* add docs
* add domain events
* add draw layout
* add first integration test
* add getmatch feature
* add gettournament feature
* add global exception handler
* add http logging to json console
* add integration testing
* add libraries for integration testing
* add login
* add match layout to dependency injection
* add matches to start tournament
* add matchid converter, clean up
* add more testing on ordering of participants, semi working start tournament
* add nsubstitute, opp match position testing
* add one-to-many relationship for matches
* add one-to-many tournament-match relationship
* add oneof union for the handler
* add opentelemetry metrics
* add opponent ordering
* add outbox ef core configuration
* add outbox for completed match
* add outbox for started tournament
* add participants to feature
* add participants/registration
* add quartz for outbox jobs
* add quartz instrumentation
* add redis service to docker
* add single elimination draw
* add start/join tournament
* add swaggerui to development
* add testing for match ids
* add tests for ordering participants
* add tournamentid converter
* add unit tests for rules
* add update match feature
* add update tournament
* add update tournament feature
* add utitlies to read common claims
* add win/lose progression and participants
* add winner functionality for match
* create match and progression ids
* increase get performance
* move to record type for primary key
* setup opentelemetry logging
* setup opentelemetry tracing
* unit test drawsize object
* use options pattern for database settings

### Bug Fixes

* add opentelemetry protocol
* clean up draw size/ordered participants
* correct 4 participants test
* correct data type for drawsize
* correct execution strategy for transactions/retry policy
* correct http context extensions for claims
* correct integration helper
* correct match completed feature
* correct observability and logging
* correct parsing of guids
* correct participant converter
* correct postgres test container settings
* correct progression on 1 round and final round
* correct scope for dbcontext options, more standard dir for dbcontext
* correct tests for 64,128
* correct transaction and json attributes
* correct winnerid, allow null
* corrected complete tournament feature
* enable update tournament/match
* locally fix integration tests
* point at tournament schema
* use postgresql for integration tests
* use proper tryparse for matchid,tournamentid
* use transactions for outbox
* use transactions, update first round matches
* use value converters for better separation

