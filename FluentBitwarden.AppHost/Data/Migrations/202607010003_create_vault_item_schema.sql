CREATE TABLE vault_collection (
    row_id INTEGER PRIMARY KEY,
    user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
    collection_id TEXT NOT NULL COLLATE NOCASE CHECK (length(collection_id) > 0),
    organization_id TEXT NOT NULL COLLATE NOCASE CHECK (length(organization_id) > 0),
    is_read_only INTEGER NOT NULL CHECK (is_read_only IN (0, 1)),
    can_manage INTEGER NOT NULL CHECK (can_manage IN (0, 1)),
    hide_passwords INTEGER NOT NULL CHECK (hide_passwords IN (0, 1)),
    collection_type INTEGER NULL,
    encrypted_name BLOB NOT NULL CHECK (length(encrypted_name) > 0),

    UNIQUE (user_id, collection_id),
    UNIQUE (user_id, organization_id, collection_id),

    FOREIGN KEY (user_id, organization_id) REFERENCES vault_organization(user_id, organization_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);

CREATE TABLE vault_cipher (
    row_id INTEGER PRIMARY KEY,
    user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
    cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
    organization_id TEXT NULL COLLATE NOCASE CHECK (organization_id IS NULL OR length(organization_id) > 0),
    cipher_type INTEGER NOT NULL CHECK (cipher_type IN (1, 2, 3, 4, 5)),
    revision_date_unix_ms INTEGER NOT NULL CHECK (revision_date_unix_ms >= 0),
    creation_date_unix_ms INTEGER NOT NULL CHECK (creation_date_unix_ms >= 0),
    deleted_date_unix_ms INTEGER NULL CHECK (deleted_date_unix_ms IS NULL OR deleted_date_unix_ms >= 0),
    archived_date_unix_ms INTEGER NULL CHECK (archived_date_unix_ms IS NULL OR archived_date_unix_ms >= 0),
    is_favorite INTEGER NOT NULL CHECK (is_favorite IN (0, 1)),
    reprompt INTEGER NOT NULL CHECK (reprompt IN (0, 1)),
    can_edit INTEGER NOT NULL CHECK (can_edit IN (0, 1)),
    can_view_password INTEGER NOT NULL CHECK (can_view_password IN (0, 1)),
    encrypted_cipher_key BLOB NULL CHECK (encrypted_cipher_key IS NULL OR length(encrypted_cipher_key) > 0),
    encrypted_payload BLOB NOT NULL CHECK (length(encrypted_payload) > 0),

    UNIQUE (user_id, cipher_id),
    UNIQUE (user_id, organization_id, cipher_id),

    FOREIGN KEY (user_id, organization_id) REFERENCES vault_organization(user_id, organization_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);


CREATE TABLE vault_cipher_folder (
    user_id TEXT NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
    cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
    folder_id TEXT NOT NULL COLLATE NOCASE CHECK (length(folder_id) > 0),
    
    PRIMARY KEY (user_id, cipher_id),
    
    FOREIGN KEY (user_id, cipher_id) REFERENCES vault_cipher(user_id, cipher_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY (user_id, folder_id) REFERENCES vault_folder(user_id, folder_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);

CREATE TABLE vault_cipher_collection (
    user_id TEXT NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
    cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
    collection_id TEXT NOT NULL COLLATE NOCASE CHECK (length(collection_id) > 0),
    
    PRIMARY KEY (user_id, cipher_id, collection_id),
    
    FOREIGN KEY (user_id, cipher_id) REFERENCES vault_cipher(user_id, cipher_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY (user_id, collection_id) REFERENCES vault_collection(user_id, collection_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);
