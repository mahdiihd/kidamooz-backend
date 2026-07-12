IF OBJECT_ID(N'[app_opens_daily]', N'U') IS NULL
BEGIN
    CREATE TABLE [app_opens_daily] (
        [ViewDate] date NOT NULL,
        [OpenCount] int NOT NULL,
        CONSTRAINT [PK_app_opens_daily] PRIMARY KEY ([ViewDate])
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260711195252_AppOpensDaily'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260711195252_AppOpensDaily', N'9.0.9');
END
GO
