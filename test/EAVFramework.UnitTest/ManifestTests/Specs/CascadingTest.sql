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
    CREATE TABLE [tests].[FormSubmissions] (
        [Id] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_FormSubmissions] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'FormSubmissions';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Filer] (
        [Id] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NULL,
        [FileId] uniqueidentifier NULL,
        [MyRepeatingTableId] uniqueidentifier NULL,
        CONSTRAINT [PK_Filer] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Filer_Documents_FileId] FOREIGN KEY ([FileId]) REFERENCES [KFST].[Documents] ([Id]),
        CONSTRAINT [FK_Filer_MyRepeatingTable_MyRepeatingTableId] FOREIGN KEY ([MyRepeatingTableId]) REFERENCES [tests].[MyRepeatingTable] ([Id]) ON DELETE CASCADE
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Filer';
END;
GO

IF NOT EXISTS(SELECT * FROM [manifest_migrations].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[MyRepeatingTable] (
        [Id] uniqueidentifier NOT NULL,
        [FormSubmissionId] uniqueidentifier NULL,
        CONSTRAINT [PK_MyRepeatingTable] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MyRepeatingTable_FormSubmissions_FormSubmissionId] FOREIGN KEY ([FormSubmissionId]) REFERENCES [tests].[FormSubmissions] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'MyRepeatingTable';
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

