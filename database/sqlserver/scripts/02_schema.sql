USE [$(DbName)];
GO

IF SCHEMA_ID(N'payments') IS NULL
    EXEC (N'CREATE SCHEMA payments AUTHORIZATION dbo');
GO

IF OBJECT_ID(N'payments.payment', N'U') IS NULL
BEGIN
    CREATE TABLE payments.payment
    (
        payment_id       uniqueidentifier NOT NULL,
        customer_id      uniqueidentifier NOT NULL,
        service_provider nvarchar(150)    NOT NULL,
        amount           decimal(18, 2)   NOT NULL,
        currency         varchar(3)       NOT NULL,
        status           varchar(20)      NOT NULL CONSTRAINT df_payment_status DEFAULT ('pendiente'),
        created_at       datetime2(7)     NOT NULL CONSTRAINT df_payment_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT pk_payment PRIMARY KEY NONCLUSTERED (payment_id),
        CONSTRAINT ck_payment_amount CHECK (amount > 0 AND amount <= 1500),
        CONSTRAINT ck_payment_currency CHECK (currency = 'BOB'),
        CONSTRAINT ck_payment_service_provider CHECK (LEN(LTRIM(RTRIM(service_provider))) > 0)
    );

    -- Las consultas siempre filtran por cliente y ordenan por fecha.
    CREATE CLUSTERED INDEX ix_payment_customer_created ON payments.payment (customer_id, created_at DESC);
END
GO
