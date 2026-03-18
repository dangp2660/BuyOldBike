# Plan: Main Flow (WPF BuyOldBike)

## Summary
- Tài liệu hoá “main flow” hiện tại của app WPF theo đúng code: startup → buyer home → login/register → điều hướng theo role → guard role → logout.
- Kết quả mong muốn: có 1 sơ đồ luồng (Mermaid) + tóm tắt theo từng bước, kèm link tới các file quan trọng để tra cứu nhanh.

## Current State Analysis (đã kiểm tra từ repo)
- Entrypoint WPF dùng `StartupUri="Views/BicycleListWindow.xaml"` trong [App.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/App.xaml#L1-L13).
- Auth/session đang là in-memory static `AppSession.CurrentUser` + event `CurrentUserChanged` (không có persist giữa các lần chạy app) trong [AppSession.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/AppSession.cs#L1-L26).
- Điều hướng theo role + chặn truy cập theo role nằm ở [RoleNavigator.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/RoleNavigator.cs#L1-L64) và role constants ở [RoleConstants.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Constants/RoleConstants.cs).
- Logout chuẩn hoá qua [LogoutManager.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/LogoutManager.cs#L1-L59): confirm → clear session → mở LoginWindow → đóng các window còn lại.
- Window “home mặc định” là [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs#L21-L102); Login flow nằm ở [LoginWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/LoginWindow.xaml.cs#L7-L49).

## Proposed Changes (tạo tài liệu main flow)
### 1) Sơ đồ luồng tổng quát (Mermaid)
Sử dụng sơ đồ dưới đây làm “map” 1 trang:

```mermaid
flowchart TD
  A[App start] --> B[BicycleListWindow (StartupUri)]
  B -->|Login click| L[LoginWindow]
  B -->|Profile menu (auth)| P[ProfileWindow (dialog)]
  B -->|Logout| O[LogoutManager]

  L -->|Register link| R[RegisterWindow]
  L -->|Login OK| S[AppSession.SetCurrentUser]
  S --> N[RoleNavigator.NavigateToHome]

  N -->|role Seller| HS[SellerWindow]
  N -->|role Inspector| HI[InspectorWindow]
  N -->|role Admin| HA[AdminWindow]
  N -->|default| HB[BicycleListWindow]

  HS -->|EnsureRole fail| N
  HI -->|EnsureRole fail| N
  HA -->|EnsureRole fail| N

  O --> C[AppSession.Clear]
  C --> L2[LoginWindow]
```

### 2) Tóm tắt main flow (theo từng bước)
- **Startup**
  - App mở [BicycleListWindow](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/App.xaml#L1-L13) do `StartupUri`.
- **Buyer home (mặc định)**
  - [BicycleListWindow](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs#L21-L102) lắng nghe `AppSession.CurrentUserChanged` và cập nhật UI (ẩn/hiện Login/Profile).
- **Đi tới Login**
  - Click Login: mở [LoginWindow](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/LoginWindow.xaml.cs#L14-L41) và đóng BicycleListWindow.
- **Đăng nhập thành công**
  - `LoginService.LoginAndGetUser` trả về `User` → set vào [AppSession](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/AppSession.cs#L14-L18).
  - Gọi [RoleNavigator.NavigateToHome](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/RoleNavigator.cs#L10-L24) để mở “home” tương ứng role và đóng LoginWindow.
- **Điều hướng theo role**
  - [RoleNavigator.CreateHomeWindow](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/RoleNavigator.cs#L53-L60): Seller → SellerWindow, Inspector → InspectorWindow, Admin → AdminWindow, còn lại → BicycleListWindow.
- **Role guard**
  - Các window khu vực gọi `RoleNavigator.EnsureRole(...)` để:
    - Chưa login → mở LoginWindow và đóng window hiện tại.
    - Sai role → mở “home theo role hiện tại” và đóng window hiện tại.
- **Logout**
  - [LogoutManager.Logout](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/LogoutManager.cs#L11-L31): confirm → clear session → mở LoginWindow → đóng các window khác.

## Assumptions & Decisions
- Người dùng đã skip phần chọn phạm vi, nên tài liệu này mặc định tập trung vào **Auth + điều hướng theo role** (medium detail).
- Không mô tả chi tiết luồng nghiệp vụ trong từng role (Seller/Inspector/Admin) ngoài phần guard + điểm vào/ra, vì “main flow” thường là entry/auth/navigation.

## Verification (đảm bảo plan đúng với code)
- Đối chiếu `StartupUri` trong App.xaml.
- Đối chiếu luồng: `LoginWindow` → `AppSession.SetCurrentUser` → `RoleNavigator.NavigateToHome`.
- Đối chiếu `EnsureRole` cho 3 role (Seller/Inspector/Admin) và `LogoutManager` đóng window.

