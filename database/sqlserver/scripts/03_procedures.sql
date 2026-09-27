USE [$(DbName)];
GO

CREATE OR ALTER PROCEDURE payments.sp_payment_create
    @payment_id       uniqueidentifier,
    @customer_id      uniqueidentifier,
    @service_provider nvarchar(150),
    @amount           decimal(18, 2),
    @currency         varchar(3),
    @status           varchar(20),
    @created_at       datetime2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO payments.payment (payment_id, customer_id, service_provider, amount, currency, status, created_at)
    OUTPUT inserted.payment_id, inserted.customer_id, inserted.service_provider, inserted.amount,
           inserted.currency, inserted.status, inserted.created_at
    VALUES (@payment_id, @customer_id, @service_provider, @amount, @currency, @status, @created_at);
END
GO

CREATE OR ALTER PROCEDURE payments.sp_payment_get_by_customer
    @customer_id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT payment_id, customer_id, service_provider, amount, currency, status, created_at
    FROM payments.payment
    WHERE customer_id = @customer_id
    ORDER BY created_at DESC;
END
GO

CREATE OR ALTER PROCEDURE payments.sp_health_check
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 1 AS status;
END
GO
