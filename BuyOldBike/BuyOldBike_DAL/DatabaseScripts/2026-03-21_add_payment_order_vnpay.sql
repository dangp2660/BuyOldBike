SET NOCOUNT ON;

IF OBJECT_ID('dbo.orders', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.orders', 'OrderId') IS NOT NULL AND COL_LENGTH('dbo.orders', 'order_id') IS NULL
        EXEC sp_rename 'dbo.orders.OrderId', 'order_id', 'COLUMN';
    IF COL_LENGTH('dbo.orders', 'BuyerId') IS NOT NULL AND COL_LENGTH('dbo.orders', 'buyer_id') IS NULL
        EXEC sp_rename 'dbo.orders.BuyerId', 'buyer_id', 'COLUMN';
    IF COL_LENGTH('dbo.orders', 'ListingId') IS NOT NULL AND COL_LENGTH('dbo.orders', 'listing_id') IS NULL
        EXEC sp_rename 'dbo.orders.ListingId', 'listing_id', 'COLUMN';
    IF COL_LENGTH('dbo.orders', 'Status') IS NOT NULL AND COL_LENGTH('dbo.orders', 'status') IS NULL
        EXEC sp_rename 'dbo.orders.Status', 'status', 'COLUMN';
    IF COL_LENGTH('dbo.orders', 'TotalAmount') IS NOT NULL AND COL_LENGTH('dbo.orders', 'total_amount') IS NULL
        EXEC sp_rename 'dbo.orders.TotalAmount', 'total_amount', 'COLUMN';
    IF COL_LENGTH('dbo.orders', 'CreatedAt') IS NOT NULL AND COL_LENGTH('dbo.orders', 'created_at') IS NULL
        EXEC sp_rename 'dbo.orders.CreatedAt', 'created_at', 'COLUMN';
END;

IF OBJECT_ID('dbo.orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.orders
    (
        order_id uniqueidentifier NOT NULL CONSTRAINT DF_orders_order_id DEFAULT (newid()),
        buyer_id uniqueidentifier NULL,
        listing_id uniqueidentifier NULL,
        status varchar(50) NULL,
        total_amount decimal(18, 2) NULL,
        created_at datetime NULL,
        CONSTRAINT PK_orders PRIMARY KEY (order_id)
    );
END;

IF COL_LENGTH('dbo.orders', 'buyer_id') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD buyer_id uniqueidentifier NULL;
END;

IF COL_LENGTH('dbo.orders', 'listing_id') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD listing_id uniqueidentifier NULL;
END;

IF COL_LENGTH('dbo.orders', 'status') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD status varchar(50) NULL;
END;

IF COL_LENGTH('dbo.orders', 'total_amount') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD total_amount decimal(18, 2) NULL;
END;

IF COL_LENGTH('dbo.orders', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD created_at datetime NULL;
END;

IF OBJECT_ID('dbo.orders', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.default_constraints dc
       INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
       WHERE dc.parent_object_id = OBJECT_ID('dbo.orders')
         AND c.name = 'order_id'
   )
BEGIN
    ALTER TABLE dbo.orders ADD CONSTRAINT DF_orders_order_id DEFAULT (newid()) FOR order_id;
END;

IF OBJECT_ID('dbo.payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.payments
    (
        payment_id uniqueidentifier NOT NULL CONSTRAINT DF_payments_payment_id DEFAULT (newid()),
        user_id uniqueidentifier NULL,
        amount decimal(18, 2) NULL,
        payment_type varchar(50) NULL,
        status varchar(50) NULL,
        created_at datetime NULL,
        order_id uniqueidentifier NULL,
        txn_ref varchar(100) NULL,
        provider_txn_no varchar(50) NULL,
        CONSTRAINT PK_payments PRIMARY KEY (payment_id)
    );
END;

IF OBJECT_ID('dbo.payments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.payments', 'PaymentId') IS NOT NULL AND COL_LENGTH('dbo.payments', 'payment_id') IS NULL
        EXEC sp_rename 'dbo.payments.PaymentId', 'payment_id', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'UserId') IS NOT NULL AND COL_LENGTH('dbo.payments', 'user_id') IS NULL
        EXEC sp_rename 'dbo.payments.UserId', 'user_id', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'OrderId') IS NOT NULL AND COL_LENGTH('dbo.payments', 'order_id') IS NULL
        EXEC sp_rename 'dbo.payments.OrderId', 'order_id', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'Amount') IS NOT NULL AND COL_LENGTH('dbo.payments', 'amount') IS NULL
        EXEC sp_rename 'dbo.payments.Amount', 'amount', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'PaymentType') IS NOT NULL AND COL_LENGTH('dbo.payments', 'payment_type') IS NULL
        EXEC sp_rename 'dbo.payments.PaymentType', 'payment_type', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'Status') IS NOT NULL AND COL_LENGTH('dbo.payments', 'status') IS NULL
        EXEC sp_rename 'dbo.payments.Status', 'status', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'CreatedAt') IS NOT NULL AND COL_LENGTH('dbo.payments', 'created_at') IS NULL
        EXEC sp_rename 'dbo.payments.CreatedAt', 'created_at', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'TxnRef') IS NOT NULL AND COL_LENGTH('dbo.payments', 'txn_ref') IS NULL
        EXEC sp_rename 'dbo.payments.TxnRef', 'txn_ref', 'COLUMN';
    IF COL_LENGTH('dbo.payments', 'ProviderTxnNo') IS NOT NULL AND COL_LENGTH('dbo.payments', 'provider_txn_no') IS NULL
        EXEC sp_rename 'dbo.payments.ProviderTxnNo', 'provider_txn_no', 'COLUMN';
