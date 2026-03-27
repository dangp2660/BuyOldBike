SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.addresses', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.addresses', 'city') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN city NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.addresses', 'district') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN district NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.addresses', 'full_name') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN full_name NVARCHAR(255) NULL;
        IF COL_LENGTH('dbo.addresses', 'province') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN province NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.addresses', 'ward') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN ward NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.addresses', 'detail') IS NOT NULL
            ALTER TABLE dbo.addresses ALTER COLUMN detail NVARCHAR(MAX) NULL;
    END

    IF OBJECT_ID(N'dbo.brands', N'U') IS NOT NULL AND COL_LENGTH('dbo.brands', 'brand_name') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.brands WHERE brand_name IS NULL)
            THROW 50001, 'dbo.brands.brand_name contains NULL values', 1;

        DECLARE @brands_drop_name sysname;
        DECLARE @brands_drop_is_constraint bit;

        DECLARE brands_drop_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT
            COALESCE(kc.name, i.name) AS drop_name,
            CASE WHEN kc.name IS NULL THEN 0 ELSE 1 END AS is_constraint
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        LEFT JOIN sys.key_constraints kc
            ON kc.parent_object_id = i.object_id
            AND kc.unique_index_id = i.index_id
            AND kc.[type] = 'UQ'
        WHERE i.object_id = OBJECT_ID(N'dbo.brands')
          AND ic.is_included_column = 0
          AND c.name = N'brand_name'
          AND i.is_primary_key = 0
          AND i.is_hypothetical = 0;

        OPEN brands_drop_cursor;
        FETCH NEXT FROM brands_drop_cursor INTO @brands_drop_name, @brands_drop_is_constraint;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @brands_drop_is_constraint = 1
                EXEC(N'ALTER TABLE dbo.brands DROP CONSTRAINT [' + @brands_drop_name + N']');
            ELSE
                EXEC(N'DROP INDEX [' + @brands_drop_name + N'] ON dbo.brands');

            FETCH NEXT FROM brands_drop_cursor INTO @brands_drop_name, @brands_drop_is_constraint;
        END
        CLOSE brands_drop_cursor;
        DEALLOCATE brands_drop_cursor;

        ALTER TABLE dbo.brands ALTER COLUMN brand_name NVARCHAR(100) NOT NULL;

        IF EXISTS (
            SELECT 1
            FROM dbo.brands
            GROUP BY brand_name
            HAVING COUNT(*) > 1
        )
            THROW 50002, 'dbo.brands.brand_name contains duplicate values', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.key_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.brands')
              AND [type] = 'UQ'
              AND name = N'UQ_brands_brand_name'
        )
            ALTER TABLE dbo.brands ADD CONSTRAINT UQ_brands_brand_name UNIQUE (brand_name);
    END

    IF OBJECT_ID(N'dbo.inspections', N'U') IS NOT NULL AND COL_LENGTH('dbo.inspections', 'reject_reason') IS NOT NULL
        ALTER TABLE dbo.inspections ALTER COLUMN reject_reason NVARCHAR(MAX) NULL;

    IF OBJECT_ID(N'dbo.inspection_components', N'U') IS NOT NULL AND COL_LENGTH('dbo.inspection_components', 'component_name') IS NOT NULL
        ALTER TABLE dbo.inspection_components ALTER COLUMN component_name NVARCHAR(255) NULL;

    IF OBJECT_ID(N'dbo.inspection_locations', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.inspection_locations', 'address_line') IS NOT NULL
        BEGIN
            UPDATE dbo.inspection_locations
            SET address_line = N''
            WHERE address_line IS NULL;

            ALTER TABLE dbo.inspection_locations ALTER COLUMN address_line NVARCHAR(255) NOT NULL;
        END

        IF COL_LENGTH('dbo.inspection_locations', 'city') IS NOT NULL
            ALTER TABLE dbo.inspection_locations ALTER COLUMN city NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.inspection_locations', 'contact_name') IS NOT NULL
            ALTER TABLE dbo.inspection_locations ALTER COLUMN contact_name NVARCHAR(255) NULL;
        IF COL_LENGTH('dbo.inspection_locations', 'district') IS NOT NULL
            ALTER TABLE dbo.inspection_locations ALTER COLUMN district NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.inspection_locations', 'ward') IS NOT NULL
            ALTER TABLE dbo.inspection_locations ALTER COLUMN ward NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.inspection_locations', 'note') IS NOT NULL
            ALTER TABLE dbo.inspection_locations ALTER COLUMN note NVARCHAR(MAX) NULL;
    END

    IF OBJECT_ID(N'dbo.inspection_scores', N'U') IS NOT NULL AND COL_LENGTH('dbo.inspection_scores', 'note') IS NOT NULL
        ALTER TABLE dbo.inspection_scores ALTER COLUMN note NVARCHAR(MAX) NULL;

    IF OBJECT_ID(N'dbo.kyc_profiles', N'U') IS NOT NULL AND COL_LENGTH('dbo.kyc_profiles', 'placeOfResidence') IS NOT NULL
        ALTER TABLE dbo.kyc_profiles ALTER COLUMN placeOfResidence NVARCHAR(255) NULL;

    IF OBJECT_ID(N'dbo.listings', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.listings', 'title') IS NOT NULL
            ALTER TABLE dbo.listings ALTER COLUMN title NVARCHAR(255) NULL;
        IF COL_LENGTH('dbo.listings', 'description') IS NOT NULL
            ALTER TABLE dbo.listings ALTER COLUMN description NVARCHAR(MAX) NULL;
    END

    IF OBJECT_ID(N'dbo.orders', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.orders', 'delivery_detail') IS NOT NULL
            ALTER TABLE dbo.orders ALTER COLUMN delivery_detail NVARCHAR(MAX) NULL;
        IF COL_LENGTH('dbo.orders', 'delivery_district') IS NOT NULL
            ALTER TABLE dbo.orders ALTER COLUMN delivery_district NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.orders', 'delivery_full_name') IS NOT NULL
            ALTER TABLE dbo.orders ALTER COLUMN delivery_full_name NVARCHAR(255) NULL;
        IF COL_LENGTH('dbo.orders', 'delivery_province') IS NOT NULL
            ALTER TABLE dbo.orders ALTER COLUMN delivery_province NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.orders', 'delivery_ward') IS NOT NULL
            ALTER TABLE dbo.orders ALTER COLUMN delivery_ward NVARCHAR(100) NULL;
    END

    IF OBJECT_ID(N'dbo.return_requests', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.return_requests', 'detail') IS NOT NULL
            ALTER TABLE dbo.return_requests ALTER COLUMN detail NVARCHAR(MAX) NULL;
        IF COL_LENGTH('dbo.return_requests', 'reason') IS NOT NULL
            ALTER TABLE dbo.return_requests ALTER COLUMN reason NVARCHAR(MAX) NULL;
    END

    IF OBJECT_ID(N'dbo.reviews', N'U') IS NOT NULL AND COL_LENGTH('dbo.reviews', 'description') IS NOT NULL
        ALTER TABLE dbo.reviews ALTER COLUMN description NVARCHAR(MAX) NULL;

    IF OBJECT_ID(N'dbo.types', N'U') IS NOT NULL AND COL_LENGTH('dbo.types', 'name') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.types WHERE name IS NULL)
            THROW 50003, 'dbo.types.name contains NULL values', 1;

        DECLARE @types_drop_name sysname;
        DECLARE @types_drop_is_constraint bit;

        DECLARE types_drop_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT
            COALESCE(kc.name, i.name) AS drop_name,
            CASE WHEN kc.name IS NULL THEN 0 ELSE 1 END AS is_constraint
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        LEFT JOIN sys.key_constraints kc
            ON kc.parent_object_id = i.object_id
            AND kc.unique_index_id = i.index_id
            AND kc.[type] = 'UQ'
        WHERE i.object_id = OBJECT_ID(N'dbo.types')
          AND ic.is_included_column = 0
          AND c.name = N'name'
          AND i.is_primary_key = 0
          AND i.is_hypothetical = 0;

        OPEN types_drop_cursor;
        FETCH NEXT FROM types_drop_cursor INTO @types_drop_name, @types_drop_is_constraint;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @types_drop_is_constraint = 1
                EXEC(N'ALTER TABLE dbo.types DROP CONSTRAINT [' + @types_drop_name + N']');
            ELSE
                EXEC(N'DROP INDEX [' + @types_drop_name + N'] ON dbo.types');

            FETCH NEXT FROM types_drop_cursor INTO @types_drop_name, @types_drop_is_constraint;
        END
        CLOSE types_drop_cursor;
        DEALLOCATE types_drop_cursor;

        ALTER TABLE dbo.types ALTER COLUMN name NVARCHAR(100) NOT NULL;

        IF EXISTS (
            SELECT 1
            FROM dbo.types
            GROUP BY name
            HAVING COUNT(*) > 1
        )
            THROW 50004, 'dbo.types.name contains duplicate values', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.key_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.types')
              AND [type] = 'UQ'
              AND name = N'UQ_types_name'
        )
            ALTER TABLE dbo.types ADD CONSTRAINT UQ_types_name UNIQUE (name);
    END

    IF OBJECT_ID(N'dbo.wallet_transactions', N'U') IS NOT NULL AND COL_LENGTH('dbo.wallet_transactions', 'note') IS NOT NULL
        ALTER TABLE dbo.wallet_transactions ALTER COLUMN note NVARCHAR(MAX) NULL;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH
