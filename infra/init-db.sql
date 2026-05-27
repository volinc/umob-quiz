CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS game_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NULL,
    score INT NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'Active'
);

CREATE INDEX IF NOT EXISTS ix_game_sessions_user_id ON game_sessions(user_id);

CREATE TABLE IF NOT EXISTS bikes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider VARCHAR(50) NOT NULL,
    external_id VARCHAR(100) NOT NULL,
    location geography(Point, 4326) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_bikes_provider_external UNIQUE (provider, external_id)
);

CREATE INDEX IF NOT EXISTS ix_bikes_location ON bikes USING GIST (location);
CREATE INDEX IF NOT EXISTS ix_bikes_provider_active ON bikes (provider, is_active);

CREATE TABLE IF NOT EXISTS stations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider VARCHAR(50) NOT NULL,
    external_id VARCHAR(100) NOT NULL,
    location geography(Point, 4326) NOT NULL,
    capacity INT NULL,
    num_bikes_available INT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_stations_provider_external UNIQUE (provider, external_id)
);

CREATE INDEX IF NOT EXISTS ix_stations_location ON stations USING GIST (location);
CREATE INDEX IF NOT EXISTS ix_stations_provider_active ON stations (provider, is_active);
