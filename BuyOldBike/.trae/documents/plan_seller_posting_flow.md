# FULL CODE: LƯU BÀI ĐĂNG & LƯU ẢNH VÀO DATABASE (3 LỚP)

Tài liệu này nhấn mạnh việc lưu thông tin bài đăng và **lưu URL của từng ảnh vào bảng ListingImages** trong database.

***

## 1. LỚP TRUY XUẤT DỮ LIỆU (DAL)

### File: `BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs`

Đây là nơi quan trọng nhất xử lý việc lưu đồng thời vào 3 bảng: `Listings`, `ListingImages`, và `Inspections`.

```csharp
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class BikePostRepository
    {
        private readonly BuyOldBikeContext _context = new BuyOldBikeContext();

        // HÀM LƯU TẤT CẢ DỮ LIỆU VÀO DB
        public async Task SavePostAndImagesToDbAsync(Listing listing, List<string> imageUrls, Inspection inspection)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bước 1: Lưu bài đăng (Listing)
                await _context.Listings.AddAsync(listing);
                await _context.SaveChangesAsync();

                // Bước 2: Lưu danh sách URL ảnh vào bảng ListingImages
                foreach (var url in imageUrls)
                {
                    var imageRecord = new ListingImage
                    {
                        ListingImageId = Guid.NewGuid(),
                        ListingId = listing.ListingId, // Liên kết với bài đăng vừa tạo
                        ImageUrl = url                 // <--- LƯU URL ẢNH VÀO DB TẠI ĐÂY
                    };
                    await _context.ListingImages.AddAsync(imageRecord);
                }

                // Bước 3: Lưu đơn kiểm định (Inspection)
                inspection.ListingId = listing.ListingId;
                await _context.Inspections.AddAsync(inspection);

                // Hoàn tất lưu tất cả
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
```

***

## 2. LỚP NGHIỆP VỤ (BLL)

### File: `BuyOldBike_BLL/Services/ListingService.cs`

```csharp
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services
{
    public class ListingService
    {
        private readonly BikePostRepository _repo = new BikePostRepository();

        public async Task CreatePostWithImagesAsync(Listing listing, List<string> imagePaths)
        {
            listing.ListingId = Guid.NewGuid();
            listing.Status = "Pending_Inspection";
            listing.CreatedAt = DateTime.Now;

            var inspection = new Inspection
            {
                InspectionId = Guid.NewGuid(),
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            // Gọi repository để lưu bài đăng và các URL ảnh vào DB
            await _repo.SavePostAndImagesToDbAsync(listing, imagePaths, inspection);
        }
    }
}
```

***

## 3. LỚP GIAO DIỆN (PRESENTATION)

### File: `BuyOldBike_Presentation/Views/SellerWindow.xaml.cs`

```csharp
using BuyOldBike_BLL.Services;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class SellerWindow : Window
    {
        private readonly ListingService _listingService = new ListingService();
        private List<string> _selectedFilePaths = new List<string>();

        // 1. Nút chọn ảnh từ máy tính
        private void btnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                _selectedFilePaths.AddRange(dialog.FileNames);
                MessageBox.Show($"Đã chọn {_selectedFilePaths.Count} ảnh.");
            }
        }

        // 2. Nút Đăng bài
        private async void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Copy ảnh vào thư mục dự án và tạo URL
                List<string> imageUrlsForDb = new List<string>();
                string uploadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                foreach (var path in _selectedFilePaths)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(path);
                    File.Copy(path, Path.Combine(uploadPath, fileName));
                    
                    // Đây là URL sẽ được lưu vào Database
                    imageUrlsForDb.Add("Uploads/" + fileName);
                }

                var listing = new Listing {
                    Title = txtTitle.Text,
                    Price = decimal.Parse(txtPrice.Text),
                    SellerId = AppSession.CurrentUser.UserId
                };

                // Gửi sang Service để lưu vào DB (bao gồm cả danh sách URL ảnh)
                await _listingService.CreatePostWithImagesAsync(listing, imageUrlsForDb);
                
                MessageBox.Show("Đã lưu bài đăng và toàn bộ ảnh vào Database!");
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
    }
}
```

