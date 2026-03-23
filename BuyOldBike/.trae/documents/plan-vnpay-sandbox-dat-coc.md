# Hướng dẫn: SQL + Entity/Mapping cho Orders/Payments (giữ lại entity, xoá phần VNPay)

## 1) Mục tiêu

Chỉ giữ các thay đổi ở **DB + Entity + EF Mapping** để:

- Bảng `payments` có thể liên kết tới `orders` qua `order_id`
- Lưu thông tin đối soát: `txn_ref`, `provider_txn_no`

Không triển khai business/UI VNPay trong lần này.

## 2) Phân tích hiện trạng (grounded)

### Code hiện có

* DB đã có entity `Order` và `Payment`:

  * [Order.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Order.cs)

  * [Payment.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Payment.cs)

  * Mapping trong [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs)

* `appsettings.json` hiện chỉ có connection string: [appsettings.json](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/appsettings.json).

### Hạn chế/điểm cần bổ sung

* DB schema cần bổ sung liên kết `payments.order_id -> orders.order_id` và 2 cột phục vụ đối soát (`txn_ref`, `provider_txn_no`).

## 3) SQL: tạo/đồng bộ bảng và ràng buộc

**File SQL đã chuẩn hoá:** [2026-03-21_add_payment_order_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

- Script chạy theo kiểu “có thì ALTER, chưa có thì CREATE” (idempotent).
- Nếu DB của bạn đã có bảng `orders/payments` thì script chỉ thêm cột/constraint/index còn thiếu.

## 4) Entity & EF Mapping: giữ lại những gì?

### 4.1 Payment entity

**File:** [Payment.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Payment.cs)

- Thêm thuộc tính:
  - `OrderId` (map cột `payments.order_id`)
  - `TxnRef` (map cột `payments.txn_ref`)
  - `ProviderTxnNo` (map cột `payments.provider_txn_no`)
- Thêm navigation:
  - `Order` (đi từ Payment sang Order)

### 4.2 Order entity

**File:** [Order.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Order.cs)

- Thêm collection navigation:
  - `Payments` (đi từ Order sang nhiều Payment)

### 4.3 DbContext mapping

**File:** [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs)

- Map thêm các cột mới cho entity Payment:
  - `order_id`, `txn_ref`, `provider_txn_no`
- Map FK:
  - `payments.order_id` -> `orders.order_id` (HasOne(Order).WithMany(Payments))

## 5) Giải thích nhanh (vì sao làm vậy)

- `payments.order_id`: giúp truy vết chính xác payment thuộc order nào (không còn phải suy luận).
- `txn_ref`: lưu mã tham chiếu phía cổng thanh toán (thường là key để lookup giao dịch).
- `provider_txn_no`: lưu mã giao dịch do cổng thanh toán cấp để đối soát/tra cứu.

## 6) Kiểm tra sau khi áp dụng

- Chạy SQL script, sau đó verify:
  - Bảng `payments` có các cột `order_id`, `txn_ref`, `provider_txn_no`
  - Có FK `FK_payments_orders_order_id`
  - Có index `IX_payments_order_id` và `IX_payments_txn_ref`
- Build solution để chắc chắn EF entities/mapping compile.

  * Gọi repo lock listing + create Order/Payment pending.

  * Sinh VNPay URL (TxnRef = OrderId).

  * Sau khi nhận callback & verify, gọi repo mark succeeded/failed.

### 3.4. Presentation: cấu hình VNPay + luồng “Đặt cọc” tại ListingDetailWindow

**File cập nhật:**

* [BuyOldBike\_Presentation/appsettings.json](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/appsettings.json)

  * Thêm section `VnPay` (sandbox URL, tmnCode, hashSecret, returnUrl localhost).

**File cập nhật (UI):**

* [ListingDetailWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml)

  * Đổi nút “Đặt mua” thành “Đặt cọc (20%)” hoặc thêm nút mới.

  * Disable nút nếu listing không còn Available.

* [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs)

  * Bắt sự kiện click “Đặt cọc”.

  * Kiểm tra đăng nhập `AppSession.IsAuthenticated` (nếu chưa: mở Login hoặc báo).

  * Hiển thị confirm số tiền cọc.

  * Tạo local callback listener bằng `HttpListener` (ví dụ `http://localhost:51123/vnpay-return/`).

  * Mở trình duyệt đến VNPay URL (`Process.Start(new ProcessStartInfo(url){ UseShellExecute = true })`).

  * Chờ callback trả về (Task/async), verify và cập nhật DB, show MessageBox kết quả.

**Thêm helper (Presentation hoặc BLL):**

* Local callback listener có timeout (ví dụ 5 phút) để tránh chờ vô hạn.

* Khi nhận callback, trả lại response HTML đơn giản “Bạn có thể quay lại ứng dụng”.

## 4) Giả định & quyết định

* Deposit = 20% Listing.Price; nếu Price null/<=0 thì chặn đặt cọc.

* Listing được “khoá tạm” bằng status mới `Deposit_Pending`; khi thanh toán thành công có thể chuyển sang `Reserved`.

* TxnRef của VNPay dùng `OrderId` (GUID string, bỏ dấu “-” nếu cần) để lookup đơn.

- Có thay đổi DB để liên kết `payments` ↔ `orders` và lưu `txn_ref/provider_txn_no`.

- Timeout callback sẽ tự rollback lock listing và đánh dấu order/payment `Expired`.

## 5) Xác minh (verification)

### Build/Run

* Build solution để đảm bảo không lỗi compile sau khi thêm lớp/config.

* Run app, mở listing Available, click “Đặt cọc”.

### Manual test (sandbox)

* Thanh toán thành công:

  * VNPay redirect về localhost → app hiện “thành công”.

  * DB: Order.Status = Deposit\_Paid, Payment.Status = Succeeded, Listing.Status = Reserved.

* Huỷ/Fail:

  * App hiện “thất bại/đã huỷ”.

  * DB: Order.Status = Deposit\_Failed, Payment.Status = Failed, Listing.Status quay về Available.

### Edge cases

* Listing đã bị khoá (không Available) → chặn đặt cọc.

- Timeout không nhận callback → rollback listing về Available và set Order/Payment = Expired.
