using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.Controls
{
    public partial class ImageUploaderControl : UserControl
    {
        public event Action<List<string>>? FilesSelected;

        private readonly List<string> _selected = new List<string>();

        public ImageUploaderControl()
        {
            InitializeComponent();
            BtnSelect.Click += BtnSelect_Click;
        }

        private void BtnSelect_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    if (_selected.Contains(f)) continue;
                    if (_selected.Count >= 10) break;
                    _selected.Add(f);
                    Thumbnails.Items.Add(CreatePreviewImage(f));
                }

                FilesSelected?.Invoke(new List<string>(_selected));
            }
        }

        private static BitmapImage CreatePreviewImage(string path)
        {
            if (!File.Exists(path)) return null!;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}