SET NOCOUNT ON;

DECLARE @buyer_user_id UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @seller_user_id UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @inspector_user_id UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @admin_user_id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';

DROP TABLE IF EXISTS dbo.messages;
DROP TABLE IF EXISTS dbo.seller_profiles;
DROP TABLE IF EXISTS dbo.inspection_scores;
DROP TABLE IF EXISTS dbo.inspections;
DROP TABLE IF EXISTS dbo.kyc_images;
DROP TABLE IF EXISTS dbo.kyc_profiles;
DROP TABLE IF EXISTS dbo.wallet_transactions;
DROP TABLE IF EXISTS dbo.user_wallets;
DROP TABLE IF EXISTS dbo.reviews;
DROP TABLE IF EXISTS dbo.return_request_images;
DROP TABLE IF EXISTS dbo.return_requests;
DROP TABLE IF EXISTS dbo.payments;
DROP TABLE IF EXISTS dbo.orders;
DROP TABLE IF EXISTS dbo.listing_images;
DROP TABLE IF EXISTS dbo.addresses;
DROP TABLE IF EXISTS dbo.listings;
DROP TABLE IF EXISTS dbo.frame_sizes;
DROP TABLE IF EXISTS dbo.inspection_locations;
DROP TABLE IF EXISTS dbo.inspection_components;
DROP TABLE IF EXISTS dbo.types;
DROP TABLE IF EXISTS dbo.brands;
DROP TABLE IF EXISTS dbo.users;

CREATE TABLE dbo.users (
    user_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_users_user_id DEFAULT (NEWID()),
    email VARCHAR(255) NOT NULL,
    password VARCHAR(255) NULL,
    phone_number VARCHAR(20) NOT NULL,
    role VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL CONSTRAINT DF_users_status DEFAULT ('Active'),
    CONSTRAINT PK_users PRIMARY KEY (user_id)
);

CREATE UNIQUE INDEX UX_users_email ON dbo.users(email);

INSERT INTO dbo.users (user_id, email, password, phone_number, role, status)
VALUES
    (@buyer_user_id, 'buyer@gmail.com', '123456', '0900000001', 'Buyer', 'Active'),
    (@seller_user_id, 'seller@gmail.com', '123456', '0900000002', 'Seller', 'Active'),
    (@inspector_user_id, 'inspector@gmail.com', '123456', '0900000003', 'Inspector', 'Active'),
    (@admin_user_id, 'admin@gmail.com', '123456', '0900000004', 'Admin', 'Active');

CREATE TABLE dbo.seller_profiles (
    seller_id UNIQUEIDENTIFIER NOT NULL,
    seller_rating FLOAT NOT NULL CONSTRAINT DF_seller_profiles_seller_rating DEFAULT (0),
    total_reviews INT NOT NULL CONSTRAINT DF_seller_profiles_total_reviews DEFAULT (0),
    last_review_date DATETIME NULL,
    CONSTRAINT PK_seller_profiles PRIMARY KEY (seller_id),
    CONSTRAINT FK_seller_profiles_users_seller_id FOREIGN KEY (seller_id) REFERENCES dbo.users(user_id)
);

INSERT INTO dbo.seller_profiles (seller_id, seller_rating, total_reviews, last_review_date)
VALUES (@seller_user_id, 0, 0, NULL);

CREATE TABLE dbo.brands (
    brand_id INT IDENTITY(1, 1) NOT NULL,
    brand_name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_brands PRIMARY KEY (brand_id)
);

CREATE UNIQUE INDEX UX_brands_brand_name ON dbo.brands(brand_name);

CREATE TABLE dbo.types (
    bike_type_id INT IDENTITY(1, 1) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_types PRIMARY KEY (bike_type_id)
);

CREATE UNIQUE INDEX UX_types_name ON dbo.types(name);

CREATE TABLE dbo.inspection_components (
    component_id INT NOT NULL,
    component_name NVARCHAR(255) NULL,
    CONSTRAINT PK_inspection_components PRIMARY KEY (component_id)
);

