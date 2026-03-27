ALTER TABLE dbo.addresses ALTER COLUMN city NVARCHAR(100) NULL;
ALTER TABLE dbo.addresses ALTER COLUMN district NVARCHAR(100) NULL;
ALTER TABLE dbo.addresses ALTER COLUMN full_name NVARCHAR(255) NULL;
ALTER TABLE dbo.addresses ALTER COLUMN province NVARCHAR(100) NULL;
ALTER TABLE dbo.addresses ALTER COLUMN ward NVARCHAR(100) NULL;
ALTER TABLE dbo.addresses ALTER COLUMN detail NVARCHAR(MAX) NULL;

DECLARE @brands_unique_name sysname;
DECLARE @brands_unique_is_constraint bit;

SELECT TOP (1)
    @brands_unique_name = i.name,
    @brands_unique_is_constraint = CASE WHEN kc.name IS NULL THEN 0 ELSE 1 END
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.key_constraints kc
    ON kc.parent_object_id = i.object_id
    AND kc.unique_index_id = i.index_id
    AND kc.[type] = 'UQ'
WHERE i.object_id = OBJECT_ID(N'dbo.brands')
  AND i.is_unique = 1
  AND c.name = N'brand_name';

IF @brands_unique_name IS NOT NULL
BEGIN
    IF @brands_unique_is_constraint = 1
        EXEC(N'ALTER TABLE dbo.brands DROP CONSTRAINT [' + @brands_unique_name + N']');
    ELSE
        EXEC(N'DROP INDEX [' + @brands_unique_name + N'] ON dbo.brands');
END

ALTER TABLE dbo.brands ALTER COLUMN brand_name NVARCHAR(100) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.brands')
      AND i.is_unique = 1
      AND c.name = N'brand_name'
)
BEGIN
    ALTER TABLE dbo.brands ADD CONSTRAINT UQ_brands_brand_name UNIQUE (brand_name);
END

ALTER TABLE dbo.inspections ALTER COLUMN reject_reason NVARCHAR(MAX) NULL;

ALTER TABLE dbo.inspection_components ALTER COLUMN component_name NVARCHAR(255) NULL;

ALTER TABLE dbo.inspection_locations ALTER COLUMN address_line NVARCHAR(255) NOT NULL;
ALTER TABLE dbo.inspection_locations ALTER COLUMN city NVARCHAR(100) NULL;
ALTER TABLE dbo.inspection_locations ALTER COLUMN contact_name NVARCHAR(255) NULL;
ALTER TABLE dbo.inspection_locations ALTER COLUMN district NVARCHAR(100) NULL;
ALTER TABLE dbo.inspection_locations ALTER COLUMN ward NVARCHAR(100) NULL;
ALTER TABLE dbo.inspection_locations ALTER COLUMN note NVARCHAR(MAX) NULL;

ALTER TABLE dbo.inspection_scores ALTER COLUMN note NVARCHAR(MAX) NULL;

ALTER TABLE dbo.kyc_profiles ALTER COLUMN placeOfResidence NVARCHAR(255) NULL;

ALTER TABLE dbo.listings ALTER COLUMN title NVARCHAR(255) NULL;
ALTER TABLE dbo.listings ALTER COLUMN description NVARCHAR(MAX) NULL;

ALTER TABLE dbo.orders ALTER COLUMN delivery_detail NVARCHAR(MAX) NULL;
ALTER TABLE dbo.orders ALTER COLUMN delivery_district NVARCHAR(100) NULL;
ALTER TABLE dbo.orders ALTER COLUMN delivery_full_name NVARCHAR(255) NULL;
ALTER TABLE dbo.orders ALTER COLUMN delivery_province NVARCHAR(100) NULL;
ALTER TABLE dbo.orders ALTER COLUMN delivery_ward NVARCHAR(100) NULL;

ALTER TABLE dbo.return_requests ALTER COLUMN detail NVARCHAR(MAX) NULL;
ALTER TABLE dbo.return_requests ALTER COLUMN reason NVARCHAR(MAX) NULL;

ALTER TABLE dbo.reviews ALTER COLUMN description NVARCHAR(MAX) NULL;

DECLARE @types_unique_name sysname;
DECLARE @types_unique_is_constraint bit;

SELECT TOP (1)
    @types_unique_name = i.name,
    @types_unique_is_constraint = CASE WHEN kc.name IS NULL THEN 0 ELSE 1 END
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.key_constraints kc
    ON kc.parent_object_id = i.object_id
    AND kc.unique_index_id = i.index_id
    AND kc.[type] = 'UQ'
WHERE i.object_id = OBJECT_ID(N'dbo.types')
  AND i.is_unique = 1
  AND c.name = N'name';

IF @types_unique_name IS NOT NULL
BEGIN
    IF @types_unique_is_constraint = 1
        EXEC(N'ALTER TABLE dbo.types DROP CONSTRAINT [' + @types_unique_name + N']');
    ELSE
        EXEC(N'DROP INDEX [' + @types_unique_name + N'] ON dbo.types');
END

ALTER TABLE dbo.types ALTER COLUMN name NVARCHAR(100) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.types')
      AND i.is_unique = 1
      AND c.name = N'name'
)
BEGIN
    ALTER TABLE dbo.types ADD CONSTRAINT UQ_types_name UNIQUE (name);
END

ALTER TABLE dbo.wallet_transactions ALTER COLUMN note NVARCHAR(MAX) NULL;
