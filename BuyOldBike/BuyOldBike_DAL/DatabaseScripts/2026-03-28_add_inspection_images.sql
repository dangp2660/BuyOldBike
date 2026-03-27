IF OBJECT_ID(N'dbo.inspection_images', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.inspection_images
    (
        image_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inspection_images_image_id DEFAULT (NEWID()),
        inspection_id UNIQUEIDENTIFIER NOT NULL,
        image_url VARCHAR(500) NOT NULL,
        created_at DATETIME NULL,
        CONSTRAINT PK_inspection_images PRIMARY KEY (image_id),
        CONSTRAINT FK_inspection_images_inspections
            FOREIGN KEY (inspection_id) REFERENCES dbo.inspections(inspection_id)
    );

    CREATE INDEX IX_inspection_images_inspection_id ON dbo.inspection_images(inspection_id);
END

