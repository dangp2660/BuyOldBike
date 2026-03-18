# Kế hoạch: Luồng Inspector (4/5 tiêu chí) + Hiển thị xe đã pass trên BicycleListWindow

## 1) Tóm tắt

* Cập nhật luồng kiểm định để **một listing được “pass” khi đạt ≥ 4/5 tiêu chí** trong tab *Inspection* của InspectorWindow.

* Khi “pass”: set `Inspection.Result = Passed`, `Inspection.Status = Completed`, và **`Listing.Status = Available`** (đã chốt theo lựa chọn).

* Cập nhật BicycleListWindow để **load dữ liệu thật từ DB** và **chỉ hiển thị các listing đã pass** (tương ứng status `Available`).

## 2) Phân tích hiện trạng (đã kiểm tra trong repo)

### Inspector

* UI checklist có 5 tiêu chí (Frame, Brake, Drivetrain, Wheel Alignment, Handlebar/Steering) nhưng code-behind hiện chỉ đọc `rbPass`/`rbFail` (GroupName="Overall") để quyết định pass/fail.

  * File: [InspectorWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml#L232-L341)

  * File: [InspectorWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml.cs#L34-L57)

* BLL/DAL hiện map pass/fail:

  * `Inspection.Result = Passed/Failed`

  * `Listing.Status = Available/Rejected`

  * Files:

    * [InspectionService.ProcessInspection](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Seller/InspectionService.cs#L25-L31)

    * [BikePostRepository.UpdateInspectionResult](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs#L66-L81)

* `Inspection` entity đã có sẵn `OverallScore` và `RejectReason` (có thể tận dụng để lưu tổng điểm/ghi chú).

  * File: [Inspection.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Inspection.cs#L20-L33)

### BicycleListWindow

* UI đang hiển thị “card” bằng dữ liệu tĩnh, không bind DB.

  * File: [BicycleListWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml#L87-L176)

* Code-behind chỉ xử lý login/profile/logout, chưa load listing.

  * File: [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs#L31-L35)

## 3) Thay đổi đề xuất (decision-complete)

### 3.1. InspectorWindow: tính pass theo 4/5 tiêu chí

**Files sẽ sửa**

* [InspectorWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml)

* [InspectorWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml.cs)

**Cách làm**

* Trong XAML:

  * Đặt `x:Name` cho radio “Pass” của từng tiêu chí (5 cái) để code-behind đọc trực tiếp (tránh phải dò VisualTree).

  * Sửa `GroupName` đang gây nhầm (“Overall”, “Suspension”) thành đúng theo từng tiêu chí để đảm bảo 2 radio trong một tiêu chí loại trừ nhau.

  * Đặt `x:Name="txtNotes"` cho TextBox Notes và bỏ nội dung text mẫu (hoặc để rỗng) để lấy ghi chú thực.

* Trong code-behind `btnComplete_Click`:

  * Lấy `selectedInspection` như hiện tại.

  * Tính `passCount` từ 5 radio “Pass”:

    * `passCount = (rbFramePass.IsChecked ? 1 : 0) + ...`

  * `isPassed = passCount >= 4`.

  * Gọi service xử lý, truyền thêm `passCount` và `notes` để lưu `Inspection.OverallScore` / `Inspection.RejectReason` (RejectReason chỉ set khi fail).

  * Refresh lại grid pending sau khi lưu.

**Tiêu chí đúng**

* Nếu inspector chọn fail ở ≥ 2 tiêu chí (tức passCount ≤ 3) thì listing bị “fail”.

* Nếu passCount = 4 hoặc 5 thì listing “pass” và được public.

### 3.2. BLL/DAL: lưu kết quả + score + notes (không thay đổi schema)

**Files sẽ sửa**

* [InspectionService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Seller/InspectionService.cs)

* [BikePostRepository.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs)

**Cách làm**

* Mở rộng `InspectionService.ProcessInspection(...)` nhận thêm:

  * `int overallScore` (0–5)

  * `string? notes`

* Trong repo `UpdateInspectionResult(...)`:

  * Set `Inspection.Status = Completed`

  * Set `Inspection.Result = Passed/Failed`

  * Set `Inspection.OverallScore = overallScore`

  * Nếu failed: set `Inspection.RejectReason = notes`; nếu passed: có thể clear `RejectReason`

  * Set `Listing.Status = Available` nếu passed, ngược lại `Rejected`

**Quyết định trạng thái (đã chốt)**

* Dùng `Listing.Status = StatusConstants.ListingStatus.Available` để biểu diễn “pass và được hiển thị trên marketplace”.

### 3.3. BicycleListWindow: load DB và chỉ hiển thị xe đã pass

**Files sẽ sửa/thêm**

* Sửa:

  * [BicycleListWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml)

  * [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs)

* Thêm mới:

  * `BuyOldBike_Presentation/ViewModels/BicycleListWindowViewModel.cs`

  * `BuyOldBike_Presentation/ViewModels/BicycleCardVm.cs` (hoặc model đơn giản tương tự)

  * `BuyOldBike_BLL/Services/ListingBrowseService.cs` (service mới cho marketplace)

* Sửa DAL:

  * Thêm method trong [BikePostRepository.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs) để lấy listing public.

**Cách làm**

* DAL:

  * Thêm `GetAvailableListings()`:

    * Filter `Listing.Status == StatusConstants.ListingStatus.Available`

    * Include `Brand`, `BikeType`, `ListingImages`

    * OrderByDesc `CreatedAt`

* BLL:

  * `ListingBrowseService.GetAvailableListings()` gọi repo và trả về danh sách listing.

* Presentation:

  * Tạo `BicycleListWindowViewModel` có `ObservableCollection<BicycleCardVm> Listings`.

  * Trong `BicycleListWindow`:

    * Set `DataContext = new BicycleListWindowViewModel()` ở constructor.

    * Load dữ liệu ở `Loaded` và `Activated` (để khi inspector vừa cập nhật thì quay lại list sẽ thấy ngay).

  * Sửa XAML:

    * Thay `WrapPanel` hard-code bằng `ItemsControl` bind `ItemsSource` tới `Listings`.

    * Dùng `ItemsPanelTemplate` là `WrapPanel` để giữ layout card như cũ.

    * Template card lấy các field cơ bản: `Title`, `Price`, `BrandName`, và ảnh đầu tiên (nếu có) hoặc placeholder.

## 4) Giả định & quyết định

* Pass = **đạt ≥ 4/5 tiêu chí** trong UI checklist.

* Trạng thái “pass” của listing được lưu bằng `Listing.Status = Available` (đúng với hằng số hiện có).

* BicycleListWindow chỉ hiển thị listing `Available` để đáp ứng “pass mới hiện”.

* Không triển khai lưu chi tiết từng component vào `InspectionScores` ở bước này (vì cần map `ComponentId`/seed dữ liệu); chỉ lưu `OverallScore` + `RejectReason` để đủ theo yêu cầu và có trace cơ bản.

## 5) Cách kiểm tra/Verify

* Tạo 1 listing mới từ luồng seller (status phải là `Pending_Inspection`).

* Đăng nhập inspector mở InspectorWindow:

  * Pending inspections phải thấy listing vừa tạo.

  * Chọn 5 tiêu chí sao cho passCount = 4 → bấm “Hoàn tất”:

    * DB: `Inspection.Status = Completed`, `Inspection.Result = Passed`, `Listing.Status = Available`.

  * Tạo case passCount = 3 → listing status phải `Rejected`.

* Mở BicycleListWindow:

  * Listing vừa pass phải xuất hiện trong danh sách.

  * Listing bị rejected không xuất hiện.

## 6) Hướng dẫn code step-by-step (kèm code copy/paste)

### Bước 1 — InspectorWindow\.xaml: đặt name + group đúng cho 5 tiêu chí, đặt name cho Notes

**File:** [InspectorWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml#L232-L341)

Thay toàn bộ 5 cụm RadioButton trong “Inspection Checklist” thành (giữ nguyên Style resource như file đang có):

```xml
<TextBlock Grid.Row="0" Grid.Column="0" Style="{StaticResource InlineLabelStyle}" Text="Frame System" />
<StackPanel Grid.Row="0" Grid.Column="2" Orientation="Horizontal">
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="FrameSystem" Content="Pass" IsChecked="True" x:Name="rbFramePass" />
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="FrameSystem" Content="Fail" />
</StackPanel>

<TextBlock Grid.Row="2" Grid.Column="0" Style="{StaticResource InlineLabelStyle}" Text="Brake System" />
<StackPanel Grid.Row="2" Grid.Column="2" Orientation="Horizontal">
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="BrakeSystem" Content="Pass" IsChecked="True" x:Name="rbBrakePass" />
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="BrakeSystem" Content="Fail" />
</StackPanel>

<TextBlock Grid.Row="4" Grid.Column="0" Style="{StaticResource InlineLabelStyle}" Text="Drivetrain" />
<StackPanel Grid.Row="4" Grid.Column="2" Orientation="Horizontal">
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="Drivetrain" Content="Pass" IsChecked="True" x:Name="rbDrivetrainPass" />
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="Drivetrain" Content="Fail" />
</StackPanel>

<TextBlock Grid.Row="6" Grid.Column="0" Style="{StaticResource InlineLabelStyle}" Text="Wheel Alignment" />
<StackPanel Grid.Row="6" Grid.Column="2" Orientation="Horizontal">
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="WheelAlignment" Content="Pass" IsChecked="True" x:Name="rbWheelAlignmentPass" />
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="WheelAlignment" Content="Fail" />
</StackPanel>

<TextBlock Grid.Row="8" Grid.Column="0" Style="{StaticResource InlineLabelStyle}" Text="Handlebar and Steering" />
<StackPanel Grid.Row="8" Grid.Column="2" Orientation="Horizontal">
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="HandlebarSteering" Content="Pass" IsChecked="True" x:Name="rbHandlebarSteeringPass" />
    <RadioButton Style="{StaticResource SmallRadioStyle}" GroupName="HandlebarSteering" Content="Fail" />
</StackPanel>
```

Sau đó đổi TextBox Notes thành:

```xml
<TextBox Margin="0,12,0,0"
         x:Name="txtNotes"
         Height="180"
         Padding="10,8"
         TextWrapping="Wrap"
         AcceptsReturn="True"
         VerticalContentAlignment="Top"
         Text="" />
```

### Bước 2 — InspectorWindow\.xaml.cs: tính passCount (0–5) và pass khi ≥ 4

**File:** [InspectorWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml.cs#L34-L57)

Thay toàn bộ hàm `btnComplete_Click` bằng:

```csharp
private void btnComplete_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var selectedInspection = dgPendingInspections.SelectedItem as Inspection;
        if (selectedInspection == null)
        {
            MessageBox.Show("Vui lòng chọn một đơn kiểm định từ danh sách!");
            return;
        }

        int passCount = 0;
        if (rbFramePass.IsChecked == true) passCount++;
        if (rbBrakePass.IsChecked == true) passCount++;
        if (rbDrivetrainPass.IsChecked == true) passCount++;
        if (rbWheelAlignmentPass.IsChecked == true) passCount++;
        if (rbHandlebarSteeringPass.IsChecked == true) passCount++;

        bool isPassed = passCount >= 4;
        string? notes = txtNotes.Text;

        _inspectionService.ProcessInspection(selectedInspection.InspectionId, isPassed, passCount, notes);

        MessageBox.Show("Đã cập nhật kết quả kiểm định thành công!");
        LoadData();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Lỗi khi xử lý kiểm định: {ex.Message}");
    }
}
```

### Bước 3 — InspectionService.cs: đổi chữ ký ProcessInspection và truyền score/notes xuống repo

**File:** [InspectionService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Seller/InspectionService.cs)

Thay class bằng (giữ nguyên namespace):

```csharp
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using BuyOldBike_DAL.Constants;
using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Services.Seller
{
    public class InspectionService
    {
        private readonly BikePostRepository _bikePostRepository;

        public InspectionService()
        {
            _bikePostRepository = new BikePostRepository();
        }

        public List<Inspection> GetPendingRequests()
        {
            return _bikePostRepository.GetPendingInspections();
        }

        public void ProcessInspection(Guid inspectionId, bool isPassed, int overallScore, string? notes)
        {
            string result = isPassed ? StatusConstants.InspectionResult.Passed : StatusConstants.InspectionResult.Failed;
            string listingStatus = isPassed ? StatusConstants.ListingStatus.Available : StatusConstants.ListingStatus.Rejected;

            _bikePostRepository.UpdateInspectionResult(inspectionId, result, listingStatus, overallScore, notes);
        }
    }
}
```

### Bước 4 — BikePostRepository.cs: update result + score + notes, thêm query GetAvailableListings

**File:** [BikePostRepository.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs)

1. Thay method `UpdateInspectionResult(...)` hiện tại bằng:

```csharp
public void UpdateInspectionResult(Guid inspectionId, string result, string listingStatus, int overallScore, string? notes)
{
    var inspection = _db.Inspections.Find(inspectionId);
    if (inspection != null)
    {
        inspection.Status = StatusConstants.InspectionStatus.Completed;
        inspection.Result = result;
        inspection.OverallScore = overallScore;

        if (result == StatusConstants.InspectionResult.Passed)
        {
            inspection.RejectReason = null;
        }
        else
        {
            inspection.RejectReason = notes;
        }

        var listing = _db.Listings.Find(inspection.ListingId);
        if (listing != null)
        {
            listing.Status = listingStatus;
        }

        _db.SaveChanges();
    }
}
```

1. Thêm method mới (đặt sau `GetListingsBySellerId` hoặc nơi bạn muốn):

```csharp
public List<Listing> GetAvailableListings()
{
    return _db.Listings
        .Include(l => l.Brand)
        .Include(l => l.BikeType)
        .Include(l => l.ListingImages)
        .Where(l => l.Status == StatusConstants.ListingStatus.Available)
        .OrderByDescending(l => l.CreatedAt)
        .ToList();
}
```

### Bước 5 — Tạo ListingBrowseService.cs (BLL) để BicycleListWindow gọi

**File mới:** `BuyOldBike_BLL/Services/ListingBrowseService.cs`

```csharp
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Services
{
    public class ListingBrowseService
    {
        private readonly BikePostRepository _repo;

        public ListingBrowseService()
        {
            _repo = new BikePostRepository();
        }

        public List<Listing> GetAvailableListings()
        {
            return _repo.GetAvailableListings();
        }
    }
}
```

### Bước 6 — Tạo ViewModel cho BicycleListWindow + đổi XAML sang ItemsControl

**File mới:** `BuyOldBike_Presentation/ViewModels/BicycleCardVm.cs`

```csharp
using System;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleCardVm
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public string BrandName { get; set; } = "";
        public BitmapImage? Image { get; set; }
    }
}
```

**File mới:** `BuyOldBike_Presentation/ViewModels/BicycleListWindowViewModel.cs`

```csharp
using BuyOldBike_BLL.Services;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleListWindowViewModel
    {
        private readonly ListingBrowseService _service;

        public ObservableCollection<BicycleCardVm> Listings { get; } = new ObservableCollection<BicycleCardVm>();

        public BicycleListWindowViewModel()
        {
            _service = new ListingBrowseService();
        }

        public void Load()
        {
            Listings.Clear();
            foreach (Listing listing in _service.GetAvailableListings())
            {
                BitmapImage? image = null;
                var first = listing.ListingImages?.FirstOrDefault();
                if (first != null && !string.IsNullOrWhiteSpace(first.ImageUrl))
                {
                    try
                    {
                        image = new BitmapImage(new Uri(first.ImageUrl, UriKind.RelativeOrAbsolute));
                    }
                    catch
                    {
                        image = null;
                    }
                }

                Listings.Add(new BicycleCardVm
                {
                    ListingId = listing.ListingId,
                    Title = listing.Title ?? "",
                    Price = listing.Price ?? 0,
                    BrandName = listing.Brand?.BrandName ?? "",
                    Image = image
                });
            }
        }
    }
}
```

**Sửa BicycleListWindow\.xaml** (thay WrapPanel hard-code trong `ScrollViewer Grid.Column="2"` bằng ItemsControl):

```xml
<ItemsControl ItemsSource="{Binding Listings}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel HorizontalAlignment="Center" />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Style="{StaticResource BicycleCardBorderStyle}">
                <StackPanel>
                    <Border Height="120"
                            Background="#F3F4F6"
                            CornerRadius="10">
                        <Grid>
                            <Image Source="{Binding Image}" Stretch="UniformToFill" />
                            <TextBlock HorizontalAlignment="Center"
                                       VerticalAlignment="Center"
                                       Foreground="#6B7280"
                                       Text="Ảnh xe">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock">
                                        <Setter Property="Visibility" Value="Collapsed" />
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding Image}" Value="{x:Null}">
                                                <Setter Property="Visibility" Value="Visible" />
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </Grid>
                    </Border>

                    <TextBlock Margin="0,10,0,0"
                               FontSize="14"
                               FontWeight="SemiBold"
                               Text="{Binding Title}" />
                    <TextBlock Margin="0,6,0,0"
                               FontSize="16"
                               FontWeight="Bold"
                               Text="{Binding Price, StringFormat={}{0:N0}đ}" />
                    <TextBlock Margin="0,6,0,0"
                               Foreground="#6B7280"
                               Text="{Binding BrandName}" />

                    <StackPanel Margin="0,10,0,0" Orientation="Horizontal">
                        <Border Width="150"
                                Padding="8,4"
                                Background="#ECFDF5"
                                CornerRadius="999">
                            <TextBlock Foreground="#047857"
                                       FontSize="12"
                                       Text="Đã kiểm định" />
                        </Border>
                        <Button Margin="10,0,0,0"
                                Style="{StaticResource IconButtonStyle}"
                                Content="♥" />
                    </StackPanel>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Sửa BicycleListWindow\.xaml.cs**:

* Thêm `using BuyOldBike_Presentation.ViewModels;`

* Trong constructor set DataContext và reload ở Loaded/Activated.

Copy/paste phần thay đổi:

```csharp
private readonly BicycleListWindowViewModel _vm = new BicycleListWindowViewModel();

public BicycleListWindow()
{
    InitializeComponent();
    DataContext = _vm;
    Loaded += BicycleListWindow_Loaded;
    Activated += BicycleListWindow_Activated;
    Unloaded += BicycleListWindow_Unloaded;
    AppSession.CurrentUserChanged += AppSession_CurrentUserChanged;
}

private void BicycleListWindow_Loaded(object sender, RoutedEventArgs e)
{
    UpdateAuthUi();
    _vm.Load();
}

private void BicycleListWindow_Activated(object? sender, EventArgs e)
{
    _vm.Load();
}
```

