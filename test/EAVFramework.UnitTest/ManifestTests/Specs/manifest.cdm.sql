IF OBJECT_ID(N'[tests].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'tests') IS NULL EXEC(N'CREATE SCHEMA [tests];');
    CREATE TABLE [tests].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Identities] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Identities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Identities_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Identities_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Identities_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Identities';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Countries] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Alpha2Code] nvarchar(2) NULL,
        [Alpha3Code] nvarchar(3) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [NameISO3166] nvarchar(255) NULL,
        [NumericCode] int NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Countries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Countries_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Countries_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Countries_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Countries';
    SET @description = N'Display Name';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Countries', 'COLUMN', N'Name';
    SET @description = N'The ISO 3166 name';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Countries', 'COLUMN', N'NameISO3166';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Addresses] (
        [Id] uniqueidentifier NOT NULL,
        [AddressLine] nvarchar(255) NULL,
        [AddressLine2] nvarchar(255) NULL,
        [City] nvarchar(100) NULL,
        [CountryId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [ZipCode] int NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [tests].[Countries] ([Id]),
        CONSTRAINT [FK_Addresses_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Addresses_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Addresses_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Addresses';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[AccountTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountTypes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountTypes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountTypes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AccountTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Accounts] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AccountTypeId] uniqueidentifier NULL,
        [AddressId] uniqueidentifier NULL,
        [AllowExternalSignin] bit NULL,
        [BillingAddressId] uniqueidentifier NULL,
        [Companyform] nvarchar(100) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EANCode] nvarchar(15) NULL,
        [Email] nvarchar(100) NULL,
        [ExternalId] nvarchar(100) NULL,
        [FieldMetadata] nvarchar(max) NULL,
        [Homepage] nvarchar(255) NULL,
        [Mainindustry] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [OwnershipType] int NULL,
        [Phone] nvarchar(100) NULL,
        [PrimaryCompanyCode] nvarchar(8) NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accounts_AccountTypes_AccountTypeId] FOREIGN KEY ([AccountTypeId]) REFERENCES [tests].[AccountTypes] ([Id]),
        CONSTRAINT [FK_Accounts_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [tests].[Addresses] ([Id]),
        CONSTRAINT [FK_Accounts_Addresses_BillingAddressId] FOREIGN KEY ([BillingAddressId]) REFERENCES [tests].[Addresses] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Accounts';
    SET @description = N'Metadata field for the info about which fields are locked';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Accounts', 'COLUMN', N'FieldMetadata';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[AccountCodes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(8) NULL,
        [AccountId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountCodes_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_AccountCodes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountCodes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountCodes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AccountCodes';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SecurityRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SecurityRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityRoles_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoles_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoles_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityRoles';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[AccountRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AutoProvisionSecurityRoleId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExternalRole] nvarchar(100) NULL,
        [IsPublic] bit NULL,
        [LocalizedName1030] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountRoles_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountRoles_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountRoles_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountRoles_SecurityRoles_AutoProvisionSecurityRoleId] FOREIGN KEY ([AutoProvisionSecurityRoleId]) REFERENCES [tests].[SecurityRoles] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AccountRoles';
    SET @description = N'Controls if external users may use / assign this role';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AccountRoles', 'COLUMN', N'IsPublic';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Contacts] (
        [Id] uniqueidentifier NOT NULL,
        [AllowNotifications] bit NULL,
        [Email] nvarchar(100) NULL,
        [FieldMetadata] nvarchar(max) NULL,
        [NemLoginRID] nvarchar(100) NULL,
        [Phone] nvarchar(100) NULL,
        [Status] int NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Contacts';
    SET @description = N'Metadata field for the info about which fields are locked';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Contacts', 'COLUMN', N'FieldMetadata';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[AccountRoleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AccountId] uniqueidentifier NULL,
        [ContactId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountRoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_AccountRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [tests].[AccountRoles] ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [tests].[Contacts] ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_AccountRoleAssignments_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AccountRoleAssignments';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Servers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Heartbeat] datetime2 NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Servers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Servers_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Servers_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Servers_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Servers';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[EnvironmentVariables] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [ApplicationName] nvarchar(100) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [ServerId] uniqueidentifier NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_EnvironmentVariables] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EnvironmentVariables_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_EnvironmentVariables_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_EnvironmentVariables_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_EnvironmentVariables_Servers_ServerId] FOREIGN KEY ([ServerId]) REFERENCES [tests].[Servers] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'EnvironmentVariables';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Documents] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Compressed] bit NULL,
        [Container] nvarchar(100) NULL,
        [ContentType] nvarchar(100) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Data] varbinary(max) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Path] nvarchar(512) NULL,
        [RowVersion] rowversion NOT NULL,
        [Size] int NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documents_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Documents_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Documents_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Documents';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SystemUsers] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(100) NULL,
        CONSTRAINT [PK_SystemUsers] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SystemUsers';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SecurityGroups] (
        [Id] uniqueidentifier NOT NULL,
        [ExternalId] nvarchar(100) NULL,
        [IsBusinessUnit] bit NULL,
        CONSTRAINT [PK_SecurityGroups] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityGroups';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ExternalId] ON [tests].[SecurityGroups] ([ExternalId]) WHERE [ExternalId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Permissions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Permissions_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Permissions_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Permissions_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Permissions';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Name] ON [tests].[Permissions] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SecurityRolePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [SecurityRoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SecurityRolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityRolePermissions_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRolePermissions_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRolePermissions_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [tests].[Permissions] ([Id]),
        CONSTRAINT [FK_SecurityRolePermissions_SecurityRoles_SecurityRoleId] FOREIGN KEY ([SecurityRoleId]) REFERENCES [tests].[SecurityRoles] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityRolePermissions';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE INDEX [IX_PermissionId] ON [tests].[SecurityRolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RolePermission] ON [tests].[SecurityRolePermissions] ([SecurityRoleId], [PermissionId]) WHERE [SecurityRoleId] IS NOT NULL AND [PermissionId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SecurityRoleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [IdentityId] uniqueidentifier NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [SecurityRoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SecurityRoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityRoleAssignments_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoleAssignments_Identities_IdentityId] FOREIGN KEY ([IdentityId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoleAssignments_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoleAssignments_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityRoleAssignments_SecurityRoles_SecurityRoleId] FOREIGN KEY ([SecurityRoleId]) REFERENCES [tests].[SecurityRoles] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityRoleAssignments';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[SecurityRoleAssignments] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_IdentityRole] ON [tests].[SecurityRoleAssignments] ([SecurityRoleId], [IdentityId]) WHERE [SecurityRoleId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[SecurityGroupMembers] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [IdentityId] uniqueidentifier NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [SecurityGroupId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SecurityGroupMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityGroupMembers_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityGroupMembers_Identities_IdentityId] FOREIGN KEY ([IdentityId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityGroupMembers_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityGroupMembers_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SecurityGroupMembers_SecurityGroups_SecurityGroupId] FOREIGN KEY ([SecurityGroupId]) REFERENCES [tests].[SecurityGroups] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityGroupMembers';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[SecurityGroupMembers] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SecurityGroupIdentity] ON [tests].[SecurityGroupMembers] ([SecurityGroupId], [IdentityId]) WHERE [SecurityGroupId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[RecordShares] (
        [Id] uniqueidentifier NOT NULL,
        [Identity] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [RecordId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RecordShares] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecordShares_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_RecordShares_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_RecordShares_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_RecordShares_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [tests].[Permissions] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'RecordShares';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE INDEX [IX_PermissionId] ON [tests].[RecordShares] ([PermissionId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Signins] (
        [Id] uniqueidentifier NOT NULL,
        [Claims] nvarchar(max) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [IdentityId] uniqueidentifier NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Properties] nvarchar(max) NULL,
        [Provider] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [SessionId] nvarchar(64) NULL,
        [Status] int NULL,
        CONSTRAINT [PK_Signins] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Signins_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Signins_Identities_IdentityId] FOREIGN KEY ([IdentityId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Signins_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Signins_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Signins';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[Signins] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[Workflows] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ManifestId] uniqueidentifier NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [Version] nvarchar(100) NULL,
        CONSTRAINT [PK_Workflows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Workflows_Documents_ManifestId] FOREIGN KEY ([ManifestId]) REFERENCES [tests].[Documents] ([Id]),
        CONSTRAINT [FK_Workflows_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Workflows_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Workflows_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Workflows';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    CREATE TABLE [tests].[WorkflowRuns] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [State] varbinary(max) NULL,
        CONSTRAINT [PK_WorkflowRuns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowRuns_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_WorkflowRuns_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_WorkflowRuns_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'WorkflowRuns';
END;
GO

IF NOT EXISTS(SELECT * FROM [tests].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_0')
BEGIN
    INSERT INTO [tests].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_0', N'{{VERSION}}');
END;
GO

COMMIT;
GO

