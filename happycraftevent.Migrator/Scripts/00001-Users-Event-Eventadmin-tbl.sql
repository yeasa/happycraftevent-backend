-- 00001: Baseline schema for users, events, and event_admins.
-- Notes:
-- 1) Enum values are intentionally NOT stored as DB enum types; app code controls allowed values.
-- 2) Text columns are used instead of varchar as requested.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Keep updated_at in sync for mutable tables.
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- =========================================
-- users
-- =========================================
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    first_name TEXT NOT NULL,
    last_name TEXT,

    email TEXT NOT NULL,
    phone TEXT,

    password_hash TEXT NOT NULL,

    role TEXT NOT NULL CHECK (role IN ('ADMIN', 'SIDE_ADMIN')),

    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE users IS 'System users including ADMIN and SIDE_ADMIN roles managed in code.';
COMMENT ON COLUMN users.is_deleted IS 'Soft delete flag; true means logically deleted.';
COMMENT ON COLUMN users.role IS 'Role text controlled by application enum UserRole.';

-- Enforce case-insensitive uniqueness for email values.
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email_lower ON users (LOWER(email));

-- Direct email index for exact match queries.
CREATE INDEX IF NOT EXISTS ix_users_email ON users (email);

-- Phone index for contact-based lookups.
CREATE INDEX IF NOT EXISTS ix_users_phone_not_deleted
    ON users (phone)
    WHERE is_deleted = FALSE AND phone IS NOT NULL;

-- Role index for authorization and role-filtered queries.
CREATE INDEX IF NOT EXISTS ix_users_role_not_deleted
    ON users (role)
    WHERE is_deleted = FALSE;

-- Helpful index for role-based filters while excluding deleted rows.
CREATE INDEX IF NOT EXISTS ix_users_role_active_not_deleted
    ON users (role, is_active)
    WHERE is_deleted = FALSE;

DROP TRIGGER IF EXISTS trg_users_set_updated_at ON users;
CREATE TRIGGER trg_users_set_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

-- =========================================
-- events
-- =========================================
CREATE TABLE IF NOT EXISTS events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    name TEXT NOT NULL,
    description TEXT,

    event_type TEXT NOT NULL CHECK (event_type IN ('Wedding', 'Corporate', 'Birthday', 'Conference')),

    venue_name TEXT,
    address TEXT,
    city TEXT NOT NULL,
    state TEXT NOT NULL,
    country TEXT,
    pincode TEXT,

    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_events_date_range CHECK (start_date <= end_date),

    created_by UUID REFERENCES users(id) ON DELETE SET NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE events IS 'Events managed by admin and side admins.';
COMMENT ON COLUMN events.event_type IS 'Event type text controlled by application enum EventType.';
COMMENT ON COLUMN events.is_deleted IS 'Soft delete flag; true means logically deleted.';

-- Index for creator-based lookups.
CREATE INDEX IF NOT EXISTS ix_events_created_by ON events (created_by);

-- Index to optimize timeline and upcoming event queries.
CREATE INDEX IF NOT EXISTS ix_events_start_date_end_date ON events (start_date, end_date);

-- Partial index to optimize active (non-deleted) event listings by location.
CREATE INDEX IF NOT EXISTS ix_events_city_state_active
    ON events (city, state)
    WHERE is_deleted = FALSE;

DROP TRIGGER IF EXISTS trg_events_set_updated_at ON events;
CREATE TRIGGER trg_events_set_updated_at
BEFORE UPDATE ON events
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

-- =========================================
-- event_admins (many-to-many mapping)
-- =========================================
CREATE TABLE IF NOT EXISTS event_admins (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE (user_id, event_id)
);

COMMENT ON TABLE event_admins IS 'Mapping table between side admins and assigned events.';
COMMENT ON COLUMN event_admins.is_active IS 'Assignment status; false disables access without deleting mapping.';

-- Separate index for event-centric lookups (UNIQUE index already supports user_id,event_id order).
CREATE INDEX IF NOT EXISTS ix_event_admins_event_id ON event_admins (event_id);

-- Active assignment index for event admin authorization checks.
CREATE INDEX IF NOT EXISTS ix_event_admins_event_id_active
    ON event_admins (event_id, is_active);