CREATE TABLE dbo.inspection_locations (
    inspection_location_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inspection_locations_inspection_location_id DEFAULT (NEWID()),
    address_line NVARCHAR(255) NOT NULL,
    city NVARCHAR(100) NULL,
    contact_name NVARCHAR(255) NULL,
    contact_phone VARCHAR(20) NULL,
    district NVARCHAR(100) NULL,
    latitude DECIMAL(9, 6) NULL,
    longitude DECIMAL(9, 6) NULL,
    note NVARCHAR(MAX) NULL,
    type VARCHAR(50) NOT NULL,
    ward NVARCHAR(100) NULL,
    CONSTRAINT PK_inspection_locations PRIMARY KEY (inspection_location_id)
);

CREATE TABLE dbo.frame_sizes (
    frame_size_id INT IDENTITY(1, 1) NOT NULL,
    size_value NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_frame_sizes PRIMARY KEY (frame_size_id)
);

CREATE TABLE dbo.listings (
    listing_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_listings_listing_id DEFAULT (NEWID()),
    seller_id UNIQUEIDENTIFIER NULL,
    brand_id INT NULL,
    title NVARCHAR(255) NULL,
    bike_type_id INT NULL,
    usage_duration INT NULL,
    frame_number VARCHAR(100) NULL,
    description NVARCHAR(MAX) NULL,
    price DECIMAL(18, 2) NULL,
    status VARCHAR(50) NULL,
    created_at DATETIME NULL,
    listing_url NVARCHAR(500) NULL,
    views INT NOT NULL CONSTRAINT DF_listings_views DEFAULT (0),
    CONSTRAINT PK_listings PRIMARY KEY (listing_id),
    CONSTRAINT FK_listings_users_seller_id FOREIGN KEY (seller_id) REFERENCES dbo.users(user_id),
    CONSTRAINT FK_listings_brands_brand_id FOREIGN KEY (brand_id) REFERENCES dbo.brands(brand_id),
    CONSTRAINT FK_listings_types_bike_type_id FOREIGN KEY (bike_type_id) REFERENCES dbo.types(bike_type_id)
);

CREATE TABLE dbo.addresses (
    address_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_addresses_address_id DEFAULT (NEWID()),
    city NVARCHAR(100) NULL,
    detail NVARCHAR(MAX) NULL,
    district NVARCHAR(100) NULL,
    full_name NVARCHAR(255) NULL,
    phone_number VARCHAR(20) NULL,
    province NVARCHAR(100) NULL,
    user_id UNIQUEIDENTIFIER NULL,
    ward NVARCHAR(100) NULL,
    CONSTRAINT PK_addresses PRIMARY KEY (address_id),
    CONSTRAINT FK_addresses_users_user_id FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);

CREATE UNIQUE INDEX UX_addresses_user_id ON dbo.addresses(user_id);

CREATE TABLE dbo.listing_images (
    image_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_listing_images_image_id DEFAULT (NEWID()),
    image_type VARCHAR(50) NULL,
    image_url VARCHAR(500) NULL,
    listing_id UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_listing_images PRIMARY KEY (image_id),
    CONSTRAINT FK_listing_images_listings_listing_id FOREIGN KEY (listing_id) REFERENCES dbo.listings(listing_id)
);

CREATE TABLE dbo.orders (
    order_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_orders_order_id DEFAULT (NEWID()),
    buyer_id UNIQUEIDENTIFIER NULL,
    created_at DATETIME NULL,
    delivery_detail NVARCHAR(MAX) NULL,
    delivery_district NVARCHAR(100) NULL,
    delivery_full_name NVARCHAR(255) NULL,
    delivery_phone_number VARCHAR(20) NULL,
    delivery_province NVARCHAR(100) NULL,
    delivery_ward NVARCHAR(100) NULL,
    listing_id UNIQUEIDENTIFIER NULL,
    status VARCHAR(50) NULL,
    total_amount DECIMAL(18, 2) NULL,
    CONSTRAINT PK_orders PRIMARY KEY (order_id),
    CONSTRAINT FK_orders_users_buyer_id FOREIGN KEY (buyer_id) REFERENCES dbo.users(user_id),
    CONSTRAINT FK_orders_listings_listing_id FOREIGN KEY (listing_id) REFERENCES dbo.listings(listing_id)
);

