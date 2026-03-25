# Kế hoạch: Tích hợp VNPay Sandbox cho luồng đặt cọc (WPF)

## 1) Tóm tắt

Tích hợp thanh toán **VNPay Sandbox** cho luồng **đặt cọc 20%** tại màn [ListingDetailWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml).

Flow mục tiêu:

1. Buyer bấm “Đặt cọc (20%)”
2. Hệ thống tạo `Order` + `Payment` (Pending) và **khoá tạm listing**
3. Mở browser đến VNPay sandbox
4. VNPay redirect về **ReturnUrl localhost** → app nhận callback, verify chữ ký
5. Thành công: cập nhật `Order/Payment` sang Success + listing sang Reserved; thất bại/timeout: rollback listing về Available

## 2) Hiện trạng (grounded)

### 2.1 UI hiện tại

* Nút ở footer đang là “Đặt mua”, chưa có hành vi đặt cọc: [ListingDetailWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml#L124-L133)

* Code-behind chỉ có đóng window: [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs)

* ViewModel chỉ load listing + ảnh: [ListingDetailViewModel.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/ViewModels/ListingDetailViewModel.cs)

### 2.2 DB/EF đã sẵn nền tảng

* Entity đã có liên kết `Payment → Order` + trường đối soát:

  * [Payment.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Payment.cs)

  * [Order.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/Order.cs)

  * Mapping trong [BuyOldBikeContext.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Entities/BuyOldBikeContext.cs)

* Script DB idempotent (CREATE/ALTER + rename PascalCase→snake\_case) đã có:

  * [2026-03-21\_add\_payment\_order\_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

### 2.3 Constants trạng thái chưa có cho deposit

* Listing status mới chỉ có Pending\_Inspection/Available/Rejected: [StatusConstants.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Constants/StatusConstants.cs)

## 3) Thay đổi đề xuất (decision-complete)

### 3.1 DB: đảm bảo schema đúng trước khi chạy app

**File:** [2026-03-21\_add\_payment\_order\_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

* Chạy script trên DB `BuyOldBike` trước khi test flow.

* Sau khi chạy, verify:

  * `payments` có `order_id`, `txn_ref`, `provider_txn_no`

  * Có FK `FK_payments_orders_order_id`

  * Có index `IX_payments_order_id`, `IX_payments_txn_ref`

### 3.2 DAL: bổ sung constants cho trạng thái order/payment/listing (deposit)

**File:** [StatusConstants.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Constants/StatusConstants.cs)

* Thêm:

  * `ListingStatus.Deposit_Pending`, `ListingStatus.Reserved`

  * `OrderStatus.Deposit_Pending`, `OrderStatus.Deposit_Paid`, `OrderStatus.Deposit_Failed`, `OrderStatus.Deposit_Expired`

  * `PaymentStatus.Pending`, `PaymentStatus.Succeeded`, `PaymentStatus.Failed`, `PaymentStatus.Expired`

  * `PaymentType.Deposit_VNPay`

### 3.3 DAL: tạo repository cho luồng đặt cọc

**Thêm file mới:**

* `BuyOldBike_DAL/Repositories/Payment/DepositRepository.cs`

API tối thiểu:

* `CreateDeposit(buyerId, listingId, depositAmount) -> (order, payment)`:

  * Check listing tồn tại và `Status == Available`

  * Set listing `Deposit_Pending`

  * Insert `Order` + `Payment` (Pending), gán `payment.TxnRef` (dùng OrderId dạng `N`)

  * Transaction

* `GetDepositByTxnRef(txnRef) -> (order, payment)`:

  * Lookup `Payment` theo `txn_ref`

* `MarkDepositSucceeded(orderId, providerTxnNo)`:

  * Idempotent: nếu đã success thì return

  * Set `OrderStatus.Deposit_Paid`, `PaymentStatus.Succeeded`, set `provider_txn_no`

  * Set listing `Reserved`

* `MarkDepositFailed(orderId)` / `MarkDepositExpired(orderId)`:

  * Nếu chưa success: set fail/expired và rollback listing về `Available` (chỉ khi listing đang `Deposit_Pending`)

### 3.4 BLL: VNPay builder + verifier + deposit orchestration

**Thêm folder mới:**

* `BuyOldBike_BLL/Features/Payments/VnPay/`

* `BuyOldBike_BLL/Features/Payments/`

Các class:

* `VnPayOptions`: BaseUrl, TmnCode, HashSecret, ReturnUrl, Version, Command, CurrCode, Locale

* `VnPayRequestBuilder`: build URL với HMACSHA512 (vnp\_SecureHash) và amount theo quy ước VNPay (VND \* 100)

* `VnPayReturnVerifier`: verify chữ ký return bằng cách sort params và tính lại hash

* `DepositService`:

  * Tính depositAmount = round(Listing.Price \* 0.2)

  * Gọi DAL repo `CreateDeposit`

  * Build VNPay URL

  * Xử lý return: verify signature, validate amount khớp DB, update trạng thái success/fail

  * Timeout: mark expired

### 3.5 Presentation: cấu hình + UI đặt cọc + callback localhost

**File cập nhật:**

* [BuyOldBike\_Presentation/appsettings.json](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/appsettings.json)

  * Thêm section `VnPay`:

    * `BaseUrl` sandbox

    * `TmnCode`, `HashSecret` (để trống trong repo; tự điền khi chạy local)

    * `ReturnUrl`: `http://localhost:51123/vnpay-return/` (có trailing slash)

* [ListingDetailWindow.xaml](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml)

  * Đổi/Thêm button “Đặt cọc (20%)”

* [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs)

  * Click handler:

    * Check login qua [AppSession.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/State/AppSession.cs)

    * Confirm số tiền cọc

    * Start `HttpListener` lắng nghe `ReturnUrl`

    * Open browser tới VNPay URL

    * Chờ callback (timeout 5 phút)

    * On timeout: gọi `DepositService.MarkDepositExpired(orderId)`

    * On receive: parse query, gọi `DepositService.ProcessVnPayReturn(...)`, thông báo kết quả, reload VM

Lưu ý môi trường:

* `HttpListener` có thể cần URLACL (Windows) nếu không có quyền lắng nghe prefix (lỗi Access is denied).

### 3.6 Tutorial chi tiết (làm theo từng bước)

#### Bước 0: Chuẩn bị VNPay sandbox

* Có sẵn `TMN_CODE` và `HASH_SECRET` của sandbox.

* Chọn 1 return URL nội bộ cho app, ví dụ:

  * `http://localhost:51123/vnpay-return/`

* Nếu VNPay sandbox yêu cầu khai báo ReturnUrl ở portal sandbox, điền đúng URL trên (có trailing slash).

#### Bước 1: Chạy script DB

1. Backup DB (khuyến nghị).
2. Chạy file: [2026-03-21\_add\_payment\_order\_vnpay.sql](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/DatabaseScripts/2026-03-21_add_payment_order_vnpay.sql)

Kiểm tra nhanh sau khi chạy:

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('orders','payments')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
```

```sql
SELECT fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id IN (OBJECT_ID('dbo.orders'), OBJECT_ID('dbo.payments'))
ORDER BY fk.name;
```

#### Bước 2: Thêm constants trạng thái (DAL)

File: [StatusConstants.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_DAL/Constants/StatusConstants.cs)

Thêm chính xác các hằng sau (string đồng nhất để lưu DB):

* Listing:

  * `Deposit_Pending`

  * `Reserved`

* Order:

  * `Deposit_Pending`, `Deposit_Paid`, `Deposit_Failed`, `Deposit_Expired`

* Payment:

  * `Pending`, `Succeeded`, `Failed`, `Expired`

* PaymentType:

  * `Deposit_VNPay`

Mẫu code (bổ sung vào `StatusConstants`):

```csharp
public static class OrderStatus
{
    public const string Deposit_Pending = "Deposit_Pending";
    public const string Deposit_Paid = "Deposit_Paid";
    public const string Deposit_Failed = "Deposit_Failed";
    public const string Deposit_Expired = "Deposit_Expired";
}

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
}

public static class PaymentType
{
    public const string Deposit_VNPay = "Deposit_VNPay";
}
```

Mẫu code (bổ sung vào `ListingStatus`):

```csharp
public const string Deposit_Pending = "Deposit_Pending";
public const string Reserved = "Reserved";
```

#### Bước 3: Tạo DAL repository đặt cọc

Tạo file `BuyOldBike_DAL/Repositories/Payment/DepositRepository.cs` với các điểm kỹ thuật bắt buộc:

* Transaction khi:

  * lock listing (`Available` → `Deposit_Pending`)

  * insert `Order` + `Payment`

* Idempotent khi update:

  * nếu payment đã `Succeeded` thì không overwrite sang failed/expired

* TxnRef:

  * gán `payment.TxnRef = order.OrderId.ToString("N")` để làm khoá tra theo return

Mẫu code `DepositRepository.cs`:

```csharp
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Payment
{
    public class DepositRepository
    {
        private readonly BuyOldBikeContext _db = new BuyOldBikeContext();

        public (Order order, Entities.Payment payment) CreateDeposit(Guid buyerId, Guid listingId, decimal depositAmount)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var listing = _db.Listings.FirstOrDefault(l => l.ListingId == listingId);
                if (listing == null) throw new InvalidOperationException("Không tìm thấy listing.");
                if (!string.Equals(listing.Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal))
                    throw new InvalidOperationException("Listing không còn khả dụng để đặt cọc.");

                listing.Status = StatusConstants.ListingStatus.Deposit_Pending;

                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ListingId = listingId,
                    Status = StatusConstants.OrderStatus.Deposit_Pending,
                    TotalAmount = listing.Price,
                    CreatedAt = DateTime.Now
                };

                var payment = new Entities.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    UserId = buyerId,
                    OrderId = order.OrderId,
                    Amount = depositAmount,
                    PaymentType = StatusConstants.PaymentType.Deposit_VNPay,
                    Status = StatusConstants.PaymentStatus.Pending,
                    TxnRef = order.OrderId.ToString("N"),
                    CreatedAt = DateTime.Now
                };

                _db.Orders.Add(order);
                _db.Payments.Add(payment);
                _db.SaveChanges();

                transaction.Commit();
                return (order, payment);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public (Order order, Entities.Payment payment) GetDepositByTxnRef(string txnRef)
        {
            var payment = _db.Payments
                .Include(p => p.Order)
                .FirstOrDefault(p =>
                    p.TxnRef == txnRef &&
                    p.PaymentType == StatusConstants.PaymentType.Deposit_VNPay
                );

            if (payment?.Order == null) throw new InvalidOperationException("Không tìm thấy giao dịch theo TxnRef.");
            return (payment.Order, payment);
        }

        public void MarkDepositSucceeded(Guid orderId, string? providerTxnNo)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");

                var payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId && p.PaymentType == StatusConstants.PaymentType.Deposit_VNPay);
                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán.");

                if (string.Equals(payment.Status, StatusConstants.PaymentStatus.Succeeded, StringComparison.Ordinal))
                {
                    transaction.Commit();
                    return;
                }

                order.Status = StatusConstants.OrderStatus.Deposit_Paid;
                payment.Status = StatusConstants.PaymentStatus.Succeeded;
                payment.ProviderTxnNo = providerTxnNo;

                var listing = _db.Listings.FirstOrDefault(l => l.ListingId == order.ListingId);
                if (listing != null) listing.Status = StatusConstants.ListingStatus.Reserved;

                _db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void MarkDepositFailed(Guid orderId)
        {
            MarkDepositFailedInternal(orderId, StatusConstants.OrderStatus.Deposit_Failed, StatusConstants.PaymentStatus.Failed);
        }

        public void MarkDepositExpired(Guid orderId)
        {
            MarkDepositFailedInternal(orderId, StatusConstants.OrderStatus.Deposit_Expired, StatusConstants.PaymentStatus.Expired);
        }

        private void MarkDepositFailedInternal(Guid orderId, string orderStatus, string paymentStatus)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");

                var payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId && p.PaymentType == StatusConstants.PaymentType.Deposit_VNPay);
                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán.");

                if (string.Equals(payment.Status, StatusConstants.PaymentStatus.Succeeded, StringComparison.Ordinal))
                {
                    transaction.Commit();
                    return;
                }

                order.Status = orderStatus;
                payment.Status = paymentStatus;

                var listing = _db.Listings.FirstOrDefault(l => l.ListingId == order.ListingId);
                if (listing != null && string.Equals(listing.Status, StatusConstants.ListingStatus.Deposit_Pending, StringComparison.Ordinal))
                {
                    listing.Status = StatusConstants.ListingStatus.Available;
                }

                _db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
