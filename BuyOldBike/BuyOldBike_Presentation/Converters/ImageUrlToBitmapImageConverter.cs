using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.Converters
{
    public class ImageUrlToBitmapImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? raw = value as string;
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string source = raw.Trim();

            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? absoluteUri))
            {
                return CreateBitmapImage(absoluteUri);
            }

            var normalizedPath = source.Replace('/', Path.DirectorySeparatorChar);
            string fullPath;

            if (Path.IsPathRooted(normalizedPath))
            {
                fullPath = normalizedPath;
            }
            else
            {
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, normalizedPath);
                if (!File.Exists(fullPath))
                {
                    fullPath = Path.GetFullPath(normalizedPath);
                }
            }

            if (File.Exists(fullPath))
                return CreateBitmapImage(new Uri(fullPath, UriKind.Absolute));

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static BitmapImage? CreateBitmapImage(Uri uri)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = uri;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
