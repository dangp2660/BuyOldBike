# Kế hoạch hướng dẫn: SQL + Entity/EF Mapping cho Orders/Payments

## 1) Tóm tắt

Mục tiêu là **chuẩn hoá DB schema** cho `orders` và `payments`, đồng thời **đồng bộ Entity + EF Mapping** để:

* `payments.order_id` liên kết tới `orders.order_id`

* Lưu thông tin đối soát: `payments.txn_ref`, `payments.provider_txn_no`

Phần VNPay/đặt cọc (BLL/Presentation) **không nằm trong phạm vi**.

## 2) Hiện trạng (đã kiểm tra trong repo)

### 2.1 Script SQL hiện có

* Script đã tồn tại và theo hướng **idempotent + rename cột PascalCase → snake\_case**:

  * [2026-03-21\_add\_payment\_order\_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

* Script thực hiện:

  * `sp_rename` các cột `OrderId/BuyerId/...` → `order_id/buyer_id/...` nếu phát hiện bảng/cột kiểu PascalCase

  * `CREATE TABLE` nếu thiếu `dbo.orders` hoặc `dbo.payments`

  * `ALTER TABLE` thêm cột còn thiếu

  * Thêm default constraint cho `order_id/payment_id` nếu thiếu

  * Thêm các FK:

    * `FK_payments_orders_order_id` (payments → orders)

    * `FK_orders_users_buyer_id` (orders → users)

    * `FK_orders_listings_listing_id` (orders → listings)

    * `FK_payments_users_user_id` (payments → users)

  * Thêm index:

    * `IX_payments_order_id`, `IX_payments_txn_ref`

### 2.2 Entity & EF mapping hiện có

* Entity:

  * [Payment.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Payment.cs) đã có `OrderId`, `TxnRef`, `ProviderTxnNo`, navigation `Order`

  * [Order.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Order.cs) đã có collection `Payments`

* Mapping:

  * [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs) đã map `order_id/txn_ref/provider_txn_no` và FK `FK_payments_orders_order_id`

### 2.3 Tài liệu hướng dẫn hiện tại

* Tài liệu cũ có phần “đặt cọc/VNPay” còn sót ở cuối: [plan-vnpay-sandbox-dat-coc.md](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/.trae/documents/plan-vnpay-sandbox-dat-coc.md)

* Khi dùng làm hướng dẫn DB/entity, chỉ nên tham khảo phần DB/entity; phần “Edge cases đặt cọc/timeout” không còn phù hợp scope hiện tại.

## 3) Kế hoạch thực hiện (cho người triển khai)

### Bước 1: Backup và xác định môi trường DB

* Xác định DB đang chạy: connection string hiện đặt tại [appsettings.json](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/appsettings.json)

* Tạo backup DB (khuyến nghị) trước khi chạy script vì script có `sp_rename` cột.

### Bước 2: Chạy script SQL

* Mở SSMS/Azure Data Studio, chọn đúng database (ví dụ `BuyOldBike`)

* Chạy script: [2026-03-21\_add\_payment\_order\_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

Lưu ý quan trọng:

* `sp_rename` sẽ đổi tên cột hiện hữu (nếu DB trước đó dùng PascalCase). Điều này là “breaking change” với các app/queries cũ.

* Script chỉ tạo FK nếu bảng `dbo.users` và `dbo.listings` tồn tại.

### Bước 3: Verify schema sau khi chạy

Chạy các câu lệnh kiểm tra nhanh:

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'payments'
ORDER BY ORDINAL_POSITION;
```

Kỳ vọng tối thiểu có:

* `payment_id`, `user_id`, `amount`, `payment_type`, `status`, `created_at`

* `order_id`, `txn_ref`, `provider_txn_no`

Kiểm tra FK:

```sql
SELECT fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('dbo.payments');
```

Kỳ vọng có `FK_payments_orders_order_id` và (nếu users tồn tại) `FK_payments_users_user_id`.

Kiểm tra index:

```sql
SELECT name
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.payments') AND name IN ('IX_payments_order_id','IX_payments_txn_ref');
```

### Bước 4: Verify code mapping (Entity Framework)

* Đảm bảo các file sau không bị sửa lệch naming:

  * [Payment.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Payment.cs)

  * [Order.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Order.cs)

  * [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs)

* Nếu DB thực tế dùng cột PascalCase (không chạy script rename), thì mapping snake\_case sẽ lỗi. Khi đó bắt buộc:

  * Hoặc chạy script để rename về snake\_case

  * Hoặc sửa mapping để trỏ đúng tên cột thực tế (không khuyến nghị vì làm repo không thống nhất)

### Bước 5: Build solution

* Build solution để xác nhận compile OK sau khi schema/entity/mapping đồng bộ.

* Acceptance: build thành công; không còn code phụ thuộc VNPay/Deposit.

## 4) Giả định & quyết định

* Chuẩn tên cột dùng snake\_case theo EF mapping hiện tại: `order_id`, `payment_id`, `user_id`, `listing_id`.

* Payment–Order là quan hệ 1-n (một order có thể có nhiều payment) để dễ mở rộng (retry/refund).

* Không đưa logic VNPay/deposit vào plan này; chỉ tập trung DB/entity/mapping.

## 5) Tiêu chí hoàn thành

* DB có đủ cột + FK + index như mục 3.3

* EF mapping map đúng cột mới và chạy được các query include navigation (Order.Payments / Payment.Order)

* Build solution thành công

