-- En PostgreSQL los procedimientos que devuelven filas se implementan como funciones.
-- SECURITY DEFINER: se ejecutan con los permisos del dueño, así el usuario de la app no necesita acceso a las tablas.

CREATE OR REPLACE FUNCTION payments.sp_payment_create(
    p_payment_id       uuid,
    p_customer_id      uuid,
    p_service_provider varchar,
    p_amount           numeric,
    p_currency         varchar,
    p_status           varchar,
    p_created_at       timestamptz)
    RETURNS TABLE
            (
                payment_id       uuid,
                customer_id      uuid,
                service_provider varchar,
                amount           numeric,
                currency         varchar,
                status           varchar,
                created_at       timestamptz
            )
    LANGUAGE sql
    SECURITY DEFINER
    SET search_path = payments, pg_temp
AS
$$
INSERT INTO payments.payment AS p (payment_id, customer_id, service_provider, amount, currency, status, created_at)
VALUES (p_payment_id, p_customer_id, p_service_provider, p_amount, p_currency, p_status, p_created_at)
RETURNING p.payment_id, p.customer_id, p.service_provider, p.amount, p.currency, p.status, p.created_at;
$$;

CREATE OR REPLACE FUNCTION payments.sp_payment_get_by_customer(p_customer_id uuid)
    RETURNS TABLE
            (
                payment_id       uuid,
                customer_id      uuid,
                service_provider varchar,
                amount           numeric,
                currency         varchar,
                status           varchar,
                created_at       timestamptz
            )
    LANGUAGE sql
    STABLE
    SECURITY DEFINER
    SET search_path = payments, pg_temp
AS
$$
SELECT p.payment_id, p.customer_id, p.service_provider, p.amount, p.currency, p.status, p.created_at
FROM payments.payment p
WHERE p.customer_id = p_customer_id
ORDER BY p.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION payments.sp_health_check()
    RETURNS integer
    LANGUAGE sql
    STABLE
AS
$$
SELECT 1;
$$;
