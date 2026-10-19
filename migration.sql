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
CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);

CREATE TABLE [JobTitles] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_JobTitles] PRIMARY KEY ([Id])
);

CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(120) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PhoneNumber] nvarchar(30) NOT NULL,
    [HireDate] datetime2 NOT NULL,
    [Salary] decimal(18,2) NOT NULL,
    [DepartmentId] int NOT NULL,
    [JobTitleId] int NOT NULL,
    [ProfileImagePath] nvarchar(250) NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [JobTitles] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);

CREATE INDEX [IX_Employees_JobTitleId] ON [Employees] ([JobTitleId]);

CREATE TABLE [UserAccounts] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(120) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [EmployeeId] int NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [MustChangePassword] bit NOT NULL CONSTRAINT [DF_UserAccounts_MustChangePassword] DEFAULT (0),
    CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserAccounts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_UserAccounts_Email] ON [UserAccounts] ([Email]);
CREATE UNIQUE INDEX [IX_UserAccounts_EmployeeId] ON [UserAccounts] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;

CREATE TABLE [AttendanceRecords] (
    [Id] int NOT NULL IDENTITY, [EmployeeId] int NOT NULL, [WorkDate] date NOT NULL,
    [ClockIn] time NULL, [ClockOut] time NULL, [Notes] nvarchar(400) NULL,
    CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_WorkDate] ON [AttendanceRecords] ([EmployeeId], [WorkDate]);

CREATE TABLE [EmployeeTasks] (
    [Id] int NOT NULL IDENTITY, [EmployeeId] int NOT NULL, [Title] nvarchar(140) NOT NULL,
    [Description] nvarchar(1000) NULL, [DueDate] date NULL, [Status] nvarchar(24) NOT NULL,
    [AssignedAtUtc] datetime2 NOT NULL, [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_EmployeeTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeTasks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_EmployeeTasks_EmployeeId] ON [EmployeeTasks] ([EmployeeId]);

CREATE TABLE [PasswordResetTokens] (
    [Id] int NOT NULL IDENTITY, [UserAccountId] int NOT NULL, [TokenHash] nvarchar(64) NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL, [UsedAtUtc] datetime2 NULL, [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_UserAccounts_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
CREATE INDEX [IX_PasswordResetTokens_UserAccountId] ON [PasswordResetTokens] ([UserAccountId]);

CREATE TABLE [EmployeeChangeRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PhoneNumber] nvarchar(30) NOT NULL,
    [ProposedPasswordHash] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewedByUserAccountId] int NULL,
    [ReviewNote] nvarchar(400) NULL,
    CONSTRAINT [PK_EmployeeChangeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeChangeRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeChangeRequests_UserAccounts_ReviewedByUserAccountId] FOREIGN KEY ([ReviewedByUserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_EmployeeChangeRequests_EmployeeId] ON [EmployeeChangeRequests] ([EmployeeId]) WHERE [Status] = N'Pending';
CREATE INDEX [IX_EmployeeChangeRequests_EmployeeId_RequestedAtUtc] ON [EmployeeChangeRequests] ([EmployeeId], [RequestedAtUtc]);
CREATE INDEX [IX_EmployeeChangeRequests_ReviewedByUserAccountId] ON [EmployeeChangeRequests] ([ReviewedByUserAccountId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261009141234_InitialCreate', N'10.0.12');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019090000_AddUserAccounts', N'10.0.12');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019113000_AddAttendanceTasksAndPasswordReset', N'10.0.12');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019143000_AddEmployeeChangeRequests', N'10.0.12');

COMMIT;
GO

