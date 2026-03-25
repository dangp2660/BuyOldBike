# Kế hoạch: Click listing ở BicycleListWindow mở trang chi tiết

## 1) Summary

Cho phép người dùng click vào card/listing trong `BicycleListWindow` để mở `ListingDetailWindow` (modal) và hiển thị chi tiết đúng listing tương ứng.

## 2) Current State Analysis (hiện trạng)

* `BicycleListWindow` render danh sách listing bằng `ItemsControl` + `DataTemplate` là một `Border`, hiện **không có** event/command để click mở chi tiết: [BicycleListWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml#L110-L171).

* `BicycleListWindow.xaml.cs` chỉ xử lý auth UI và phân trang, **không có** handler cho listing: [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs).

* `ListingDetailWindow(Guid listingId)` đã tồn tại và load data theo `listingId`: [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs#L7-L15).

* `ListingDetailViewModel.Load` hiện query `BuyOldBikeContext` trực tiếp trong Presentation (chưa theo 3-layer): [ListingDetailViewModel.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/ViewModels/ListingDetailViewModel.cs#L15-L34).

* BLL đã có `ListingBrowseService` và DAL có `BikePostRepository`; trong task này chỉ **check** hiện trạng (không sửa 2 tầng này): [ListingBrowseService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/BicycleListWindow/ListingBrowseService.cs#L7-L21), [BikePostRepository.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs#L133-L142).

## 3) Assumptions & Decisions

* Mở chi tiết theo kiểu **modal** (`ShowDialog()`), danh sách vẫn giữ nguyên phía sau.

* Click nút **♥** trên card **không mở** detail.

* Theo yêu cầu: **chỉ code ở tầng Presentation**; hai tầng BLL/DAL chỉ “check” (xác nhận đang có sẵn luồng dữ liệu cần thiết), không thay đổi code.

## 4) Proposed Changes (cụ thể từng file)

### 4.1 Presentation: bắt click trên card listing

**File:** [BicycleListWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml)

* Thêm `MouseLeftButtonUp` (hoặc `MouseUp`) cho `Border` của card để bắt click.

* Set `Cursor="Hand"` để có UX rõ ràng khi hover.

* Giữ nguyên nút ♥ là `Button`; do Button xử lý mouse event riêng, click vào ♥ sẽ không bubble lên handler của card (đảm bảo “không mở detail khi click ♥”).

**File:** [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs)

* Thêm handler `ListingCard_MouseLeftButtonUp`.

* Trong handler: lấy `ListingId` từ `DataContext` (`BicycleCardVm`) rồi mở `new ListingDetailWindow(listingId) { Owner = this }` và gọi `ShowDialog()`.

* Nếu `ListingId` rỗng/không hợp lệ: hiển thị `MessageBox` và không mở cửa sổ.

### 4.2 (Tuỳ chọn nhưng khuyến nghị) UX khi listing không tồn tại

**File:** [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs)

* Sau `vm.Load(listingId)`, nếu `vm.Listing == null`: thông báo “Không tìm thấy listing” và `Close()` luôn để tránh cửa sổ trống.

## 5) Verification (cách tự kiểm tra sau khi implement)

* Chạy app, vào `BicycleListWindow`:

  * Click vào card bất kỳ → mở `ListingDetailWindow` modal và hiện đúng `ListingId/Title/Price/...`.

  * Click nút “Đóng” → quay về danh sách, không mất trạng thái đăng nhập.

  * Click nút ♥ → không mở detail.

* Smoke check: mở detail từ `SellerWindow` (luồng cũ) vẫn hoạt động bình thường.

