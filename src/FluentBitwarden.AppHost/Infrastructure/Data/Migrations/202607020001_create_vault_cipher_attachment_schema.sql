CREATE TABLE vault_cipher_attachment (
    user_id TEXT NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
    cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
    attachment_id TEXT NOT NULL COLLATE NOCASE CHECK (length(attachment_id) > 0),
    sort_order INTEGER NOT NULL CHECK (sort_order >= 0),
    encrypted_file_name BLOB NOT NULL CHECK (length(encrypted_file_name) > 0),
    size INTEGER NOT NULL CHECK (size >= 0),

    PRIMARY KEY (user_id, cipher_id, attachment_id),
    FOREIGN KEY (user_id, cipher_id) REFERENCES vault_cipher(user_id, cipher_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX ix_vault_cipher_attachment_user_cipher_sort_order
ON vault_cipher_attachment (user_id, cipher_id, sort_order);
