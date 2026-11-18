-- Consent is scoped to one tenant and one operator-configured recipient contract.
CREATE TABLE IF NOT EXISTS statistics_consents (
    tenant_id uuid PRIMARY KEY,
    recipient_hash text NOT NULL,
    delivery_seed uuid NOT NULL,
    enabled_at timestamptz NOT NULL,
    enabled_by uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    last_delivered_week date,
    pending_week date,
    pending_counts jsonb,
    CHECK ((pending_week IS NULL) = (pending_counts IS NULL))
);
