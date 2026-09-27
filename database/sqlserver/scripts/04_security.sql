-- Usuario de la aplicación: solo EXECUTE sobre el esquema payments.
-- Los procedimientos acceden a las tablas por encadenamiento de propiedad (mismo dueño: dbo).

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(AppUser)')
    CREATE LOGIN [$(AppUser)] WITH PASSWORD = N'$(AppPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = [$(DbName)];
ELSE
    ALTER LOGIN [$(AppUser)] WITH PASSWORD = N'$(AppPassword)';
GO

USE [$(DbName)];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(AppUser)')
    CREATE USER [$(AppUser)] FOR LOGIN [$(AppUser)] WITH DEFAULT_SCHEMA = payments;
GO

GRANT EXECUTE ON SCHEMA::payments TO [$(AppUser)];
DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::payments TO [$(AppUser)];
GO
