using System;
using System.IO;
using System.Threading;

namespace BuyOldBike_Presentation.Services
{
    public class LocalMediaService
    {
        private readonly string _mediaRoot; // relative path used in URLs
        private readonly string _mediaRootFullPath;

        public LocalMediaService(string mediaRoot)
        {
            _mediaRoot = string.IsNullOrWhiteSpace(mediaRoot) ? "Uploads" : mediaRoot;
            _mediaRootFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _mediaRoot);
            if (!Directory.Exists(_mediaRootFullPath))
                Directory.CreateDirectory(_mediaRootFullPath);
        }

        // Saves a source image file into media root with a GUID filename.
        // Returns a relative URL suitable for ListingImage.ImageUrl (e.g. "Uploads/{guid}.jpg").
        public string SaveImage(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new ArgumentException("sourceFilePath is required", nameof(sourceFilePath));

            string ext = Path.GetExtension(sourceFilePath);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            string fileName = Guid.NewGuid().ToString("N") + ext;
            string destPath = Path.Combine(_mediaRootFullPath, fileName);

            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Copy(sourceFilePath, destPath, true);
                    // Return forward-slash path for consistency with existing code ("Uploads/...")
                    return _mediaRoot.Replace('\\', '/') + "/" + fileName;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    // simple backoff between retries
                    Thread.Sleep(100 * attempt);
                }
            }

            // Final attempt (let exception bubble if it fails)
            File.Copy(sourceFilePath, destPath, true);
            return _mediaRoot.Replace('\\', '/') + "/" + fileName;
        }
    }
}