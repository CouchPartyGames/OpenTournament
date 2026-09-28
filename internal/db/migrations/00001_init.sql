-- +goose Up
CREATE TABLE tournaments (
    id                    uuid PRIMARY KEY,
    game_id               text        NOT NULL,
    name                  text        NOT NULL,
    organizer             text        NOT NULL,
    status                text        NOT NULL,
    starts_at             timestamptz NOT NULL,
    registration_opens_at timestamptz NOT NULL,
    capacity              integer     NOT NULL,
    minimum_participants  integer     NOT NULL,
    check_in_enabled      boolean     NOT NULL,
    check_in_seconds      integer     NOT NULL,
    last_event_seq        bigint      NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL,
    updated_at            timestamptz NOT NULL,
    completed_at          timestamptz
);
CREATE INDEX tournaments_listing ON tournaments (starts_at DESC, id);

CREATE TABLE stages (
    id                      uuid PRIMARY KEY,
    tournament_id           uuid    NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    position                integer NOT NULL,
    format                  text    NOT NULL,
    group_count             integer NOT NULL,
    advancement             integer NOT NULL,
    best_of                 integer NOT NULL,
    bouts                   integer NOT NULL,
    swiss_rounds            integer NOT NULL,
    result_deadline_seconds integer NOT NULL,
    status                  text    NOT NULL,
    UNIQUE (tournament_id, position)
);

CREATE TABLE participants (
    id             uuid PRIMARY KEY,
    tournament_id  uuid        NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    identity_kind  text        NOT NULL,
    identity_value text        NOT NULL,
    registered_by  text        NOT NULL,
    status         text        NOT NULL,
    registered_at  timestamptz NOT NULL,
    checked_in_at  timestamptz,
    final_from     integer,
    final_to       integer,
    UNIQUE (tournament_id, identity_kind, identity_value)
);

CREATE TABLE groups (
    id            uuid PRIMARY KEY,
    tournament_id uuid    NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    stage_id      uuid    NOT NULL REFERENCES stages ON DELETE CASCADE,
    position      integer NOT NULL,
    status        text    NOT NULL,
    UNIQUE (stage_id, position)
);

CREATE TABLE group_participants (
    group_id       uuid    NOT NULL REFERENCES groups ON DELETE CASCADE,
    participant_id uuid    NOT NULL REFERENCES participants ON DELETE CASCADE,
    seed           integer NOT NULL,
    lot            integer NOT NULL,
    advanced       boolean NOT NULL DEFAULT false,
    PRIMARY KEY (group_id, participant_id)
);

CREATE TABLE matches (
    id              uuid PRIMARY KEY,
    tournament_id   uuid        NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    stage_id        uuid        NOT NULL REFERENCES stages ON DELETE CASCADE,
    group_id        uuid        NOT NULL REFERENCES groups ON DELETE CASCADE,
    key             text        NOT NULL,
    round           integer     NOT NULL,
    bracket         text        NOT NULL,
    status          text        NOT NULL,
    result          text,
    winner_id       uuid REFERENCES participants,
    ready_at        timestamptz,
    result_deadline timestamptz,
    started_at      timestamptz,
    completed_at    timestamptz,
    aborts          integer     NOT NULL DEFAULT 0,
    allocation_id   uuid,
    server_name     text,
    server_address  text,
    server_port     integer,
    UNIQUE (group_id, key)
);
CREATE INDEX matches_by_tournament ON matches (tournament_id);
CREATE INDEX matches_with_servers ON matches (status) WHERE allocation_id IS NOT NULL;

CREATE TABLE match_slots (
    match_id       uuid    NOT NULL REFERENCES matches ON DELETE CASCADE,
    slot           integer NOT NULL,
    participant_id uuid    NOT NULL REFERENCES participants ON DELETE CASCADE,
    PRIMARY KEY (match_id, slot),
    UNIQUE (match_id, participant_id)
);
CREATE INDEX match_slots_by_participant ON match_slots (participant_id);

CREATE TABLE bout_results (
    match_id       uuid        NOT NULL REFERENCES matches ON DELETE CASCADE,
    bout           integer     NOT NULL,
    participant_id uuid        NOT NULL REFERENCES participants ON DELETE CASCADE,
    won            boolean     NOT NULL,
    forfeited      boolean     NOT NULL,
    placement      integer,
    points         integer,
    recorded_at    timestamptz NOT NULL,
    PRIMARY KEY (match_id, bout, participant_id)
);

-- Persisted due times for scheduled and retried work.
CREATE TABLE jobs (
    id            bigserial PRIMARY KEY,
    kind          text        NOT NULL,
    tournament_id uuid        NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    match_id      uuid REFERENCES matches ON DELETE CASCADE,
    due_at        timestamptz NOT NULL,
    attempts      integer     NOT NULL DEFAULT 0,
    UNIQUE NULLS NOT DISTINCT (kind, tournament_id, match_id)
);
CREATE INDEX jobs_due ON jobs (due_at);

-- The live-update outbox (ADR-0003).
CREATE TABLE events (
    tournament_id uuid        NOT NULL REFERENCES tournaments ON DELETE CASCADE,
    seq           bigint      NOT NULL,
    type          text        NOT NULL,
    data          jsonb       NOT NULL,
    private       jsonb,
    recipients    text[]      NOT NULL DEFAULT '{}',
    created_at    timestamptz NOT NULL,
    PRIMARY KEY (tournament_id, seq)
);
CREATE INDEX events_created ON events (created_at);

-- +goose Down
DROP TABLE events, jobs, bout_results, match_slots, matches, group_participants, groups, participants, stages, tournaments;
