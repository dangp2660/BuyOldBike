CREATE DATABASE BuyOldBike;
GO

USE BuyOldBike;
GO

CREATE TABLE users (
    user_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    email VARCHAR(255) NOT NULL UNIQUE,
    phone_number VARCHAR(20) NOT NULL,
    password VARCHAR(255),
    role VARCHAR(50) NOT NULL
);

CREATE TABLE brands (
    brand_id INT IDENTITY(1,1) PRIMARY KEY,
    brand_name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE types (
    bike_type_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE kyc_profiles (
    kyc_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id UNIQUEIDENTIFIER,
    id_number VARCHAR(50),
    full_name NVARCHAR(255),
    dateOfBirth NVARCHAR(50),
    gender NVARCHAR(50),
    nationality NVARCHAR(100),
    placeOfOrigin NVARCHAR(255),
    placeOfResidence VARCHAR(255),
    expiryDate VARCHAR(50),
    verified_at DATETIME,
    status VARCHAR(50),

    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE TABLE listings (
    listing_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    seller_id UNIQUEIDENTIFIER,
    brand_id INT,
    title VARCHAR(255),
    bike_type_id INT,
    usage_duration INT,
    frame_number VARCHAR(100),
    description TEXT,
    price DECIMAL(18,2),
    status VARCHAR(50),
    created_at DATETIME,
    listing_url NVARCHAR(500),

    FOREIGN KEY (seller_id) REFERENCES users(user_id),
    FOREIGN KEY (brand_id) REFERENCES brands(brand_id),
    FOREIGN KEY (bike_type_id) REFERENCES types(bike_type_id)
);

CREATE TABLE listing_images (
    image_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    listing_id UNIQUEIDENTIFIER,
    image_url VARCHAR(500),
    image_type VARCHAR(50),

    FOREIGN KEY (listing_id) REFERENCES listings(listing_id)
);

CREATE TABLE kyc_images (
    image_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    kyc_id UNIQUEIDENTIFIER NOT NULL,
    image_type VARCHAR(50),
    image_url VARCHAR(500) NOT NULL,
    created_at DATETIME,

    FOREIGN KEY (kyc_id) REFERENCES kyc_profiles(kyc_id)
);

CREATE TABLE inspection_locations (
    inspection_location_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    type VARCHAR(50) NOT NULL,
    contact_name VARCHAR(255),
    contact_phone VARCHAR(20),
    address_line VARCHAR(255) NOT NULL,
    ward VARCHAR(100),
    district VARCHAR(100),
    city VARCHAR(100),
    latitude DECIMAL(9,6),
    longitude DECIMAL(9,6),
    note TEXT
);

CREATE TABLE inspections (
    inspection_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    listing_id UNIQUEIDENTIFIER NOT NULL,
    inspector_id UNIQUEIDENTIFIER,
    inspection_type_id VARCHAR(50),
    inspection_location_id UNIQUEIDENTIFIER NOT NULL,
    scheduled_at DATETIME,
    status VARCHAR(50) NOT NULL,
    result VARCHAR(50),
    overall_score INT,
    reject_reason TEXT,
    created_at DATETIME NOT NULL,

    FOREIGN KEY (listing_id) REFERENCES listings(listing_id),
    FOREIGN KEY (inspector_id) REFERENCES users(user_id),
    FOREIGN KEY (inspection_location_id) REFERENCES inspection_locations(inspection_location_id)
);

CREATE TABLE inspection_components (
    component_id INT PRIMARY KEY,
    component_name VARCHAR(255)
);

CREATE TABLE inspection_scores (
    inspection_id UNIQUEIDENTIFIER,
    component_id INT,
    score INT,
    note TEXT,

    PRIMARY KEY (inspection_id, component_id),

    FOREIGN KEY (inspection_id) REFERENCES inspections(inspection_id),
    FOREIGN KEY (component_id) REFERENCES inspection_components(component_id)
);

CREATE TABLE payments (
    payment_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id UNIQUEIDENTIFIER,
    amount DECIMAL(18,2),
    payment_type VARCHAR(50),
    status VARCHAR(50),
    created_at DATETIME,

    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE TABLE orders (
    order_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    buyer_id UNIQUEIDENTIFIER,
    listing_id UNIQUEIDENTIFIER,
    status VARCHAR(50),
    total_amount DECIMAL(18,2),
    created_at DATETIME,

    FOREIGN KEY (buyer_id) REFERENCES users(user_id),
    FOREIGN KEY (listing_id) REFERENCES listings(listing_id)
);

CREATE TABLE addresses (
    address_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id UNIQUEIDENTIFIER UNIQUE,
    full_name VARCHAR(255),
    phone_number VARCHAR(20),
    province VARCHAR(100),
    city VARCHAR(100),
    district VARCHAR(100),
    ward VARCHAR(100),
    detail TEXT,

    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE TABLE return_requests (
    return_request_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_id UNIQUEIDENTIFIER,
    reason TEXT,
    detail TEXT,
    status VARCHAR(50),
    created_at DATETIME,

    FOREIGN KEY (order_id) REFERENCES orders(order_id)
);

CREATE TABLE return_request_images (
    image_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    return_request_id UNIQUEIDENTIFIER NOT NULL,
    image_url VARCHAR(500) NOT NULL,
    created_at DATETIME,

    FOREIGN KEY (return_request_id) REFERENCES return_requests(return_request_id)
);

CREATE TABLE reviews (
    review_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_id UNIQUEIDENTIFIER,
    buyer_id UNIQUEIDENTIFIER,
    seller_id UNIQUEIDENTIFIER,
    rating INT,
    description TEXT,
    created_at DATETIME,

    FOREIGN KEY (order_id) REFERENCES orders(order_id),
    FOREIGN KEY (buyer_id) REFERENCES users(user_id),
    FOREIGN KEY (seller_id) REFERENCES users(user_id)
);