CREATE SCHEMA IF NOT EXISTS payments;

CREATE TABLE IF NOT EXISTS payments.payment
(
    payment_id       uuid           NOT NULL,
    customer_id      uuid           NOT NULL,
    service_provider varchar(150)   NOT NULL,
    amount           numeric(18, 2) NOT NULL,
    currency         varchar(3)     NOT NULL,
    status           varchar(20)    NOT NULL DEFAULT 'pendiente',
    created_at       timestamptz    NOT NULL DEFAULT now(),
    CONSTRAINT pk_payment PRIMARY KEY (payment_id),
    CONSTRAINT ck_payment_amount CHECK (amount > 0 AND amount <= 1500),
    CONSTRAINT ck_payment_currency CHECK (currency = 'BOB'),
    CONSTRAINT ck_payment_service_provider CHECK (length(btrim(service_provider)) > 0)
);

CREATE INDEX IF NOT EXISTS ix_payment_customer_created
    ON payments.payment (customer_id, created_at DESC);
