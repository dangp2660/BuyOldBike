SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.listings', N'U') IS NOT NULL AND COL_LENGTH('dbo.listings', 'views') IS NULL
BEGIN
    ALTER TABLE dbo.listings
    ADD views INT NOT NULL CONSTRAINT DF_listings_views DEFAULT (0) WITH VALUES;
END

