IF DB_ID(N'$(DbName)') IS NULL
    CREATE DATABASE [$(DbName)];
GO
