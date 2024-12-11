# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="0.9.1"></a>
## 0.9.1 (2024-12-11)

### Bug Fixes

* correct version of endpoint groups
* default scalar to csharp

<a name="0.9.0"></a>
## 0.9.0 (2024-12-10)

### Features

* add local and redis caching

### Bug Fixes

* update docs, cleanup endpoints

<a name="0.7.6"></a>
## 0.7.6 (2024-12-03)

### Bug Fixes

* correct match completion

<a name="0.7.5"></a>
## 0.7.5 (2024-12-03)

### Bug Fixes

* add problem details to empty status code responses

<a name="0.7.4"></a>
## 0.7.4 (2024-12-02)

### Bug Fixes

* correct completed match, prevent opponent injection

<a name="0.7.3"></a>
## 0.7.3 (2024-12-01)

### Bug Fixes

* fix dotnet versions in github actions

<a name="0.7.2"></a>
## 0.7.2 (2024-12-01)

### Bug Fixes

* ensure matches with byes are marked complete

<a name="0.5.2"></a>
## 0.5.2 (2024-11-15)

<a name="0.4.6"></a>
## 0.4.6 (2024-09-16)

### Bug Fixes

* allow next match with both opponents

<a name="0.4.5"></a>
## 0.4.5 (2024-09-16)

### Bug Fixes

* correct match completed consumer handler

<a name="0.4.4"></a>
## 0.4.4 (2024-09-13)

<a name="0.4.3"></a>
## 0.4.3 (2024-09-13)

### Bug Fixes

* add tournament id to match events

<a name="0.4.2"></a>
## 0.4.2 (2024-09-13)

### Bug Fixes

* correct tournament started consumer

<a name="0.4.0"></a>
## 0.4.0 (2024-09-13)

### Features

* add command to masstransit

<a name="0.3.0"></a>
## 0.3.0 (2024-09-12)

### Features

* add consumer events
* add consumers
* add masstransit deps
* use bus

<a name="0.2.1"></a>
## 0.2.1 (2024-09-04)

### Bug Fixes

* remove missing guid

<a name="0.2.0"></a>
## 0.2.0 (2024-08-30)

### Features

* add basic layout
* add daisyui

### Bug Fixes

* correct configuration for telemetry
* correct project
* correct references to solution file

<a name="0.1.7"></a>
## 0.1.7 (2024-07-15)

### Bug Fixes

* new otel logging syntax

<a name="0.1.6"></a>
## 0.1.6 (2024-07-15)

<a name="0.1.5"></a>
## 0.1.5 (2024-07-01)

<a name="0.1.4"></a>
## 0.1.4 (2024-06-11)

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