```

#### Bước 4: Tạo BLL VNPay

Tạo folder `BuyOldBike_BLL/Features/Payments/VnPay/` và implement:

1. `VnPayRequestBuilder`:

   * Input: amount (VND), txnRef, orderInfo, ipAddr, createDate

   * Output: URL dạng `BaseUrl + ?vnp_* + vnp_SecureHash`

   * Amount: `vnp_Amount = (long)Math.Round(amountVnd * 100)`

   * Hash data: sort key asc, join `key=value` bằng `&`, HMACSHA512 với `HashSecret`

2. `VnPayReturnVerifier`:

   * Nhận dictionary query params

   * Loại `vnp_SecureHash` và `vnp_SecureHashType`

   * Sort key asc, build hash data như trên và so sánh với `vnp_SecureHash`

   * Success khi:

     * `vnp_ResponseCode == "00"` và `vnp_TransactionStatus == "00"`

3. `DepositService`:

   * `CreateDepositPaymentUrl(buyerId, listingId, options, ipAddr)`:

     * tính cọc `20%`

     * gọi repo tạo order/payment + lock listing

     * build url

   * `ProcessVnPayReturn(options, queryParams)`:

     * verify chữ ký

     * lookup theo `vnp_TxnRef`

     * validate amount khớp với `Payment.Amount`

     * cập nhật success/fail

   * `MarkDepositExpired(orderId)` dùng khi timeout

Mẫu code (các file mới trong BLL):

`VnPayOptions.cs`

```csharp
namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayOptions
    {
        public string BaseUrl { get; set; } = "";
        public string TmnCode { get; set; } = "";
        public string HashSecret { get; set; } = "";
        public string ReturnUrl { get; set; } = "";
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string OrderType { get; set; } = "other";
    }
}
```

`VnPayCreatePaymentRequest.cs`

```csharp
public static class OrderStatus
{
    public const string Deposit_Pending = "Deposit_Pending";
    public const string Deposit_Paid = "Deposit_Paid";
    public const string Deposit_Failed = "Deposit_Failed";
    public const string Deposit_Expired = "Deposit_Expired";
}

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
}

