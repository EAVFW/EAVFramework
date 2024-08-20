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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectResources] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Description] nvarchar(255) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [Name] nvarchar(100) NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Properties] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [ShowInDiscoveryDocument] bit NULL,
        [UserClaims] nvarchar(max) NULL,
        CONSTRAINT [PK_OpenIdConnectResources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectResources_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectResources_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectResources_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources';
    SET @description = N'The displayname of the resource';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'DisplayName';
    SET @description = N'The description of the resource';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'Description';
    SET @description = N'The unique name of the resource';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'Name';
    SET @description = N'Specifies custom properties for the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'Properties';
    SET @description = N'Specifies whether this scope is shown in the discovery document';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'ShowInDiscoveryDocument';
    SET @description = N'List of associated user claims that should be included when this resource is requested';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectResources', 'COLUMN', N'UserClaims';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Name] ON [tests].[OpenIdConnectResources] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectScopes] (
        [Id] uniqueidentifier NOT NULL,
        [Emphasize] bit NULL,
        [Required] bit NULL,
        CONSTRAINT [PK_OpenIdConnectScopes] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectScopes';
    SET @description = N'Specifies whether the consent screen will emphasize this scope. Used for sensitive or important scopes';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectScopes', 'COLUMN', N'Emphasize';
    SET @description = N'Specifies whether the user can de-select the scope on the consent screen';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectScopes', 'COLUMN', N'Required';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectIdentityResources] (
        [Id] uniqueidentifier NOT NULL,
        [Emphasize] bit NULL,
        [Required] bit NULL,
        CONSTRAINT [PK_OpenIdConnectIdentityResources] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectIdentityResources';
    SET @description = N'Specifies whether the consent screen will emphasize this scope. Used for sensitive or important scopes';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectIdentityResources', 'COLUMN', N'Emphasize';
    SET @description = N'Specifies whether the user can de-select the scope on the consent screen';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectIdentityResources', 'COLUMN', N'Required';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectAPIResources] (
        [Id] uniqueidentifier NOT NULL,
        [Emphasize] bit NULL,
        [Required] bit NULL,
        [RequireResourceIndicator] bit NULL,
        CONSTRAINT [PK_OpenIdConnectAPIResources] PRIMARY KEY ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAPIResources';
    SET @description = N'Specifies whether the consent screen will emphasize this scope. Used for sensitive or important scopes';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAPIResources', 'COLUMN', N'Emphasize';
    SET @description = N'Specifies whether the user can de-select the scope on the consent screen';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAPIResources', 'COLUMN', N'Required';
    SET @description = N'Indicates if this API resource requires the resource indicator to resquest it, and expects access tokens issues to it will only ever contain this API resource as the audience.';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAPIResources', 'COLUMN', N'RequireResourceIndicator';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectClients] (
        [Id] uniqueidentifier NOT NULL,
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
        [UserSSOLifetime] int NULL,
        CONSTRAINT [PK_OpenIdConnectClients] PRIMARY KEY ([Id])
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [Client Id] ON [tests].[OpenIdConnectClients] ([ClientId]) WHERE [ClientId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectSecrets] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(255) NULL,
        [ApiId] uniqueidentifier NULL,
        [ClientId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Expiration] datetime2 NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [SecretTypeCode] int NULL,
        [Value] nvarchar(100) NULL,
        [ValueHint] nvarchar(100) NULL,
        CONSTRAINT [PK_OpenIdConnectSecrets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectSecrets_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectSecrets_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectSecrets_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectSecrets_OpenIdConnectAPIResources_ApiId] FOREIGN KEY ([ApiId]) REFERENCES [tests].[OpenIdConnectAPIResources] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpenIdConnectSecrets_OpenIdConnectClients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [tests].[OpenIdConnectClients] ([Id]) ON DELETE NO ACTION
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets';
    SET @description = N'The API Resource that this secret belongs to';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'ApiId';
    SET @description = N'The OAuth Client that this secret belongs to';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'ClientId';
    SET @description = N'The expiration';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'Expiration';
    SET @description = N'The type of the secret';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'SecretTypeCode';
    SET @description = N'The value';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'Value';
    SET @description = N'The hint for UI';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectSecrets', 'COLUMN', N'ValueHint';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_APISecret] ON [tests].[OpenIdConnectSecrets] ([ApiId], [Value]) WHERE [ApiId] IS NOT NULL AND [Value] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ClientSecret] ON [tests].[OpenIdConnectSecrets] ([ClientId], [Value]) WHERE [ClientId] IS NOT NULL AND [Value] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectAuthorizations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [ClientId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Properties] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        CONSTRAINT [PK_OpenIdConnectAuthorizations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizations_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizations_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizations_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizations_Identities_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizations_OpenIdConnectClients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [tests].[OpenIdConnectClients] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAuthorizations';
    SET @description = N'Specifies custom properties for the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAuthorizations', 'COLUMN', N'Properties';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_SubjectId] ON [tests].[OpenIdConnectAuthorizations] ([SubjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_ClientId] ON [tests].[OpenIdConnectAuthorizations] ([ClientId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectTokens] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AuthorizationId] uniqueidentifier NULL,
        [ClientId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExpirationDate] datetime2 NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Payload] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [RedemptionDate] datetime2 NULL,
        [ReferenceId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        CONSTRAINT [PK_OpenIdConnectTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_Identities_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_OpenIdConnectAuthorizations_AuthorizationId] FOREIGN KEY ([AuthorizationId]) REFERENCES [tests].[OpenIdConnectAuthorizations] ([Id]),
        CONSTRAINT [FK_OpenIdConnectTokens_OpenIdConnectClients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [tests].[OpenIdConnectClients] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectTokens';
    SET @description = N'Specifies custom properties for the client';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectTokens', 'COLUMN', N'Properties';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_SubjectId] ON [tests].[OpenIdConnectTokens] ([SubjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ReferenceId] ON [tests].[OpenIdConnectTokens] ([ReferenceId]) WHERE [ReferenceId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectAuthorizationScopes] (
        [Id] uniqueidentifier NOT NULL,
        [AuthorizationId] uniqueidentifier NOT NULL,
        [ScopeId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_OpenIdConnectAuthorizationScopes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizationScopes_OpenIdConnectAuthorizations_AuthorizationId] FOREIGN KEY ([AuthorizationId]) REFERENCES [tests].[OpenIdConnectAuthorizations] ([Id]),
        CONSTRAINT [FK_OpenIdConnectAuthorizationScopes_OpenIdConnectIdentityResources_ScopeId] FOREIGN KEY ([ScopeId]) REFERENCES [tests].[OpenIdConnectIdentityResources] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectAuthorizationScopes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_ScopeId] ON [tests].[OpenIdConnectAuthorizationScopes] ([ScopeId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AuthorizationScope] ON [tests].[OpenIdConnectAuthorizationScopes] ([AuthorizationId], [ScopeId]) WHERE [AuthorizationId] IS NOT NULL AND [ScopeId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[OpenIdConnectScopeResources] (
        [Id] uniqueidentifier NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [ScopeId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_OpenIdConnectScopeResources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIdConnectScopeResources_OpenIdConnectIdentityResources_ScopeId] FOREIGN KEY ([ScopeId]) REFERENCES [tests].[OpenIdConnectIdentityResources] ([Id]),
        CONSTRAINT [FK_OpenIdConnectScopeResources_OpenIdConnectResources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [tests].[OpenIdConnectResources] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'OpenIdConnectScopeResources';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_ResourceId] ON [tests].[OpenIdConnectScopeResources] ([ResourceId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ScopeResource] ON [tests].[OpenIdConnectScopeResources] ([ScopeId], [ResourceId]) WHERE [ScopeId] IS NOT NULL AND [ResourceId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[QuickForms] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [QuickFormDefinitionId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Slug] nvarchar(100) NULL,
        [Status] int NULL,
        [Version] nvarchar(100) NULL,
        CONSTRAINT [PK_QuickForms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuickForms_Documents_QuickFormDefinitionId] FOREIGN KEY ([QuickFormDefinitionId]) REFERENCES [tests].[Documents] ([Id]),
        CONSTRAINT [FK_QuickForms_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_QuickForms_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_QuickForms_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'QuickForms';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[UserAgents] (
        [Id] uniqueidentifier NOT NULL,
        [BrowserName] nvarchar(255) NULL,
        [Value] nvarchar(max) NULL,
        [BrowserMajor] nvarchar(8) NULL,
        [BrowserVersion] nvarchar(16) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [DeviceName] nvarchar(100) NULL,
        [DeviceVersion] nvarchar(16) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OSName] nvarchar(100) NULL,
        [OSVersion] nvarchar(16) NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserAgents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAgents_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_UserAgents_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_UserAgents_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'UserAgents';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[QuickFormAnswers] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(255) NULL,
        [AnswerType] nvarchar(100) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Encoding] nvarchar(16) NULL,
        [ExternalId] nvarchar(100) NULL,
        [FilledFromGeoCity] nvarchar(100) NULL,
        [FilledFromGeoCountry] nvarchar(3) NULL,
        [FilledFromGeoLatitude] decimal(18,4) NULL,
        [FilledFromGeoLongitude] decimal(18,4) NULL,
        [FilledFromGeoRegion] nvarchar(10) NULL,
        [FilledFromIP] nvarchar(39) NULL,
        [FilledFromUserAgentId] uniqueidentifier NULL,
        [FilledOn] datetime2 NULL,
        [IsBot] bit NULL,
        [MessageType] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PayloadId] uniqueidentifier NULL,
        [ProcessedOn] datetime2 NULL,
        [QuickFormDefinitionId] uniqueidentifier NULL,
        [QuickformId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NULL,
        CONSTRAINT [PK_QuickFormAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_Documents_PayloadId] FOREIGN KEY ([PayloadId]) REFERENCES [tests].[Documents] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_Documents_QuickFormDefinitionId] FOREIGN KEY ([QuickFormDefinitionId]) REFERENCES [tests].[Documents] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_QuickForms_QuickformId] FOREIGN KEY ([QuickformId]) REFERENCES [tests].[QuickForms] ([Id]),
        CONSTRAINT [FK_QuickFormAnswers_UserAgents_FilledFromUserAgentId] FOREIGN KEY ([FilledFromUserAgentId]) REFERENCES [tests].[UserAgents] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'QuickFormAnswers';
    SET @description = N'A field for storing the IP that the data was filled from, either ipv4 or IPv6';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'QuickFormAnswers', 'COLUMN', N'FilledFromIP';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentSeries] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PublishNotAfter] datetime2 NULL,
        [PublishNotBefore] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [Slug] nvarchar(100) NULL,
        CONSTRAINT [PK_TournamentSeries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentSeries_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSeries_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSeries_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentSeries';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[ContactInformations] (
        [Id] uniqueidentifier NOT NULL,
        [Value] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RelationId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Type] int NULL,
        CONSTRAINT [PK_ContactInformations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContactInformations_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_ContactInformations_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_ContactInformations_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'ContactInformations';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Accounts] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [AccountTypeId] uniqueidentifier NULL,
        [AddressId] uniqueidentifier NULL,
        [BillingAddressId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EANCode] nvarchar(15) NULL,
        [ExternalId] nvarchar(100) NULL,
        [Homepage] nvarchar(255) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PrimaryCompanyCode] nvarchar(8) NULL,
        [PrimaryEmailId] uniqueidentifier NULL,
        [PrimaryLandlinePhoneId] uniqueidentifier NULL,
        [PrimaryMobilePhoneId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accounts_AccountTypes_AccountTypeId] FOREIGN KEY ([AccountTypeId]) REFERENCES [tests].[AccountTypes] ([Id]),
        CONSTRAINT [FK_Accounts_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [tests].[Addresses] ([Id]),
        CONSTRAINT [FK_Accounts_Addresses_BillingAddressId] FOREIGN KEY ([BillingAddressId]) REFERENCES [tests].[Addresses] ([Id]),
        CONSTRAINT [FK_Accounts_ContactInformations_PrimaryEmailId] FOREIGN KEY ([PrimaryEmailId]) REFERENCES [tests].[ContactInformations] ([Id]),
        CONSTRAINT [FK_Accounts_ContactInformations_PrimaryLandlinePhoneId] FOREIGN KEY ([PrimaryLandlinePhoneId]) REFERENCES [tests].[ContactInformations] ([Id]),
        CONSTRAINT [FK_Accounts_ContactInformations_PrimaryMobilePhoneId] FOREIGN KEY ([PrimaryMobilePhoneId]) REFERENCES [tests].[ContactInformations] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Accounts_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Accounts';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Venues] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [LocationId] uniqueidentifier NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Venues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Venues_Accounts_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_Venues_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Venues_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Venues_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Venues';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Tournaments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PublishNotAfter] datetime2 NULL,
        [PublishNotBefore] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [Slug] nvarchar(256) NULL,
        [StartTime] datetime2 NULL,
        [TournamentSerieId] uniqueidentifier NULL,
        [VenueId] uniqueidentifier NULL,
        CONSTRAINT [PK_Tournaments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tournaments_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Tournaments_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Tournaments_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Tournaments_TournamentSeries_TournamentSerieId] FOREIGN KEY ([TournamentSerieId]) REFERENCES [tests].[TournamentSeries] ([Id]),
        CONSTRAINT [FK_Tournaments_Venues_VenueId] FOREIGN KEY ([VenueId]) REFERENCES [tests].[Venues] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Tournaments';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentClassTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TournamentClassTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentClassTypes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentClassTypes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentClassTypes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentClassTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentClasses] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PublishNotAfter] datetime2 NULL,
        [PublishNotBefore] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [Slug] nvarchar(256) NULL,
        [StartTime] datetime2 NULL,
        [TournamentId] uniqueidentifier NULL,
        [TypeId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentClasses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentClasses_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentClasses_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentClasses_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentClasses_TournamentClassTypes_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [tests].[TournamentClassTypes] ([Id]),
        CONSTRAINT [FK_TournamentClasses_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentClasses';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Contacts] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Birthday] datetime2 NULL,
        [FirstLogon] datetime2 NULL,
        [LastLogon] datetime2 NULL,
        [Password] nvarchar(100) NULL,
        [PrimaryEmailId] uniqueidentifier NULL,
        [PrimaryLandlinePhoneId] uniqueidentifier NULL,
        [PrimaryMobilePhoneId] uniqueidentifier NULL,
        [ShouldResetPassword] bit NULL,
        [Status] int NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contacts_ContactInformations_PrimaryEmailId] FOREIGN KEY ([PrimaryEmailId]) REFERENCES [tests].[ContactInformations] ([Id]),
        CONSTRAINT [FK_Contacts_ContactInformations_PrimaryLandlinePhoneId] FOREIGN KEY ([PrimaryLandlinePhoneId]) REFERENCES [tests].[ContactInformations] ([Id]),
        CONSTRAINT [FK_Contacts_ContactInformations_PrimaryMobilePhoneId] FOREIGN KEY ([PrimaryMobilePhoneId]) REFERENCES [tests].[ContactInformations] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Contacts';
    SET @description = N'The primary email used for communicating with the contact';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Contacts', 'COLUMN', N'PrimaryEmailId';
    SET @description = N'The status of the contact - inactive contacts are not used, nor showed in lists';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Contacts', 'COLUMN', N'Status';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentParticipantPurposes] (
        [Id] uniqueidentifier NOT NULL,
        [Purpose] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TournamentParticipantPurposes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentParticipantPurposes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPurposes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPurposes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentParticipantPurposes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentTeams] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TournamentTeams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentTeams_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentTeams_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentTeams_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentTeams';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentParticipants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [ClassId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EntryReason] int NULL,
        [EntryTime] datetime2 NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PlayerId] uniqueidentifier NULL,
        [PurposeId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Status] int NULL,
        [TeamId] uniqueidentifier NULL,
        [TournamentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentParticipants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentParticipants_Contacts_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [tests].[Contacts] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_TournamentClasses_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [tests].[TournamentClasses] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_TournamentParticipantPurposes_PurposeId] FOREIGN KEY ([PurposeId]) REFERENCES [tests].[TournamentParticipantPurposes] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id]),
        CONSTRAINT [FK_TournamentParticipants_TournamentTeams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [tests].[TournamentTeams] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentParticipants';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentSignups] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayValue] nvarchar(255) NULL,
        [AnswerId] uniqueidentifier NULL,
        [TournamentId] uniqueidentifier NULL,
        [TournamentParticipantId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentSignups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentSignups_QuickFormAnswers_AnswerId] FOREIGN KEY ([AnswerId]) REFERENCES [tests].[QuickFormAnswers] ([Id]),
        CONSTRAINT [FK_TournamentSignups_TournamentParticipants_TournamentParticipantId] FOREIGN KEY ([TournamentParticipantId]) REFERENCES [tests].[TournamentParticipants] ([Id]),
        CONSTRAINT [FK_TournamentSignups_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentSignups';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Courts] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [Size] int NULL,
        [Type] int NULL,
        [VenueId] uniqueidentifier NULL,
        CONSTRAINT [PK_Courts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Courts_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Courts_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Courts_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Courts_Venues_VenueId] FOREIGN KEY ([VenueId]) REFERENCES [tests].[Venues] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Courts';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[SponsorTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SponsorTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SponsorTypes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SponsorTypes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_SponsorTypes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'SponsorTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentSerieSponsors] (
        [Id] uniqueidentifier NOT NULL,
        [PublicName] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PublishNotAfter] datetime2 NULL,
        [PublishNotBefore] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [SerieId] uniqueidentifier NULL,
        [SponsorId] uniqueidentifier NULL,
        [SponsorTypeId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentSerieSponsors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_Accounts_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_SponsorTypes_SponsorTypeId] FOREIGN KEY ([SponsorTypeId]) REFERENCES [tests].[SponsorTypes] ([Id]),
        CONSTRAINT [FK_TournamentSerieSponsors_TournamentSeries_SerieId] FOREIGN KEY ([SerieId]) REFERENCES [tests].[TournamentSeries] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentSerieSponsors';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentEntryFees] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Amount] decimal(18,4) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [DiscountAmount] decimal(18,4) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [TeamFee] bit NULL,
        [TournamentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentEntryFees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentEntryFees_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentEntryFees_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentEntryFees_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentEntryFees_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentEntryFees';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentSponsors] (
        [Id] uniqueidentifier NOT NULL,
        [PublicName] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PublishNotAfter] datetime2 NULL,
        [PublishNotBefore] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [SponsorId] uniqueidentifier NULL,
        [SponsorTypeId] uniqueidentifier NULL,
        [TournamentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentSponsors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentSponsors_Accounts_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_TournamentSponsors_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsors_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsors_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsors_SponsorTypes_SponsorTypeId] FOREIGN KEY ([SponsorTypeId]) REFERENCES [tests].[SponsorTypes] ([Id]),
        CONSTRAINT [FK_TournamentSponsors_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentSponsors';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentSponsorItems] (
        [Id] uniqueidentifier NOT NULL,
        [Value] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RegardingId] uniqueidentifier NULL,
        [RegardingType] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [SponsorId] uniqueidentifier NULL,
        [Status] int NULL,
        CONSTRAINT [PK_TournamentSponsorItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentSponsorItems_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsorItems_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsorItems_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentSponsorItems_TournamentSponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [tests].[TournamentSponsors] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentSponsorItems';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TournamentTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentTypes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentTypes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentTypes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentTypes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentParticipantPayments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PaymentType] int NULL,
        [RowVersion] rowversion NOT NULL,
        [TournamentEntryFeeId] uniqueidentifier NULL,
        [TournamentParticipantId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentParticipantPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentParticipantPayments_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPayments_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPayments_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPayments_TournamentEntryFees_TournamentEntryFeeId] FOREIGN KEY ([TournamentEntryFeeId]) REFERENCES [tests].[TournamentEntryFees] ([Id]),
        CONSTRAINT [FK_TournamentParticipantPayments_TournamentParticipants_TournamentParticipantId] FOREIGN KEY ([TournamentParticipantId]) REFERENCES [tests].[TournamentParticipants] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentParticipantPayments';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentCourts] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [ExternalId] nvarchar(100) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [StartTime] datetime2 NULL,
        [TournamentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentCourts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentCourts_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentCourts_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentCourts_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentCourts_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentCourts';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Prizes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [Amount] decimal(18,4) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [ExternalId] nvarchar(100) NULL,
        [ExternalUrl] nvarchar(256) NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [SponsorId] uniqueidentifier NULL,
        CONSTRAINT [PK_Prizes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prizes_Accounts_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_Prizes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Prizes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Prizes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Prizes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[TournamentPrizes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [PrizeId] uniqueidentifier NULL,
        [Quantity] decimal(18,4) NULL,
        [RowVersion] rowversion NOT NULL,
        [TournamentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TournamentPrizes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TournamentPrizes_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentPrizes_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentPrizes_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_TournamentPrizes_Prizes_PrizeId] FOREIGN KEY ([PrizeId]) REFERENCES [tests].[Prizes] ([Id]),
        CONSTRAINT [FK_TournamentPrizes_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [tests].[Tournaments] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'TournamentPrizes';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[Websites] (
        [Id] uniqueidentifier NOT NULL,
        [Domain] nvarchar(255) NULL,
        [AccountId] uniqueidentifier NULL,
        [CreatedById] uniqueidentifier NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ModifiedById] uniqueidentifier NOT NULL,
        [ModifiedOn] datetime2 NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Websites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Websites_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_Websites_Identities_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Websites_Identities_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [tests].[Identities] ([Id]),
        CONSTRAINT [FK_Websites_Identities_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [tests].[Identities] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Websites';
    SET @description = N'The homepage domain name / host where the customer can have quotation forms embeded';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'Websites', 'COLUMN', N'Domain';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ExternalId] ON [tests].[SecurityGroups] ([ExternalId]) WHERE [ExternalId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Name] ON [tests].[Permissions] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_PermissionId] ON [tests].[SecurityRolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RolePermission] ON [tests].[SecurityRolePermissions] ([SecurityRoleId], [PermissionId]) WHERE [SecurityRoleId] IS NOT NULL AND [PermissionId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[SecurityRoleAssignments] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_IdentityRole] ON [tests].[SecurityRoleAssignments] ([SecurityRoleId], [IdentityId]) WHERE [SecurityRoleId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[SecurityGroupMembers] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SecurityGroupIdentity] ON [tests].[SecurityGroupMembers] ([SecurityGroupId], [IdentityId]) WHERE [SecurityGroupId] IS NOT NULL AND [IdentityId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_PermissionId] ON [tests].[RecordShares] ([PermissionId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE INDEX [IX_IdentityId] ON [tests].[Signins] ([IdentityId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[ContactInformationContactReferences] (
        [Id] uniqueidentifier NOT NULL,
        [ContactId] uniqueidentifier NULL,
        [ContactInformationId] uniqueidentifier NULL,
        CONSTRAINT [PK_ContactInformationContactReferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContactInformationContactReferences_ContactInformations_ContactInformationId] FOREIGN KEY ([ContactInformationId]) REFERENCES [tests].[ContactInformations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ContactInformationContactReferences_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [tests].[Contacts] ([Id])
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'ContactInformationContactReferences';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ContactInformationId] ON [tests].[ContactInformationContactReferences] ([ContactInformationId]) WHERE [ContactInformationId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    CREATE TABLE [tests].[ContactInformationAccountReferences] (
        [Id] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NULL,
        [ContactInformationId] uniqueidentifier NULL,
        CONSTRAINT [PK_ContactInformationAccountReferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContactInformationAccountReferences_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [tests].[Accounts] ([Id]),
        CONSTRAINT [FK_ContactInformationAccountReferences_ContactInformations_ContactInformationId] FOREIGN KEY ([ContactInformationId]) REFERENCES [tests].[ContactInformations] ([Id]) ON DELETE CASCADE
    );
    DECLARE @description AS sql_variant;
    SET @description = N'comment';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', N'tests', 'TABLE', N'ContactInformationAccountReferences';
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ContactInformationId] ON [tests].[ContactInformationAccountReferences] ([ContactInformationId]) WHERE [ContactInformationId] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenId Connect ClientValue] ON [tests].[AllowedGrantTypes] ([OpenIdConnectClientId], [AllowedGrantTypeValue]) WHERE [OpenIdConnectClientId] IS NOT NULL AND [AllowedGrantTypeValue] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
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

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenId Connect ClientValue] ON [tests].[AllowedIdentityTokenSigningAlgorithm] ([OpenIdConnectClientId], [AllowedIdentityTokenSigningAlgorithmValue]) WHERE [OpenIdConnectClientId] IS NOT NULL AND [AllowedIdentityTokenSigningAlgorithmValue] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [oidc].[__MigrationsHistory] WHERE [MigrationId] = N'tests_1_0_2')
BEGIN
    INSERT INTO [oidc].[__MigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'tests_1_0_2',N'{{VERSION}}');
END;
GO

COMMIT;
GO

