CREATE TABLE vault_organization (
    row_id INTEGER PRIMARY KEY,
    user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
    organization_id TEXT NOT NULL COLLATE NOCASE CHECK (length(organization_id) > 0),
    organization_user_id TEXT NULL COLLATE NOCASE CHECK (organization_user_id IS NULL OR length(organization_user_id) > 0),
    organization_name TEXT NOT NULL CHECK (length(organization_name) > 0),
    is_enabled INTEGER NOT NULL CHECK (is_enabled IN (0, 1)),
    access_secrets_manager INTEGER NOT NULL CHECK (access_secrets_manager IN (0, 1)),
    member_status INTEGER NOT NULL,
    encrypted_organization_key BLOB NULL CHECK (encrypted_organization_key IS NULL OR length(encrypted_organization_key) > 0),
    
    UNIQUE (user_id, organization_id)
);

CREATE TABLE vault_folder (
    row_id INTEGER PRIMARY KEY,
    user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
    folder_id TEXT NOT NULL COLLATE NOCASE CHECK (length(folder_id) > 0),
    revision_date_unix_ms INTEGER NOT NULL CHECK (revision_date_unix_ms >= 0),
    encrypted_name BLOB NOT NULL CHECK (length(encrypted_name) > 0),
    
    UNIQUE (user_id, folder_id)
);