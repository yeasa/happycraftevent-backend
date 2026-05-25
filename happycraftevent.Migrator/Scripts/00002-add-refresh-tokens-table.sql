-- 00002: Add refresh_tokens table for session management.

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash TEXT NOT NULL UNIQUE,

    expires_at TIMESTAMP NOT NULL,
    revoked_at TIMESTAMP,
    replaced_by_token_hash TEXT,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ip TEXT,
    revoked_by_ip TEXT
);

COMMENT ON TABLE refresh_tokens IS 'Session tokens for refresh token rotation and revocation.';
COMMENT ON COLUMN refresh_tokens.token_hash IS 'Hash of the actual refresh token; never store raw token.';
COMMENT ON COLUMN refresh_tokens.revoked_at IS 'Timestamp when token was explicitly revoked; NULL if still valid.';
COMMENT ON COLUMN refresh_tokens.replaced_by_token_hash IS 'Hash of the replacement token when rotated; enables chain tracking.';

-- Index for fast user session lookups.
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens (user_id);

-- Index for active (non-revoked, non-replaced) token lookups.
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_valid
    ON refresh_tokens (token_hash)
    WHERE revoked_at IS NULL AND replaced_by_token_hash IS NULL;

-- Index for revoked token tracking and audit.
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_revoked_at ON refresh_tokens (revoked_at)
    WHERE revoked_at IS NOT NULL;
