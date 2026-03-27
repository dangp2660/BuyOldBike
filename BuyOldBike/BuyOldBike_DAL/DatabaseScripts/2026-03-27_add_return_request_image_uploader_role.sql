ALTER TABLE dbo.return_request_images
ADD uploader_role VARCHAR(20) NOT NULL
    CONSTRAINT DF_return_request_images_uploader_role DEFAULT ('Buyer');

