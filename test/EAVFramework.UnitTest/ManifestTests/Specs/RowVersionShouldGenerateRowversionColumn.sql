IF OBJECT_ID(N'[manifest_rowversion].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'manifest_rowversion') IS NULL EXEC(N'CREATE SCHEMA [manifest_rowversion];');
    CREATE TABLE [manifest_rowversion].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_rowversion].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    CREATE TABLE [tests].[customentities] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_customentities] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'customentities';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_rowversion].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    INSERT INTO [manifest_rowversion].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_Initial', N'5.0.14');
END;
GO

COMMIT;
GO