public static class PaymentType
{
    public const string Deposit_VNPay = "Deposit_VNPay";
}
```

`VnPayRequestBuilder.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayRequestBuilder
    {
        public string BuildPaymentUrl(VnPayOptions options, VnPayCreatePaymentRequest request)
        {
            var amount = ToVnPayAmount(request.AmountVnd);
            var createDate = request.CreateDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = options.Version,
                ["vnp_Command"] = options.Command,
                ["vnp_TmnCode"] = options.TmnCode,
                ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
                ["vnp_CurrCode"] = options.CurrCode,
                ["vnp_TxnRef"] = request.TxnRef,
                ["vnp_OrderInfo"] = request.OrderInfo,
                ["vnp_OrderType"] = options.OrderType,
                ["vnp_Locale"] = options.Locale,
                ["vnp_ReturnUrl"] = options.ReturnUrl,
                ["vnp_IpAddr"] = request.IpAddr,
                ["vnp_CreateDate"] = createDate
            };

            var query = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
            var secureHash = ComputeHmacSha512(options.HashSecret, query);
            return $"{options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public static long ToVnPayAmount(decimal amountVnd)
        {
            var value = Math.Round(amountVnd * 100m, 0, MidpointRounding.AwayFromZero);
            return (long)value;
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
```

`VnPayReturnResult.cs`

```csharp
namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayReturnResult
    {
        public bool IsValidSignature { get; set; }
        public bool IsSuccess { get; set; }
        public string? TxnRef { get; set; }
        public long? Amount { get; set; }
        public string? TransactionNo { get; set; }
        public string? ResponseCode { get; set; }
        public string? TransactionStatus { get; set; }
        public string? Message { get; set; }
    }
}
```

`VnPayReturnVerifier.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayReturnVerifier
    {
        public VnPayReturnResult Verify(VnPayOptions options, IReadOnlyDictionary<string, string> queryParameters)
        {
            var result = new VnPayReturnResult
            {
                TxnRef = Get(queryParameters, "vnp_TxnRef"),
                ResponseCode = Get(queryParameters, "vnp_ResponseCode"),
                TransactionStatus = Get(queryParameters, "vnp_TransactionStatus"),
                TransactionNo = Get(queryParameters, "vnp_TransactionNo")
            };

            if (long.TryParse(Get(queryParameters, "vnp_Amount"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
                result.Amount = amount;

            var secureHash = Get(queryParameters, "vnp_SecureHash");
            if (string.IsNullOrWhiteSpace(secureHash))
            {
                result.IsValidSignature = false;
                result.Message = "Thiếu vnp_SecureHash.";
                return result;
            }

            var filtered = queryParameters
                .Where(kvp =>
                    !string.Equals(kvp.Key, "vnp_SecureHash", StringComparison.Ordinal) &&
                    !string.Equals(kvp.Key, "vnp_SecureHashType", StringComparison.Ordinal))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            var hashData = string.Join("&", filtered.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
            var computed = ComputeHmacSha512(options.HashSecret, hashData);

            result.IsValidSignature = string.Equals(computed, secureHash, StringComparison.OrdinalIgnoreCase);
            if (!result.IsValidSignature)
            {
                result.Message = "Chữ ký không hợp lệ.";
                return result;
            }

            result.IsSuccess =
                string.Equals(result.ResponseCode, "00", StringComparison.Ordinal) &&
                string.Equals(result.TransactionStatus, "00", StringComparison.Ordinal);

            result.Message = result.IsSuccess ? "Thanh toán thành công." : "Thanh toán không thành công.";
            return result;
        }

        private static string? Get(IReadOnlyDictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value : null;
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
```

`DepositService.cs`

```csharp
using BuyOldBike_BLL.Features.Payments.VnPay;
using BuyOldBike_DAL.Repositories.Payment;
using BuyOldBike_DAL.Repositories.Seller;
using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Features.Payments
{
    public class DepositService
    {
        private readonly DepositRepository _depositRepo = new DepositRepository();
        private readonly BikePostRepository _listingRepo = new BikePostRepository();
        private readonly VnPayRequestBuilder _builder = new VnPayRequestBuilder();
        private readonly VnPayReturnVerifier _verifier = new VnPayReturnVerifier();
        private const decimal DepositRate = 0.2m;

        public (Guid orderId, string paymentUrl) CreateDepositPaymentUrl(Guid buyerId, Guid listingId, VnPayOptions options, string ipAddr)
        {
            var listing = _listingRepo.GetListingDetailById(listingId);
            if (listing?.Price == null || listing.Price <= 0) throw new InvalidOperationException("Giá listing không hợp lệ.");

            var depositAmount = Math.Round(listing.Price.Value * DepositRate, 0, MidpointRounding.AwayFromZero);
            var (order, payment) = _depositRepo.CreateDeposit(buyerId, listingId, depositAmount);

            var url = _builder.BuildPaymentUrl(options, new VnPayCreatePaymentRequest
            {
                AmountVnd = depositAmount,
                TxnRef = payment.TxnRef ?? order.OrderId.ToString("N"),
                OrderInfo = $"Dat coc {depositAmount:N0}d cho listing {listingId}",
                IpAddr = ipAddr
            });

            return (order.OrderId, url);
        }

        public bool ProcessVnPayReturn(VnPayOptions options, IReadOnlyDictionary<string, string> queryParameters, out string message)
        {
            var verify = _verifier.Verify(options, queryParameters);
            if (!verify.IsValidSignature) throw new InvalidOperationException(verify.Message ?? "Chữ ký không hợp lệ.");
            if (string.IsNullOrWhiteSpace(verify.TxnRef)) throw new InvalidOperationException("Thiếu vnp_TxnRef.");

            var (order, payment) = _depositRepo.GetDepositByTxnRef(verify.TxnRef);

            if (verify.Amount.HasValue)
            {
                var expected = VnPayRequestBuilder.ToVnPayAmount(payment.Amount ?? 0m);
                if (verify.Amount.Value != expected) throw new InvalidOperationException("Số tiền VNPay trả về không khớp.");
            }

            if (verify.IsSuccess)
            {
                _depositRepo.MarkDepositSucceeded(order.OrderId, verify.TransactionNo);
                message = verify.Message ?? "Thanh toán thành công.";
                return true;
            }

            _depositRepo.MarkDepositFailed(order.OrderId);
            message = verify.Message ?? "Thanh toán không thành công.";
            return false;
        }

        public void MarkDepositExpired(Guid orderId)
        {
            _depositRepo.MarkDepositExpired(orderId);
        }
    }
}
```

#### Bước 5: Thêm config VNPay ở Presentation

Sửa [appsettings.json](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/appsettings.json) để có:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "VnPay": {
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "TmnCode": "<TMN_CODE>",
    "HashSecret": "<HASH_SECRET>",
    "ReturnUrl": "http://localhost:51123/vnpay-return/"
  }
}
```

#### Bước 6: Thêm nút “Đặt cọc” ở Listing detail

1. XAML: đổi button “Đặt mua” thành “Đặt cọc (20%)” và gắn `Click`.
2. Code-behind:

   * Check login: `AppSession.IsAuthenticated`

   * Start `HttpListener` với prefix từ `ReturnUrl`

   * `Process.Start(url)` để mở trình duyệt

   * Chờ callback:

     * timeout: `MarkDepositExpired(orderId)`

     * receive: parse query → `ProcessVnPayReturn`

   * Reload `ListingDetailViewModel.Load(listingId)`

Mẫu XAML:

```xml
<Button Height="40" MinWidth="120" Margin="0,0,10,0" Content="Đặt cọc (20%)" Click="BtnDeposit_Click" />
```

Mẫu code-behind (chèn vào [ListingDetailWindow.xaml.cs](file:///d:/SP26/PRN212/BuyOldBike/BuyOldBike/BuyOldBike_Presentation/Views/ListingDetailWindow.xaml.cs)):

```csharp
using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.VnPay;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

private async void BtnDeposit_Click(object sender, RoutedEventArgs e)
{
    if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
    {
        MessageBox.Show("Vui lòng đăng nhập để đặt cọc.");
        return;
    }

    if (DataContext is not ListingDetailViewModel vm || vm.ListingBike == null)
    {
        MessageBox.Show("Dữ liệu listing không hợp lệ.");
        return;
    }

    var options = LoadVnPayOptions();
    var service = new DepositService();
    Guid orderId = Guid.Empty;

    try
    {
        (orderId, var payUrl) = service.CreateDepositPaymentUrl(
            AppSession.CurrentUser.UserId,
            vm.ListingBike.ListingId,
            options,
            "127.0.0.1"
        );

        var prefix = NormalizePrefix(options.ReturnUrl);
        var waitTask = WaitForReturnAsync(prefix, TimeSpan.FromMinutes(5));
        Process.Start(new ProcessStartInfo(payUrl) { UseShellExecute = true });
        var query = await waitTask;

        service.ProcessVnPayReturn(options, query, out var message);
        MessageBox.Show(message);
        vm.Load(vm.ListingBike.ListingId);
    }
    catch (TimeoutException)
    {
        if (orderId != Guid.Empty) service.MarkDepositExpired(orderId);
        MessageBox.Show("Hết thời gian chờ thanh toán.");
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private static VnPayOptions LoadVnPayOptions()
{
    IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", true, true)
        .Build();

    return new VnPayOptions
    {
        BaseUrl = config["VnPay:BaseUrl"] ?? "",
        TmnCode = config["VnPay:TmnCode"] ?? "",
        HashSecret = config["VnPay:HashSecret"] ?? "",
        ReturnUrl = config["VnPay:ReturnUrl"] ?? ""
    };
}

private static string NormalizePrefix(string url)
{
    if (string.IsNullOrWhiteSpace(url)) return "";
    return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
}

private static async Task<Dictionary<string, string>> WaitForReturnAsync(string prefix, TimeSpan timeout)
{
    using var listener = new HttpListener();
    listener.Prefixes.Add(prefix);
    listener.Start();

    var contextTask = listener.GetContextAsync();
    var completed = await Task.WhenAny(contextTask, Task.Delay(timeout));
    if (completed != contextTask) throw new TimeoutException();

    var context = await contextTask;
    var query = ParseQuery(context.Request.Url?.Query);

    var html = "<html><body>Đã nhận kết quả thanh toán. Bạn có thể quay lại ứng dụng.</body></html>";
    var bytes = Encoding.UTF8.GetBytes(html);
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength64 = bytes.Length;
    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    context.Response.OutputStream.Close();

    listener.Stop();
    return query;
}

private static Dictionary<string, string> ParseQuery(string? queryString)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    if (string.IsNullOrWhiteSpace(queryString)) return result;
    var q = queryString.StartsWith("?", StringComparison.Ordinal) ? queryString[1..] : queryString;
    foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = part.Split('=', 2);
        var key = Uri.UnescapeDataString(kv[0].Replace("+", " "));
        var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1].Replace("+", " ")) : "";
        if (!string.IsNullOrWhiteSpace(key)) result[key] = value;
    }
    return result;
}
```

#### Bước 7: Fix lỗi HttpListener (nếu gặp)

Nếu chạy bị lỗi quyền (Access is denied) khi `listener.Start()`, chạy URLACL (PowerShell/Command Prompt admin):

```bat
netsh http add urlacl url=http://localhost:51123/vnpay-return/ user=Everyone
```

Khi cần xoá:

```bat
netsh http delete urlacl url=http://localhost:51123/vnpay-return/
```

## 4) Giả định & quyết định

* Tiền cọc = 20% giá listing, làm tròn số nguyên VND.

* Trong lúc chờ thanh toán: khoá tạm listing bằng `Deposit_Pending`.

* Success: listing `Reserved`. Fail/Expired: rollback listing `Available`.

* Tích hợp theo hướng “browser + callback localhost”.

* Không hardcode secret trong code; chỉ đọc từ `appsettings.json` runtime.

## 5) Xác minh (verification)

### Build

* Build solution thành công sau khi thêm DAL/BLL/UI.

### Manual test (sandbox)

* Case success:

  * VNPay return về localhost → app báo thành công

  * DB: Payment.status = Succeeded, Order.status = Deposit\_Paid, listing.status = Reserved

* Case cancel/fail:

  * App báo không thành công

  * DB: Payment.status = Failed, Order.status = Deposit\_Failed, listing.status = Available

* Case timeout:

  * App báo timeout

  * DB: Payment.status = Expired, Order.status = Deposit\_Expired, listing.status = Available

