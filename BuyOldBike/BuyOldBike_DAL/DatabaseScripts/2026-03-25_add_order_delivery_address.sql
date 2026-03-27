ALTER TABLE dbo.orders
ADD
    delivery_full_name VARCHAR(255) NULL,
    delivery_phone_number VARCHAR(20) NULL,
    delivery_province VARCHAR(100) NULL,
    delivery_district VARCHAR(100) NULL,
    delivery_ward VARCHAR(100) NULL,
    delivery_detail TEXT NULL;

