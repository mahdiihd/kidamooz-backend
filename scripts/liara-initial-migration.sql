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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [app_users] (
        [Id] nvarchar(64) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_app_users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [audience_segments] (
        [Id] nvarchar(64) NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_audience_segments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [audit_logs] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [Action] nvarchar(50) NOT NULL,
        [EntityType] nvarchar(50) NOT NULL,
        [EntityId] nvarchar(128) NOT NULL,
        [EntityTitle] nvarchar(300) NOT NULL,
        [ActorEmail] nvarchar(256) NOT NULL,
        [Details] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT [PK_audit_logs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [catalog_meta] (
        [Id] int NOT NULL,
        [Version] nvarchar(128) NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_catalog_meta] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_catalog_meta_singleton] CHECK ([Id] = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [categories] (
        [Id] nvarchar(64) NOT NULL,
        [Slug] nvarchar(64) NOT NULL,
        [TitleFa] nvarchar(300) NOT NULL,
        [TitleEn] nvarchar(300) NOT NULL,
        [IconUrl] nvarchar(1000) NOT NULL,
        [Color] nvarchar(16) NOT NULL,
        [SortOrder] int NOT NULL,
        [Published] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT [PK_categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Role] nvarchar(50) NOT NULL DEFAULT N'editor',
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT [PK_users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [stories] (
        [Id] nvarchar(64) NOT NULL,
        [CategoryId] nvarchar(64) NOT NULL,
        [TitleFa] nvarchar(300) NOT NULL,
        [TitleEn] nvarchar(300) NOT NULL,
        [DescriptionFa] nvarchar(2000) NOT NULL,
        [DescriptionEn] nvarchar(2000) NOT NULL,
        [CoverUrl] nvarchar(1000) NOT NULL,
        [AudioUrl] nvarchar(1000) NOT NULL,
        [DurationSeconds] int NOT NULL,
        [AgeMin] int NOT NULL,
        [AgeMax] int NOT NULL,
        [Featured] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Published] bit NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [Visibility] nvarchar(20) NOT NULL DEFAULT N'public',
        [DeletedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT [PK_stories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_stories_visibility] CHECK ([Visibility] IN ('public', 'restricted')),
        CONSTRAINT [FK_stories_categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [refresh_tokens] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(256) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT [PK_refresh_tokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_refresh_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [story_audience_segments] (
        [StoryId] nvarchar(64) NOT NULL,
        [SegmentId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_story_audience_segments] PRIMARY KEY ([StoryId], [SegmentId]),
        CONSTRAINT [FK_story_audience_segments_audience_segments_SegmentId] FOREIGN KEY ([SegmentId]) REFERENCES [audience_segments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_story_audience_segments_stories_StoryId] FOREIGN KEY ([StoryId]) REFERENCES [stories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [story_audience_users] (
        [StoryId] nvarchar(64) NOT NULL,
        [UserId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_story_audience_users] PRIMARY KEY ([StoryId], [UserId]),
        CONSTRAINT [FK_story_audience_users_app_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [app_users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_story_audience_users_stories_StoryId] FOREIGN KEY ([StoryId]) REFERENCES [stories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [story_chapters] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [StoryId] nvarchar(64) NOT NULL,
        [TitleFa] nvarchar(300) NOT NULL,
        [TitleEn] nvarchar(300) NOT NULL,
        [StartSeconds] int NOT NULL,
        [ImageUrl] nvarchar(1000) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_story_chapters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_story_chapters_stories_StoryId] FOREIGN KEY ([StoryId]) REFERENCES [stories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE TABLE [story_views_daily] (
        [ViewDate] date NOT NULL,
        [StoryId] nvarchar(64) NOT NULL,
        [ViewCount] int NOT NULL,
        CONSTRAINT [PK_story_views_daily] PRIMARY KEY ([ViewDate], [StoryId]),
        CONSTRAINT [FK_story_views_daily_stories_StoryId] FOREIGN KEY ([StoryId]) REFERENCES [stories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_CreatedAt] ON [audit_logs] ([CreatedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_categories_Slug] ON [categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_categories_SortOrder] ON [categories] ([SortOrder]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_refresh_tokens_UserId] ON [refresh_tokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_stories_CategoryId] ON [stories] ([CategoryId]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_stories_Published] ON [stories] ([Published]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_stories_SortOrder] ON [stories] ([SortOrder]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_story_audience_segments_SegmentId] ON [story_audience_segments] ([SegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_story_audience_users_UserId] ON [story_audience_users] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_story_chapters_StoryId_SortOrder] ON [story_chapters] ([StoryId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_story_views_daily_StoryId] ON [story_views_daily] ([StoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_Email] ON [users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709161654_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709161654_InitialCreate', N'9.0.9');
END;

COMMIT;
GO

IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Email] = N'admin@kidamooz.com')
BEGIN
    INSERT INTO [users] ([Id], [Email], [PasswordHash], [DisplayName], [Role], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES (
        N'533D9AAB-DA42-49FA-A442-0849ECD5ADE4',
        N'admin@kidamooz.com',
        N'$2a$11$tz9aTI595sbR5SUmF5RdZOI4G7fAYo8X2ArLoqe0TyyrtQFOoQCmm',
        N'مدیر سیستم',
        N'admin',
        1,
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [catalog_meta] WHERE [Id] = 1)
BEGIN
    INSERT INTO [catalog_meta] ([Id], [Version], [UpdatedAt])
    VALUES (1, CONCAT(CONVERT(nvarchar(33), SYSDATETIMEOFFSET(), 127), N'-0-0'), SYSDATETIMEOFFSET());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [audience_segments] WHERE [Id] = N'premium')
BEGIN
    INSERT INTO [audience_segments] ([Id], [Label], [Description], [IsActive]) VALUES
        (N'premium', N'اشتراک ویژه', N'کاربران با اشتراک پریمیوم', 1),
        (N'family', N'پلن خانوادگی', N'خانواده‌های با چند پروفایل کودک', 1),
        (N'beta', N'تسترهای بتا', N'گروه تست داخلی', 1),
        (N'school', N'مدارس همکار', N'مدارس طرف قرارداد', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM [app_users] WHERE [Id] = N'u-1001')
BEGIN
    INSERT INTO [app_users] ([Id], [DisplayName], [Email], [IsActive]) VALUES
        (N'u-1001', N'سارا احمدی', N'sara@example.com', 1),
        (N'u-1002', N'علی رضایی', N'ali@example.com', 1);
END;
GO
