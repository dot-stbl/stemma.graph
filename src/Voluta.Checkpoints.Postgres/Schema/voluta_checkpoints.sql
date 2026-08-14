-- Voluta Postgres checkpointer schema (wire format v1).
-- Apply once per database (idempotent). Default table: public.voluta_checkpoints
-- Override schema/table via PostgresCheckpointerOptions.Schema / .Table.

CREATE TABLE IF NOT EXISTS public.voluta_checkpoints (
    thread_id   text        NOT NULL,
    step        bigint      NOT NULL,
    status      text        NOT NULL,
    snapshot    jsonb       NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (thread_id, step)
);

CREATE INDEX IF NOT EXISTS ix_voluta_checkpoints_thread_step
    ON public.voluta_checkpoints (thread_id, step DESC);
