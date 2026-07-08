CREATE TABLE account_profiles (
    user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
    email TEXT NOT NULL CHECK (length(email) > 0),
    api_base TEXT NOT NULL CHECK (length(api_base) > 0),
    identity_base TEXT NOT NULL CHECK (length(identity_base) > 0),
    notifications_base TEXT NOT NULL CHECK (length(notifications_base) > 0),
    vault_base TEXT NOT NULL CHECK (length(vault_base) > 0),
    profile_name TEXT NULL CHECK (profile_name IS NULL OR length(profile_name) > 0),
    profile_culture TEXT NULL CHECK (profile_culture IS NULL OR length(profile_culture) > 0),
    profile_creation_date_unix_ms INTEGER NULL CHECK (profile_creation_date_unix_ms IS NULL OR profile_creation_date_unix_ms >= 0),
    profile_synced INTEGER NOT NULL DEFAULT 0 CHECK (profile_synced IN (0, 1))
);

CREATE TABLE account_key_material (
    user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
    salt TEXT NOT NULL CHECK (length(salt) > 0),
    encrypted_user_key BLOB NOT NULL CHECK (length(encrypted_user_key) > 0),
    encrypted_private_key BLOB NOT NULL CHECK (length(encrypted_private_key) > 0),
    kdf_type INTEGER NOT NULL CHECK (kdf_type IN (0, 1)),
    kdf_iterations INTEGER NOT NULL CHECK (kdf_iterations > 0),
    kdf_memory_mib INTEGER,
    kdf_parallelism INTEGER,

    CHECK (
        (kdf_type = 0 AND kdf_memory_mib IS NULL AND kdf_parallelism IS NULL) OR
        (
            kdf_type = 1 AND
            kdf_memory_mib IS NOT NULL AND kdf_memory_mib > 0 AND
            kdf_parallelism IS NOT NULL AND kdf_parallelism > 0
        )
    )
);

CREATE TABLE account_session_tokens (
    user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
    protected_refresh_token BLOB NOT NULL CHECK (length(protected_refresh_token) > 0)
);

CREATE TABLE account_tpm_cng_unlock_keys (
    user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
    protected_user_key BLOB NOT NULL CHECK (length(protected_user_key) > 0)
);