INSERT INTO users (user_id, email, phone_number, password, role)
VALUES 
(NEWID(), 'admin@gmail.com', '0900000001', '123', 'admin'),
(NEWID(), 'seller@gmail.com', '0900000002', '123', 'seller'),
(NEWID(), 'buyer@gmail.com', '0900000003', '123', 'buyer'),
(NEWID(), 'inspector@gmail.com', '0900000004', '123', 'inspector');
-----------------------------------------------------------------------
INSERT INTO brands (brand_name)
VALUES 
('Honda'),
('Yamaha'),
('Giant'),
('Trek');
-----------------------------------------------------------------------
INSERT INTO types (name)
VALUES 
('Mountain Bike'),
('Road Bike'),
('Hybrid'),
('Electric');
-----------------------------------------------------------------------
INSERT INTO inspection_components (component_id, component_name)
VALUES 
(1, 'Frame'),
(2, 'Brakes'),
(3, 'Wheels'),
(4, 'Chain'),
(5, 'Engine');
-----------------------------------------------------------------------
INSERT INTO inspection_locations (
    inspection_location_id, type, contact_name, contact_phone,
    address_line, ward, district, city
)
VALUES 
(NEWID(), 'Center', 'Inspector A', '0909999999',
 '123 Nguyen Trai', 'Ward 1', 'District 1', 'HCM');
-----------------------------------------------------------------------
INSERT INTO kyc_profiles (
    kyc_id, user_id, full_name, id_number, status, verified_at
)
SELECT 
    NEWID(), user_id, 'Seller Name', '123456789', 'verified', GETDATE()
FROM users
WHERE role = 'seller';
-----------------------------------------------------------------------
INSERT INTO listings (
    listing_id, seller_id, brand_id, bike_type_id,
    title, usage_duration, frame_number, description,
    price, status, created_at
)
SELECT TOP 1
    NEWID(),
    u.user_id,
    b.brand_id,
    t.bike_type_id,
    'Used Honda Bike',
    12,
    'FRAME123',
    'Good condition bike',
    1500000,
    'pending',
    GETDATE()
FROM users u, brands b, types t
WHERE u.role = 'seller'
-----------------------------------------------------------------------
INSERT INTO listing_images (listing_id, image_url, image_type)
SELECT TOP 1
    listing_id,
    'https://via.placeholder.com/150',
    'thumbnail'
FROM listings;
-----------------------------------------------------------------------
INSERT INTO inspections (
    inspection_id, listing_id, inspector_id,
    inspection_location_id, status, created_at
)
SELECT TOP 1
    NEWID(),
    l.listing_id,
    u.user_id,
    il.inspection_location_id,
    'pending',
    GETDATE()
FROM listings l
CROSS JOIN users u
CROSS JOIN inspection_locations il
WHERE u.role = 'inspector';