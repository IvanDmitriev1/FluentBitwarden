CREATE TABLE vault_sync_state (
    user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
    server_revision_date_unix_ms INTEGER NOT NULL CHECK (server_revision_date_unix_ms >= 0)
);
