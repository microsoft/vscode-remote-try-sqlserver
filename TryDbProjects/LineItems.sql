CREATE TABLE [dbo].[LineItems]
(
    [Id] INT IDENTITY(1, 1) NOT NULL,
    [BEOid] INT NOT NULL,
    [ItemText] NVARCHAR(MAX) NULL,
    [Quantity] INT NULL,
    [Notes] NVARCHAR(MAX) NULL,
    [CompletedTime] TIME(0) NULL,
    [CompletedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_LineItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_LineItems_BEOs] FOREIGN KEY ([BEOid]) REFERENCES [dbo].[BEOs] ([Id])
);

CREATE INDEX [IX_LineItems_BEOid] ON [dbo].[LineItems] ([BEOid]);
