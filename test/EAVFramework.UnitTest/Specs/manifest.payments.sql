IF OBJECT_ID(N'[payments].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'payments') IS NULL EXEC(N'CREATE SCHEMA [payments];');
    CREATE TABLE [payments].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [payments].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    CREATE TABLE [tests].[PaymentProviderTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(255) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [Properties] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PaymentProviderTypes] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'PaymentProviderTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [payments].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    CREATE TABLE [tests].[PaymentProviders] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AuthContext] nvarchar(max) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [PaymentProviderTypeId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PaymentProviders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentProviders_PaymentProviderTypes_PaymentProviderTypeId] FOREIGN KEY ([PaymentProviderTypeId]) REFERENCES [tests].[PaymentProviderTypes] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'PaymentProviders';
END;
GO

IF NOT EXISTS(SELECT * FROM [payments].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    CREATE TABLE [tests].[Agreements] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [ProviderId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Agreements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Agreements_PaymentProviders_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [tests].[PaymentProviders] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Agreements';
END;
GO

IF NOT EXISTS(SELECT * FROM [payments].[__MigrationsHistory] WHERE [MigrationId] = N'tests_Initial')
BEGIN
    INSERT INTO [payments].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_Initial', N'{{VERSION}}');
END;
GO

COMMIT;
GO