END;

IF COL_LENGTH('dbo.payments', 'user_id') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD user_id uniqueidentifier NULL;
END;

IF COL_LENGTH('dbo.payments', 'amount') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD amount decimal(18, 2) NULL;
END;

IF COL_LENGTH('dbo.payments', 'payment_type') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD payment_type varchar(50) NULL;
END;

IF COL_LENGTH('dbo.payments', 'status') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD status varchar(50) NULL;
END;

IF COL_LENGTH('dbo.payments', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD created_at datetime NULL;
END;

IF COL_LENGTH('dbo.payments', 'order_id') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD order_id uniqueidentifier NULL;
END;

IF COL_LENGTH('dbo.payments', 'txn_ref') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD txn_ref varchar(100) NULL;
END;

IF COL_LENGTH('dbo.payments', 'provider_txn_no') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD provider_txn_no varchar(50) NULL;
END;

IF OBJECT_ID('dbo.payments', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.default_constraints dc
       INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
       WHERE dc.parent_object_id = OBJECT_ID('dbo.payments')
         AND c.name = 'payment_id'
   )
BEGIN
    ALTER TABLE dbo.payments ADD CONSTRAINT DF_payments_payment_id DEFAULT (newid()) FOR payment_id;
END;

IF OBJECT_ID('dbo.payments', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.orders', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_key_columns fkc
       INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
       WHERE fk.parent_object_id = OBJECT_ID('dbo.payments')
         AND fk.referenced_object_id = OBJECT_ID('dbo.orders')
         AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'order_id'
         AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'order_id'
   )
BEGIN
    ALTER TABLE dbo.payments
    ADD CONSTRAINT FK_payments_orders_order_id
    FOREIGN KEY (order_id) REFERENCES dbo.orders(order_id);
END;

IF OBJECT_ID('dbo.orders', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.users', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_key_columns fkc
       INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
       WHERE fk.parent_object_id = OBJECT_ID('dbo.orders')
         AND fk.referenced_object_id = OBJECT_ID('dbo.users')
         AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'buyer_id'
         AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'user_id'
   )
BEGIN
    ALTER TABLE dbo.orders
    ADD CONSTRAINT FK_orders_users_buyer_id
    FOREIGN KEY (buyer_id) REFERENCES dbo.users(user_id);
END;

IF OBJECT_ID('dbo.orders', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.listings', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_key_columns fkc
       INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
       WHERE fk.parent_object_id = OBJECT_ID('dbo.orders')
         AND fk.referenced_object_id = OBJECT_ID('dbo.listings')
         AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'listing_id'
         AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'listing_id'
   )
BEGIN
    ALTER TABLE dbo.orders
    ADD CONSTRAINT FK_orders_listings_listing_id
    FOREIGN KEY (listing_id) REFERENCES dbo.listings(listing_id);
END;

IF OBJECT_ID('dbo.payments', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.users', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_key_columns fkc
       INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
       WHERE fk.parent_object_id = OBJECT_ID('dbo.payments')
         AND fk.referenced_object_id = OBJECT_ID('dbo.users')
         AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'user_id'
         AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'user_id'
   )
BEGIN
    ALTER TABLE dbo.payments
    ADD CONSTRAINT FK_payments_users_user_id
    FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_payments_order_id' AND object_id = OBJECT_ID('dbo.payments'))
BEGIN
    CREATE INDEX IX_payments_order_id ON dbo.payments(order_id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_payments_txn_ref' AND object_id = OBJECT_ID('dbo.payments'))
BEGIN
    CREATE INDEX IX_payments_txn_ref ON dbo.payments(txn_ref);
END;
