IF OBJECT_ID(N'[manifest_binary].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'manifest_binary') IS NULL EXEC(N'CREATE SCHEMA [manifest_binary];');
    CREATE TABLE [manifest_binary].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_binary].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    CREATE TABLE [tests].[customentities] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Data] varbinary(max) NULL,
        CONSTRAINT [PK_customentities] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'customentities';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_binary].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    INSERT INTO [manifest_binary].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_Initial', N'5.0.10');
END;
GO

COMMIT;
GO

