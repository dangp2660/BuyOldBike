CREATE TABLE [dbo].[user_wallets] (
    [wallet_id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_user_wallets_wallet_id] DEFAULT (newid()),
    [user_id] UNIQUEIDENTIFIER NOT NULL,
    [balance] DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_user_wallets_balance] DEFAULT (0),
    [updated_at] DATETIME NOT NULL CONSTRAINT [DF_user_wallets_updated_at] DEFAULT (getdate()),
    CONSTRAINT [PK_user_wallets] PRIMARY KEY CLUSTERED ([wallet_id] ASC),
    CONSTRAINT [UQ_user_wallets_user_id] UNIQUE ([user_id]),
    CONSTRAINT [FK_user_wallets_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);

CREATE TABLE [dbo].[wallet_transactions] (
    [wallet_transaction_id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_wallet_transactions_id] DEFAULT (newid()),
    [wallet_id] UNIQUEIDENTIFIER NOT NULL,
    [amount] DECIMAL(18, 2) NOT NULL,
    [direction] VARCHAR(20) NOT NULL,
    [type] VARCHAR(50) NOT NULL,
    [order_id] UNIQUEIDENTIFIER NULL,
    [note] TEXT NULL,
    [created_at] DATETIME NOT NULL CONSTRAINT [DF_wallet_transactions_created_at] DEFAULT (getdate()),
    CONSTRAINT [PK_wallet_transactions] PRIMARY KEY CLUSTERED ([wallet_transaction_id] ASC),
    CONSTRAINT [FK_wallet_transactions_user_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [dbo].[user_wallets] ([wallet_id])
);

CREATE INDEX [IX_wallet_transactions_wallet_id_created_at] ON [dbo].[wallet_transactions] ([wallet_id], [created_at] DESC);

