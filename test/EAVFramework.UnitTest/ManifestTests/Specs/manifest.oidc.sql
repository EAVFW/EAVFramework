IF OBJECT_ID(N'[oidc].[__MigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'oidc') IS NULL EXEC(N'CREATE SCHEMA [oidc];');
    CREATE TABLE [oidc].[__MigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___MigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectClients] (
        [AbsoluteRefreshTokenLifetime] int NULL,
        [AccessTokenLifetime] int NULL,
        [AccessTokenType] int NULL,
        [AllowAccessTokensViaBrowser] bit NULL,
        [AllowedCORSOrigins] nvarchar(max) NULL,
        [AllowedScopes] nvarchar(max) NULL,
        [AllowOfflineAccess] bit NULL,
        [AllowPlainTextPKCE] bit NULL,
        [AllowRememberConsent] bit NULL,
        [AlwaysIncludeUserClaimsInIdToken] bit NULL,
        [AlwaysSendClientClaims] bit NULL,
        [AuthorizationCodeLifetime] int NULL,
        [BackChannelLogoutSessionRequired] bit NULL,
        [BackChannelLogoutURI] nvarchar(100) NULL,
        [Claims] nvarchar(max) NULL,
        [ClientCLaimsPrefix] nvarchar(100) NULL,
        [ClientId] nvarchar(100) NULL,
        [ClientSecret] nvarchar(100) NULL,
        [ClientUri] nvarchar(100) NULL,
        [ConsentLifetime] int NULL,
        [ConsentType] int NULL,
        [Description] nvarchar(max) NULL,
        [DeviceCodeLifetime] int NULL,
        [EnableLocalLogin] bit NULL,
        [FrontChannelLogoutSessionRequired] bit NULL,
        [FrontChannelLogoutURI] nvarchar(100) NULL,
        [IdentityProviderRestrictions] nvarchar(max) NULL,
        [IdentityTokenLifetime] int NULL,
        [IncludeJWTId] bit NULL,
        [LogoUri] nvarchar(100) NULL,
        [PairWiseSubjectSalt] nvarchar(100) NULL,
        [PostLogoutRedirectURIs] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [ProtocolType] int NULL,
        [RedirectUris] nvarchar(max) NULL,
        [RefreshTokenExpiration] int NULL,
        [RefreshTokenUsage] int NULL,
        [RequireClientSecret] bit NULL,
        [RequireConsent] bit NULL,
        [RequirePKCE] bit NULL,
        [RequireRequestObject] bit NULL,
        [SlidingRefreshTokenLifetime] int NULL,
        [Status] int NULL,
        [Type] int NOT NULL,
        [UpdateAccessTokenClaimsOnRefresh] bit NULL,
        [UserCodeType] nvarchar(100) NULL,
        [UserSSOLifetime] int NULL
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients';
    SET @description = N'Maximum lifetime of a refresh token in secords';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AbsoluteRefreshTokenLifetime';
    SET @description = N'Lifetime of access token in seconds';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AccessTokenLifetime';
    SET @description = N'Specifies wheter the access token is a reference token or a self contained JWT token';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AccessTokenType';
    SET @description = N'Controls whether accesss tokens are transmitted via the browser for this client. This can prevent accidential leakage of access tokens when multiple resposne types are allowed.';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AllowAccessTokensViaBrowser';
    SET @description = N'Specifies the allowed CORS origins for JavaScript clients';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AllowedCORSOrigins';
    SET @description = N'Specifies the API scopes that the client is allowed to request, if empty, the client cant access any scopes';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AllowedScopes';
    SET @description = N'Specifies whether a proof key can be sent using plain method (not recommended)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AllowPlainTextPKCE';
    SET @description = N'Specifies whether user can choose to store consent decisions';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AllowRememberConsent';
    SET @description = N'When requesting both an id token and access token, should the user claims always be added to the id token instead of requiring the client to use the userinfo endpoint';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AlwaysIncludeUserClaimsInIdToken';
    SET @description = N'Specifies if Client Claims always should be included in accesss token, or only for client credentials flow';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AlwaysSendClientClaims';
    SET @description = N'Lifetime of authorization code in secords';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'AuthorizationCodeLifetime';
    SET @description = N'Specifies if the users sessions id should be sent to BackChannelLogoutUri';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'BackChannelLogoutSessionRequired';
    SET @description = N'Specifies logout URI at client for HTTP back-channel based logout.';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'BackChannelLogoutURI';
    SET @description = N'Specifies claims that is included in the access token for the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'Claims';
    SET @description = N'Specifies a value to prefix client claim types';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ClientCLaimsPrefix';
    SET @description = N'Unique ID of the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ClientId';
    SET @description = N'Unique ID of the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ClientSecret';
    SET @description = N'URI to further information about the client (used on consent screen)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ClientUri';
    SET @description = N'Lifetime of a user consent in seconds (defaults no expiration)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ConsentLifetime';
    SET @description = N'The description of the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'Description';
    SET @description = N'The device code lifetime';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'DeviceCodeLifetime';
    SET @description = N'Specifies if local login is enabled for this client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'EnableLocalLogin';
    SET @description = N'Specifies if the users sessions id should be sent to FrontChannelLogoutUri';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'FrontChannelLogoutSessionRequired';
    SET @description = N'Specifies logout URI at client for HTTP front-channel based logout';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'FrontChannelLogoutURI';
    SET @description = N'Specifies which external IdPs can be used with this client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'IdentityProviderRestrictions';
    SET @description = N'Lifetime of the identity token in seconds';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'IdentityTokenLifetime';
    SET @description = N'Specifies if JWT access token should include an identifier';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'IncludeJWTId';
    SET @description = N'URI to client logo (used on consent screen)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'LogoUri';
    SET @description = N'Specifies a salt value used in pair-wise subjectId generation for users of this client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'PairWiseSubjectSalt';
    SET @description = N'Specifies allowed URIs to redirect to after logout';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'PostLogoutRedirectURIs';
    SET @description = N'Specifies custom properties for the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'Properties';
    SET @description = N'The protocol type';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'ProtocolType';
    SET @description = N'Specifies allowed URIs to return tokens or authorization codes to';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RedirectUris';
    SET @description = N'Specifies if using absolute expiration or sliding expiration';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RefreshTokenExpiration';
    SET @description = N'Specify if the refresh handle will be updated when refreshing tokens';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RefreshTokenUsage';
    SET @description = N'If set to false, no client secret is needed to request tokens at the token endpoint';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RequireClientSecret';
    SET @description = N'Specifies whether a consent screen is required';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RequireConsent';
    SET @description = N'Specifies whether a proof key is required for authorization code based token requests';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RequirePKCE';
    SET @description = N'Specifies whether the client must use a request object on authorization requests';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'RequireRequestObject';
    SET @description = N'Sliding lifetime of a refresh token in secords';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'SlidingRefreshTokenLifetime';
    SET @description = N'Specifies if the client is enabled';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'Status';
    SET @description = N'Specifies if access token and claims should be updated on a refresh token request';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'UpdateAccessTokenClaimsOnRefresh';
    SET @description = N'Specifies the type of device flow user code';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'UserCodeType';
    SET @description = N'The maximum duration (in seconds) since the last time the user authenticated (defaults no expiration)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectClients', 'COLUMN', N'UserSSOLifetime';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [Client Id] ON [tests].[OpenIdConnectClients] ([ClientId]) WHERE [ClientId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    CREATE TABLE [tests].[SystemUsers] (
        [Email] nvarchar(100) NULL
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SystemUsers';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    CREATE TABLE [tests].[SecurityGroups] (
        [ExternalId] nvarchar(100) NULL,
        [IsBusinessUnit] bit NULL
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SecurityGroups';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ExternalId] ON [tests].[SecurityGroups] ([ExternalId]) WHERE [ExternalId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Name] ON [tests].[Permissions] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RolePermission] ON [tests].[SecurityRolePermissions] ([SecurityRoleId], [PermissionId]) WHERE [SecurityRoleId] IS NOT NULL AND [PermissionId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_IdentityRole] ON [tests].[SecurityRoleAssignments] ([SecurityRoleId], [IdentityId]) WHERE [SecurityRoleId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SecurityGroupIdentity] ON [tests].[SecurityGroupMembers] ([SecurityGroupId], [IdentityId]) WHERE [SecurityGroupId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    CREATE TABLE [tests].[AllowedGrantTypes] (
        [Id] uniqueidentifier NOT NULL,
        [AllowedGrantTypeValue] int NULL,
        [OpenIdConnectClientId] uniqueidentifier NULL,
        CONSTRAINT [PK_AllowedGrantTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AllowedGrantTypes_OpenIdConnectClients_OpenIdConnectClientId] FOREIGN KEY ([OpenIdConnectClientId]) REFERENCES [tests].[OpenIdConnectClients] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AllowedGrantTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenId Connect ClientValue] ON [tests].[AllowedGrantTypes] ([OpenIdConnectClientId], [AllowedGrantTypeValue]) WHERE [OpenIdConnectClientId] IS NOT NULL AND [AllowedGrantTypeValue] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    CREATE TABLE [tests].[AllowedIdentityTokenSigningAlgorithm] (
        [Id] uniqueidentifier NOT NULL,
        [AllowedIdentityTokenSigningAlgorithmValue] int NULL,
        [OpenIdConnectClientId] uniqueidentifier NULL,
        CONSTRAINT [PK_AllowedIdentityTokenSigningAlgorithm] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AllowedIdentityTokenSigningAlgorithm_OpenIdConnectClients_OpenIdConnectClientId] FOREIGN KEY ([OpenIdConnectClientId]) REFERENCES [tests].[OpenIdConnectClients] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'AllowedIdentityTokenSigningAlgorithm';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenId Connect ClientValue] ON [tests].[AllowedIdentityTokenSigningAlgorithm] ([OpenIdConnectClientId], [AllowedIdentityTokenSigningAlgorithmValue]) WHERE [OpenIdConnectClientId] IS NOT NULL AND [AllowedIdentityTokenSigningAlgorithmValue] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_8')
BEGIN
    INSERT INTO [oidc].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_8', N'5.0.15');
END;
GO

COMMIT;
GO