CREATE TABLE dbo.payments (
    payment_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_payments_payment_id DEFAULT (NEWID()),
    amount DECIMAL(18, 2) NULL,
    created_at DATETIME NULL,
    order_id UNIQUEIDENTIFIER NULL,
    payment_type VARCHAR(50) NULL,
    provider_txn_no VARCHAR(50) NULL,
    status VARCHAR(50) NULL,
    txn_ref VARCHAR(100) NULL,
    user_id UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_payments PRIMARY KEY (payment_id),
    CONSTRAINT FK_payments_orders_order_id FOREIGN KEY (order_id) REFERENCES dbo.orders(order_id),
    CONSTRAINT FK_payments_users_user_id FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);

CREATE TABLE dbo.return_requests (
    return_request_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_return_requests_return_request_id DEFAULT (NEWID()),
    created_at DATETIME NULL,
    detail NVARCHAR(MAX) NULL,
    order_id UNIQUEIDENTIFIER NULL,
    reason NVARCHAR(MAX) NULL,
    status VARCHAR(50) NULL,
    CONSTRAINT PK_return_requests PRIMARY KEY (return_request_id),
    CONSTRAINT FK_return_requests_orders_order_id FOREIGN KEY (order_id) REFERENCES dbo.orders(order_id)
);

CREATE TABLE dbo.return_request_images (
    image_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_return_request_images_image_id DEFAULT (NEWID()),
    created_at DATETIME NULL,
    image_url VARCHAR(500) NOT NULL,
    return_request_id UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_return_request_images PRIMARY KEY (image_id),
    CONSTRAINT FK_return_request_images_return_requests_return_request_id FOREIGN KEY (return_request_id) REFERENCES dbo.return_requests(return_request_id)
);

CREATE TABLE dbo.reviews (
    review_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_reviews_review_id DEFAULT (NEWID()),
    buyer_id UNIQUEIDENTIFIER NULL,
    created_at DATETIME NULL,
    description NVARCHAR(MAX) NULL,
    order_id UNIQUEIDENTIFIER NULL,
    rating INT NULL,
    seller_id UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_reviews PRIMARY KEY (review_id),
    CONSTRAINT FK_reviews_users_buyer_id FOREIGN KEY (buyer_id) REFERENCES dbo.users(user_id),
    CONSTRAINT FK_reviews_orders_order_id FOREIGN KEY (order_id) REFERENCES dbo.orders(order_id),
    CONSTRAINT FK_reviews_users_seller_id FOREIGN KEY (seller_id) REFERENCES dbo.users(user_id)
);

CREATE TABLE dbo.user_wallets (
    wallet_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_wallets_wallet_id DEFAULT (NEWID()),
    user_id UNIQUEIDENTIFIER NOT NULL,
    balance DECIMAL(18, 2) NOT NULL,
    updated_at DATETIME NOT NULL,
    CONSTRAINT PK_user_wallets PRIMARY KEY (wallet_id),
    CONSTRAINT FK_user_wallets_users_user_id FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);

CREATE UNIQUE INDEX UX_user_wallets_user_id ON dbo.user_wallets(user_id);

CREATE TABLE dbo.wallet_transactions (
    wallet_transaction_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_wallet_transactions_wallet_transaction_id DEFAULT (NEWID()),
    wallet_id UNIQUEIDENTIFIER NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    direction VARCHAR(20) NOT NULL,
    type VARCHAR(50) NOT NULL,
    order_id UNIQUEIDENTIFIER NULL,
    note NVARCHAR(MAX) NULL,
    created_at DATETIME NOT NULL,
    CONSTRAINT PK_wallet_transactions PRIMARY KEY (wallet_transaction_id),
    CONSTRAINT FK_wallet_transactions_user_wallets_wallet_id FOREIGN KEY (wallet_id) REFERENCES dbo.user_wallets(wallet_id)
);

CREATE INDEX IX_wallet_transactions_wallet_id ON dbo.wallet_transactions(wallet_id);

