# BuyOldBike — Giải thích code & database scripts

Tài liệu này giải thích dự án theo 2 mức:
- Mức “đọc hiểu nhanh”: kiến trúc, luồng gọi, file quan trọng.
- Mức “từng dòng / từng khối”: ưu tiên các entity + các SQL script trong [DatabaseScripts](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts).

## Mục lục
- [1. Kiến trúc tổng quan](#1-kiến-trúc-tổng-quan)
- [2. Entity Inspection.cs (từng dòng)](#2-entity-inspectioncs-từng-dòng)
- [3. InspectorWindow.LoadDisputeDetails (từng dòng)](#3-inspectorwindowloaddisputedetails-từng-dòng)
- [4. DatabaseScripts (từng dòng/từng khối)](#4-databasescripts-từng-dòngtừng-khối)
  - [4.1 2026-03-21_add_payment_order_vnpay.sql](#41-2026-03-21_add_payment_order_vnpaysql)
  - [4.2 2026-03-23_add_wallet.sql](#42-2026-03-23_add_walletsql)
  - [4.3 2026-03-25_add_order_delivery_address.sql](#43-2026-03-25_add_order_delivery_addresssql)
  - [4.4 2026-03-27_add_return_request_image_uploader_role.sql](#44-2026-03-27_add_return_request_image_uploader_rolesql)
  - [4.5 2026-03-27_convert_unicode_columns.sql](#45-2026-03-27_convert_unicode_columnssql)
  - [4.6 2026-03-27_recreate_tables.sql](#46-2026-03-27_recreate_tablessql)
  - [4.7 2026-03-28_add_inspection_images.sql](#47-2026-03-28_add_inspection_imagessql)
  - [4.8 2026-03-28_add_listing_views.sql](#48-2026-03-28_add_listing_viewssql)

---

## 1. Kiến trúc tổng quan

Repo gồm 4 project:
- WPF UI: [BuyOldBike_Presentation.csproj](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/BuyOldBike_Presentation.csproj)
- Business Logic: [BuyOldBike_BLL.csproj](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/BuyOldBike_BLL.csproj)
- Data Access (EF Core): [BuyOldBike_DAL.csproj](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/BuyOldBike_DAL.csproj)
- IPN API (ASP.NET minimal): [BuyOldBike_IpnApi.csproj](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_IpnApi/BuyOldBike_IpnApi.csproj)

Luồng gọi điển hình:
1) UI (Window/ViewModel) gọi BLL service (hoặc đôi khi gọi DAL trực tiếp)
2) BLL gọi DAL repository
3) DAL dùng EF Core DbContext để truy cập SQL Server

Entry points quan trọng:
- WPF startup: [App.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/App.xaml.cs)
  - Seed dữ liệu inspection catalog.
  - Chạy job tự refund deposit mỗi 5 phút.
- IPN endpoint: [Program.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_IpnApi/Program.cs)
  - Map `/vnpay/ipn` → `VnPayIpnService.ProcessIpn(...)`.

---

## 2. Entity Inspection.cs (từng dòng)

File: [Inspection.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Inspection.cs)

> Đây là entity EF Core tương ứng bảng `dbo.inspections`. “Inspection” là bản ghi kiểm định của 1 listing.

### Giải thích
- L1 `using System;`: dùng kiểu `Guid`, `DateTime`.
- L2 `using System.Collections.Generic;`: dùng `ICollection<T>` cho navigation collections.
- L4 `namespace BuyOldBike_DAL.Entities;`: namespace theo project DAL.
- L6 `public partial class Inspection`: class `partial` vì thường EF scaffold sinh ra 1 phần (và có thể có phần khác mở rộng).
- L8 `public Guid InspectionId { get; set; }`: khóa chính của inspection.
- L10 `public Guid ListingId { get; set; }`: khóa ngoại trỏ tới listing được kiểm định.
- L12 `public Guid? InspectorId { get; set; }`: khóa ngoại tới user inspector (có thể null khi chưa assign).
- L14 `public string? InspectionTypeId { get; set; }`: loại kiểm định (đang để string, nullable).
- L16 `public Guid InspectionLocationId { get; set; }`: địa điểm kiểm định (FK bắt buộc).
- L18 `public DateTime? ScheduledAt { get; set; }`: lịch hẹn kiểm định (nullable).
- L20 `public string Status { get; set; } = null!;`: trạng thái kiểm định (Pending/Completed/...) — `null!` để tránh warning nullable khi scaffold.
- L22 `public string? Result { get; set; }`: kết quả Passed/Failed (nullable trước khi có kết quả).
- L24 `public int? OverallScore { get; set; }`: điểm tổng (nullable).
- L26 `public string? RejectReason { get; set; }`: lý do fail/ghi chú.
- L28 `public DateTime CreatedAt { get; set; }`: ngày tạo record.
- L30 `public virtual InspectionLocation InspectionLocation { get; set; } = null!;`: navigation 1-1/1-n tới bảng `inspection_locations` (bắt buộc).
- L32 `public virtual ICollection<InspectionImage> InspectionImages { get; set; } = new List<InspectionImage>();`: ảnh báo cáo kiểm định (0..n).
- L34 `public virtual ICollection<InspectionScore> InspectionScores { get; set; } = new List<InspectionScore>();`: điểm theo component (0..n).
- L36 `public virtual User? Inspector { get; set; }`: navigation tới user inspector (nullable).
- L38 `public virtual Listing Listing { get; set; } = null!;`: navigation tới listing (bắt buộc).

---

## 3. InspectorWindow.LoadDisputeDetails (từng dòng)

Đoạn code UI hiển thị chi tiết khiếu nại và kết quả kiểm định mới nhất.

File: [InspectorWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml.cs#L360-L433)

### Ý tưởng tổng
1) Lấy `ReturnRequest` theo id và `Include(...)` tất cả liên quan: ảnh khiếu nại, order, buyer/seller + address, listing + inspections + inspection_scores + component.
2) Chọn inspection “hợp lệ nhất”: ưu tiên inspection `Completed` gần nhất, nếu không có thì lấy inspection gần nhất.
3) Dựng view-model cho UI: pass/fail theo từng component, kết quả tổng, điểm tổng, ghi chú.

### Từng dòng (theo đoạn)
- L367 `using var db = new BuyOldBikeContext();`: tạo DbContext để truy vấn DB.
- L369–383 `var request = db.ReturnRequests ...`: query ReturnRequest, dùng `Include/ThenInclude` để load object graph.
  - Include `ReturnRequestImages`: ảnh buyer/seller upload.
  - Include `Order` → `Buyer` → `Address`: để hiện tên/địa chỉ buyer.
  - Include `Order` → `Listing` → `Seller` → `Address`: để hiện tên/địa chỉ seller.
  - Include `Order` → `Listing` → `Inspections` → `InspectionScores` → `Component`: để hiện kết quả kiểm định.
  - `AsNoTracking()`: chỉ đọc, không track để nhẹ hơn.
  - `FirstOrDefault(...)`: lấy đúng 1 request theo id.
- L386–390: nếu dữ liệu thiếu (không có order/listing) thì gán `DataContext` rỗng để UI không lỗi.
- L392: `listing = request.Order.Listing`.
- L393–396: chọn inspection:
  - `OrderByDescending(i => i.CreatedAt)`: inspection mới nhất.
  - `FirstOrDefault(i => i.Status == Completed)`: ưu tiên bản completed.
  - nếu không có completed thì lấy inspection mới nhất bất kể trạng thái.
- L398–409: dựng danh sách kết quả theo từng component name (mảng `DefaultComponentNames` của UI):
  - tìm score theo `ComponentName`
  - map `Score == 1` → "Pass", `Score == 0` → "Fail", null → "-"
- L411: lấy ghi chú từ `RejectReason` (trong code repo này thường dùng để note khi fail).
- L413–427: set `pnlDisputeDetails.DataContext = new DisputeDetailVm { ... }`
  - `InspectionResultText`: nếu `inspection.Result == Passed` → "Pass", nếu Failed → "Fail", còn lại "-"
  - `InspectionOverallText`: nếu có điểm tổng → `"x/4"` (vì hệ thống có 4 component mặc định)
  - `InspectionNoteText`: note trim hoặc "-"
- L429–432: nếu lỗi truy vấn/DB thì fallback DataContext rỗng.

---

## 4. DatabaseScripts (từng dòng/từng khối)

Các script nằm ở: [DatabaseScripts](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts)

### 4.1 2026-03-21_add_payment_order_vnpay.sql

File: [2026-03-21_add_payment_order_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

Mục tiêu:
- Chuẩn hóa tên cột Orders/Payments về snake_case.
- Tạo bảng `orders`, `payments` nếu chưa tồn tại.
- Thêm các cột bị thiếu.
- Thêm default constraints, foreign keys, indexes cho luồng thanh toán (VNPay).

Giải thích theo khối:
- Khối A (L1–17): `SET NOCOUNT ON;` + nếu `dbo.orders` tồn tại thì rename các cột PascalCase → snake_case bằng `sp_rename`.
- Khối B (L19–31): nếu chưa có `dbo.orders` thì `CREATE TABLE dbo.orders(...)` với PK `order_id`.
- Khối C (L33–57): nếu thiếu cột nào trong orders thì `ALTER TABLE ... ADD ...`.
- Khối D (L58–69): đảm bảo có default constraint cho `orders.order_id` (tránh thiếu DF khi table đã tồn tại).
- Khối E (L71–86): nếu chưa có `dbo.payments` thì tạo bảng, có các cột phục vụ VNPay như `txn_ref`, `provider_txn_no`.
- Khối F (L88–108): nếu payments tồn tại thì rename cột PascalCase → snake_case.
- Khối G (L110–148): nếu thiếu cột nào trong payments thì `ALTER TABLE ... ADD ...`.
- Khối H (L150–161): đảm bảo default constraint cho `payments.payment_id`.
- Khối I (L163–233): đảm bảo foreign key:
  - `payments.order_id` → `orders.order_id`
  - `orders.buyer_id` → `users.user_id`
  - `orders.listing_id` → `listings.listing_id`
  - `payments.user_id` → `users.user_id`
- Khối J (L235–243): tạo index cho `payments(order_id)` và `payments(txn_ref)` để tra cứu nhanh theo order/txnRef.

### 4.2 2026-03-23_add_wallet.sql

File: [2026-03-23_add_wallet.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-23_add_wallet.sql)

```sql
CREATE TABLE [dbo].[user_wallets] (
    [wallet_id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_user_wallets_wallet_id] DEFAULT (newid()),
    [user_id] UNIQUEIDENTIFIER NOT NULL,
    [balance] DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_user_wallets_balance] DEFAULT (0),
    [updated_at] DATETIME NOT NULL CONSTRAINT [DF_user_wallets_updated_at] DEFAULT (getdate()),
    CONSTRAINT [PK_user_wallets] PRIMARY KEY CLUSTERED ([wallet_id] ASC),
    CONSTRAINT [UQ_user_wallets_user_id] UNIQUE ([user_id]),
    CONSTRAINT [FK_user_wallets_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);

CREATE TABLE [dbo].[wallet_transactions] (
    [wallet_transaction_id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_wallet_transactions_id] DEFAULT (newid()),
    [wallet_id] UNIQUEIDENTIFIER NOT NULL,
    [amount] DECIMAL(18, 2) NOT NULL,
    [direction] VARCHAR(20) NOT NULL,
    [type] VARCHAR(50) NOT NULL,
    [order_id] UNIQUEIDENTIFIER NULL,
    [note] TEXT NULL,
    [created_at] DATETIME NOT NULL CONSTRAINT [DF_wallet_transactions_created_at] DEFAULT (getdate()),
    CONSTRAINT [PK_wallet_transactions] PRIMARY KEY CLUSTERED ([wallet_transaction_id] ASC),
    CONSTRAINT [FK_wallet_transactions_user_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [dbo].[user_wallets] ([wallet_id])
);

CREATE INDEX [IX_wallet_transactions_wallet_id_created_at] ON [dbo].[wallet_transactions] ([wallet_id], [created_at] DESC);
```

Giải thích từng dòng (theo câu lệnh):
- `CREATE TABLE user_wallets`: tạo bảng ví theo user.
  - `wallet_id ... DEFAULT (newid())`: PK là GUID tự sinh.
  - `user_id ... NOT NULL`: mỗi ví gắn với 1 user.
  - `balance ... DEFAULT (0)`: số dư mặc định 0.
  - `updated_at ... DEFAULT (getdate())`: timestamp cập nhật.
  - `PRIMARY KEY (wallet_id)`: khóa chính.
  - `UNIQUE (user_id)`: đảm bảo 1 user chỉ có 1 ví.
  - `FOREIGN KEY (user_id) REFERENCES users(user_id)`: ràng buộc user tồn tại.
- `CREATE TABLE wallet_transactions`: bảng lịch sử giao dịch ví.
  - `wallet_transaction_id ... DEFAULT(newid())`: PK giao dịch.
  - `wallet_id`: FK tới user_wallets.
  - `amount`: số tiền giao dịch.
  - `direction`: hướng tiền ("Credit"/"Debit").
  - `type`: loại giao dịch (TopUp/ListingPostFee/Deposit/...).
  - `order_id`: optional nếu giao dịch gắn order.
  - `note TEXT`: ghi chú (sau này có script convert sang NVARCHAR(MAX)).
  - `created_at DEFAULT(getdate())`: thời điểm.
- `CREATE INDEX ... (wallet_id, created_at DESC)`: tối ưu query “lấy lịch sử theo ví, mới nhất trước”.

### 4.3 2026-03-25_add_order_delivery_address.sql

File: [2026-03-25_add_order_delivery_address.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-25_add_order_delivery_address.sql)

```sql
ALTER TABLE dbo.orders
ADD
    delivery_full_name VARCHAR(255) NULL,
    delivery_phone_number VARCHAR(20) NULL,
    delivery_province VARCHAR(100) NULL,
    delivery_district VARCHAR(100) NULL,
    delivery_ward VARCHAR(100) NULL,
    delivery_detail TEXT NULL;
```

Giải thích:
- `ALTER TABLE dbo.orders ADD ...`: thêm nhóm cột địa chỉ nghiệm thu/giao hàng vào bảng orders.
- Các cột dạng `VARCHAR/TEXT` ở thời điểm script; sau đó script unicode sẽ chuyển sang `NVARCHAR` để lưu tiếng Việt tốt hơn.

### 4.4 2026-03-27_add_return_request_image_uploader_role.sql

File: [2026-03-27_add_return_request_image_uploader_role.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-27_add_return_request_image_uploader_role.sql)

```sql
ALTER TABLE dbo.return_request_images
ADD uploader_role VARCHAR(20) NOT NULL
    CONSTRAINT DF_return_request_images_uploader_role DEFAULT ('Buyer');
```

Giải thích:
- Thêm cột `uploader_role` để biết ảnh dispute do ai upload (Buyer/Seller/Inspector...).
- `NOT NULL` + default `'Buyer'`: dữ liệu cũ không bị lỗi khi add cột (mọi row hiện có sẽ nhận default).

### 4.5 2026-03-27_convert_unicode_columns.sql

File: [2026-03-27_convert_unicode_columns.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-27_convert_unicode_columns.sql)

Mục tiêu:
- Convert các cột text/varchar “cần tiếng Việt” sang `NVARCHAR/NVARCHAR(MAX)`.
- Với các cột “đang unique” (brands.brand_name, types.name): drop constraint/index trước khi alter, rồi add lại unique sau khi kiểm tra null/duplicate.
- Dùng transaction + TRY/CATCH để an toàn.

Giải thích theo khối chính:
- L1: `SET NOCOUNT ON;` giảm message “(x rows affected)”.
- L3–5: mở TRY + `BEGIN TRAN;` để convert atomic.

- Khối addresses (L6–20):
  - Nếu có table `dbo.addresses` thì `ALTER COLUMN` các field địa chỉ sang NVARCHAR.

- Khối brands (L22–79):
  - Check table/cột tồn tại.
  - L24–26: nếu có `brand_name IS NULL` thì `THROW` để chặn vì cột sẽ chuyển sang NOT NULL.
  - L27–59: dùng cursor để drop các unique constraint/index liên quan `brand_name` (vì SQL Server không cho alter type nếu đang bị constraint/index kiểu nhất định).
  - L61: alter `brand_name` → `NVARCHAR(100) NOT NULL`.
  - L63–69: check duplicate để đảm bảo unique.
  - L71–78: add lại unique constraint `UQ_brands_brand_name` nếu thiếu.

- Khối inspections/inspection_components/inspection_locations/inspection_scores (L81–112):
  - `reject_reason`, `component_name`, `address_line`, `note`... chuyển sang NVARCHAR/NVARCHAR(MAX).
  - Riêng `inspection_locations.address_line` (L89–96): trước khi chuyển sang NOT NULL thì update null → N''.

- Khối kyc/listings/orders/return_requests/reviews (L113–148):
  - Đổi text mô tả/địa chỉ/lý do sang NVARCHAR.

- Khối types (L149–206): tương tự brands nhưng áp dụng cho `types.name`.

- Khối wallet_transactions.note (L208–209):
  - đổi note sang NVARCHAR(MAX) để lưu tiếng Việt.

- L211–217:
  - `COMMIT TRAN;` nếu OK.
  - Nếu lỗi thì rollback + throw lại để caller biết lỗi.

### 4.6 2026-03-27_recreate_tables.sql

File: [2026-03-27_recreate_tables.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-27_recreate_tables.sql)

Mục tiêu:
- Xóa toàn bộ bảng và tạo lại schema “fresh”.
- Seed sẵn 4 user mẫu: buyer/seller/inspector/admin.

Giải thích theo khối:
- L1: `SET NOCOUNT ON;`
- L3–6: khai báo GUID cố định cho 4 tài khoản seed (để dễ login test và FK ổn định).
- L8–29: `DROP TABLE IF EXISTS ...` theo thứ tự “con trước cha” để không vướng FK.
- L31–49: tạo bảng `users` + unique index email + insert 4 users mẫu.
- L50–61: tạo `seller_profiles` và seed profile cho seller.
- L62–77: tạo bảng danh mục `brands`, `types` và unique index.
- L78–97: tạo bảng `inspection_components`, `inspection_locations`.
- L99–103: tạo `frame_sizes` (khung xe).
- L105–123: tạo `listings` (có `views` default 0) + FK tới users/brands/types.
- L125–140: tạo `addresses` + unique user_id (mỗi user 1 address).
- L141–148: tạo `listing_images`.
- L150–166: tạo `orders` (có delivery address dạng NVARCHAR).
- L168–181: tạo `payments` + FK tới orders/users.
- L183–201: tạo `return_requests` và `return_request_images`.
- L203–215: tạo `reviews` (buyer đánh giá seller theo order).
- L217–242: tạo `user_wallets` + `wallet_transactions` + index.
- L243–270: tạo `kyc_profiles`, `kyc_images`.
- L272–288: tạo `inspections` (FK tới inspection_locations/users/listings).
- L290–298: tạo `inspection_scores` (PK composite: inspection_id + component_id).
- L300–316: tạo `messages` (chat) + indexes cho query nhanh.

### 4.7 2026-03-28_add_inspection_images.sql

File: [2026-03-28_add_inspection_images.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-28_add_inspection_images.sql)

```sql
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
```

Giải thích:
- `IF OBJECT_ID(...) IS NULL`: chỉ tạo table nếu chưa có (idempotent).
- `inspection_images`: lưu ảnh report kiểm định (inspector upload).
- `inspection_id` FK tới inspections.
- Index theo `inspection_id` để load ảnh theo inspection nhanh.

### 4.8 2026-03-28_add_listing_views.sql

File: [2026-03-28_add_listing_views.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-28_add_listing_views.sql)

```sql
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.listings', N'U') IS NOT NULL AND COL_LENGTH('dbo.listings', 'views') IS NULL
BEGIN
    ALTER TABLE dbo.listings
    ADD views INT NOT NULL CONSTRAINT DF_listings_views DEFAULT (0) WITH VALUES;
END
```

Giải thích từng dòng:
- `SET NOCOUNT ON;`: giảm output phụ.
- `IF ... listings tồn tại` và `COL_LENGTH(..., 'views') IS NULL`: chỉ add cột nếu chưa có.
- `ALTER TABLE ... ADD views INT NOT NULL ... DEFAULT(0) WITH VALUES`:
  - thêm cột views bắt buộc.
  - default = 0.
  - `WITH VALUES` backfill cho các row hiện có.

---

## 5. Cách giải thích “từng dòng” cho toàn bộ code (quy trình chuẩn)

Vì số lượng `.cs/.xaml` của repo khá lớn, cách làm bền vững là đi theo “entry point → flow theo feature → entity/repo liên quan”, và mỗi file giải thích theo cùng 1 template.

### 5.1 Template giải thích cho 1 file `.cs`

1) **Mục đích file**: file này đại diện cho UI/BLL/DAL gì?
2) **Các dependency**: `using`, service/repo nào được tạo và vì sao.
3) **Các hàm public**: input/output + side effects (ghi DB, mở window, gọi VNPay…).
4) **Luồng dữ liệu**: object nào đi qua các layer (User/Listing/Inspection/Order/Payment/Wallet…).
5) **Điểm ràng buộc nghiệp vụ**: các if/throw/status check quan trọng.

### 5.2 Danh sách file nên đọc theo thứ tự (để bao phủ “toàn bộ”)

**Core UI/session**
- [App.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/App.xaml.cs)
- [AppSession.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/AppSession.cs)
- [RoleNavigator.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/RoleNavigator.cs)
- [LogoutManager.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/LogoutManager.cs)

**Buyer flow**
- [BicycleListWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/BicycleListWindow.xaml.cs)
- [BicycleListWindowViewModel.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/ViewModels/BicycleListWindowViewModel.cs)
- (detail) [ListingBrowseService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/BicycleListWindow/ListingBrowseService.cs)

**Seller flow**
- [SellerWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/SellerWindow.xaml.cs)
- [ListingService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Seller/ListingService.cs)
- [BikePostRepository.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories/Seller/BikePostRepository.cs)

**Inspection/Dispute**
- [InspectorWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/InspectorWindow.xaml.cs)
- [InspectionService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Seller/InspectionService.cs)
- (bản service khác theo namespace Inspector) [InspectionService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Services/Inspector/InspectionService.cs)
- Tài liệu feature: [inspector-dispute.md](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/docs/features/inspector-dispute.md)

**Payments/VNPay**
- [WalletWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/WalletWindow.xaml.cs)
- [VnPayReturnListener.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Payments/VnPayReturnListener.cs)
- [DepositService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Features/Payments/VnPay/DepositService.cs)
- [VnPayIpnService.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_BLL/Features/Payments/VnPay/VnPayIpnService.cs)
- IPN endpoint: [Program.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_IpnApi/Program.cs)

**DAL/EF**
- [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs)
- Entities: [BuyOldBike_DAL/Entities](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities)
- Repositories: [BuyOldBike_DAL/Repositories](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Repositories)

### 5.3 Tài liệu theo feature (đã viết sẵn)

- [docs/README.md](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/docs/README.md)
