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
    CREATE TABLE [tests].[Garages] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        CONSTRAINT [PK_Garages] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Garages';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    INSERT INTO [manifest_migrations].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_0', N'{{VERSION}}');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_1')
BEGIN
    ALTER TABLE [tests].[Cars] ADD [GarageToParkId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_1')
BEGIN
    ALTER TABLE [tests].[Cars] ADD CONSTRAINT [FK_Cars_Garages_GarageToParkId] FOREIGN KEY ([GarageToParkId]) REFERENCES [tests].[Garages] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_1')
BEGIN
    INSERT INTO [manifest_migrations].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_1', N'{{VERSION}}');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_10')
BEGIN
    ALTER TABLE [tests].[Cars] DROP CONSTRAINT [FK_Cars_Garages_GarageToParkId];
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_10')
BEGIN
    ALTER TABLE [tests].[Cars] ADD CONSTRAINT [FK_Cars_Garages_GarageToParkId] FOREIGN KEY ([GarageToParkId]) REFERENCES [tests].[Garages] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_10')
BEGIN
    INSERT INTO [manifest_migrations].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_10', N'{{VERSION}}');
END;
GO

COMMIT;
GO