CREATE TABLE dbo.kyc_profiles (
    kyc_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_kyc_profiles_kyc_id DEFAULT (NEWID()),
    dateOfBirth NVARCHAR(50) NULL,
    expiryDate VARCHAR(50) NULL,
    full_name NVARCHAR(255) NULL,
    gender NVARCHAR(50) NULL,
    id_number VARCHAR(50) NULL,
    nationality NVARCHAR(100) NULL,
    placeOfOrigin NVARCHAR(255) NULL,
    placeOfResidence NVARCHAR(255) NULL,
    user_id UNIQUEIDENTIFIER NULL,
    verified_at DATETIME NULL,
    CONSTRAINT PK_kyc_profiles PRIMARY KEY (kyc_id),
    CONSTRAINT FK_kyc_profiles_users_user_id FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);

CREATE TABLE dbo.kyc_images (
    image_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_kyc_images_image_id DEFAULT (NEWID()),
    created_at DATETIME NULL,
    image_type VARCHAR(50) NULL,
    image_url VARCHAR(500) NULL,
    image_data VARBINARY(MAX) NULL,
    content_type VARCHAR(100) NULL,
    file_name NVARCHAR(255) NULL,
    kyc_id UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_kyc_images PRIMARY KEY (image_id),
    CONSTRAINT FK_kyc_images_kyc_profiles_kyc_id FOREIGN KEY (kyc_id) REFERENCES dbo.kyc_profiles(kyc_id)
);

CREATE TABLE dbo.inspections (
    inspection_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inspections_inspection_id DEFAULT (NEWID()),
    created_at DATETIME NOT NULL,
    inspection_location_id UNIQUEIDENTIFIER NOT NULL,
    inspection_type_id VARCHAR(50) NULL,
    inspector_id UNIQUEIDENTIFIER NULL,
    listing_id UNIQUEIDENTIFIER NOT NULL,
    overall_score INT NULL,
    reject_reason NVARCHAR(MAX) NULL,
    result VARCHAR(50) NULL,
    scheduled_at DATETIME NULL,
    status VARCHAR(50) NOT NULL,
    CONSTRAINT PK_inspections PRIMARY KEY (inspection_id),
    CONSTRAINT FK_inspections_inspection_locations_inspection_location_id FOREIGN KEY (inspection_location_id) REFERENCES dbo.inspection_locations(inspection_location_id),
    CONSTRAINT FK_inspections_users_inspector_id FOREIGN KEY (inspector_id) REFERENCES dbo.users(user_id),
    CONSTRAINT FK_inspections_listings_listing_id FOREIGN KEY (listing_id) REFERENCES dbo.listings(listing_id)
);

CREATE TABLE dbo.inspection_scores (
    inspection_id UNIQUEIDENTIFIER NOT NULL,
    component_id INT NOT NULL,
    note NVARCHAR(MAX) NULL,
    score INT NULL,
    CONSTRAINT PK_inspection_scores PRIMARY KEY (inspection_id, component_id),
    CONSTRAINT FK_inspection_scores_inspections_inspection_id FOREIGN KEY (inspection_id) REFERENCES dbo.inspections(inspection_id),
    CONSTRAINT FK_inspection_scores_inspection_components_component_id FOREIGN KEY (component_id) REFERENCES dbo.inspection_components(component_id)
);

CREATE TABLE dbo.messages (
    message_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_messages_message_id DEFAULT (NEWID()),
    listing_id UNIQUEIDENTIFIER NOT NULL,
    sender_id UNIQUEIDENTIFIER NOT NULL,
    receiver_id UNIQUEIDENTIFIER NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    sent_at DATETIME NOT NULL,
    is_read BIT NOT NULL,
    CONSTRAINT PK_messages PRIMARY KEY (message_id),
    CONSTRAINT FK_messages_listings_listing_id FOREIGN KEY (listing_id) REFERENCES dbo.listings(listing_id),
    CONSTRAINT FK_messages_users_sender_id FOREIGN KEY (sender_id) REFERENCES dbo.users(user_id),
    CONSTRAINT FK_messages_users_receiver_id FOREIGN KEY (receiver_id) REFERENCES dbo.users(user_id)
);

CREATE INDEX IX_messages_listing_id ON dbo.messages(listing_id);
CREATE INDEX IX_messages_sender_id ON dbo.messages(sender_id);
CREATE INDEX IX_messages_receiver_id ON dbo.messages(receiver_id);
