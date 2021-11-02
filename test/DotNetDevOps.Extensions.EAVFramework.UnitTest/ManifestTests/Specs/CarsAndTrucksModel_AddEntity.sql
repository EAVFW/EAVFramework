IF OBJECT_ID(N'[manifest_migrations].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'manifest_migrations') IS NULL EXEC(N'CREATE SCHEMA [manifest_migrations];');
    CREATE TABLE [manifest_migrations].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Cars] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        CONSTRAINT [PK_Cars] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Cars';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    INSERT INTO [manifest_migrations].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_0', N'5.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_1')
BEGIN
    CREATE TABLE [tests].[Trucks] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        CONSTRAINT [PK_Trucks] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Trucks';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_1')
BEGIN
    INSERT INTO [manifest_migrations].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_1', N'5.0.10');
END;
GO

COMMIT;
GO

