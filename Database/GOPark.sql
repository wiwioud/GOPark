IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE TABLE [ParkingSpaces] (
        [Id] int NOT NULL IDENTITY,
        [SpaceNumber] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ParkingSpaces] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] int NOT NULL IDENTITY,
        [LicensePlate] nvarchar(450) NOT NULL,
        [VehicleType] nvarchar(max) NOT NULL,
        [OwnerName] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE TABLE [ParkingSessions] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [ParkingSpaceId] int NOT NULL,
        [EntryTime] datetime2 NOT NULL,
        [ExitTime] datetime2 NULL,
        [ParkingFee] decimal(18,2) NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ParkingSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ParkingSessions_ParkingSpaces_ParkingSpaceId] FOREIGN KEY ([ParkingSpaceId]) REFERENCES [ParkingSpaces] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ParkingSessions_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE TABLE [Reservations] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [ParkingSpaceId] int NOT NULL,
        [ReservationDate] datetime2 NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reservations_ParkingSpaces_ParkingSpaceId] FOREIGN KEY ([ParkingSpaceId]) REFERENCES [ParkingSpaces] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reservations_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ParkingSessions_ParkingSpaceId] ON [ParkingSessions] ([ParkingSpaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ParkingSessions_VehicleId] ON [ParkingSessions] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reservations_ParkingSpaceId] ON [Reservations] ([ParkingSpaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reservations_VehicleId] ON [Reservations] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vehicles_LicensePlate] ON [Vehicles] ([LicensePlate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821135538_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260821135538_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822121453_AddAuditAndSettings'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityType] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822121453_AddAuditAndSettings'
)
BEGIN
    CREATE TABLE [ParkingRateSettings] (
        [Id] int NOT NULL IDENTITY,
        [HourlyRate] decimal(18,2) NOT NULL,
        [MinimumCharge] decimal(18,2) NOT NULL,
        [GracePeriodMinutes] int NOT NULL,
        [MaximumDailyRate] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ParkingRateSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822121453_AddAuditAndSettings'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822121453_AddAuditAndSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822121453_AddAuditAndSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [Color] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [Make] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [Model] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [Notes] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [OwnerPhone] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [Reservations] ADD [Notes] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [ParkingSpaces] ADD [Type] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    ALTER TABLE [ParkingSpaces] ADD [Zone] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822133448_AddNewFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822133448_AddNewFields', N'8.0.8');
END;
GO

COMMIT;
GO

