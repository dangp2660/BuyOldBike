using BuyOldBike_BLL.Features.Payments.VnPay;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Payment;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
 
namespace BuyOldBike_BLL.Features.Payments
{
    public class WalletTopUpVnPayService
    {
        private readonly WalletTopUpRepository _repo = new WalletTopUpRepository();
        private readonly VnPayRequestBuilder _builder = new VnPayRequestBuilder();
        private readonly VnPayReturnVerifier _verifier = new VnPayReturnVerifier();
 
        public string CreateTopUpPaymentUrl(Guid userId, decimal amount, VnPayOptions options, string ipAddr)
        {
            var payment = _repo.CreateTopUpPayment(userId, amount);
            var txnRef = payment.TxnRef ?? payment.PaymentId.ToString("N");
 
            return _builder.BuildPaymetUrl(options, new VnPayCreatePaymentRequest
            {
                AmountVnd = amount,
                TxnRef = txnRef,
                OrderInfo = $"Nap vi {amount.ToString("0", CultureInfo.InvariantCulture)} VND user {userId.ToString("N")}",
                IpAddr = ipAddr
            });
        }
 
        public bool ProcessVnPayReturn(VnPayOptions options, IReadOnlyDictionary<string, string> queryParameters, out string message)
        {
            var verify = _verifier.Verify(options, queryParameters);
            if (!verify.IsValidSignature) throw new InvalidOperationException(verify.Message ?? "Chữ ký không hợp lệ.");
            if (string.IsNullOrWhiteSpace(verify.TxnRef)) throw new InvalidOperationException("Thiếu vnp_TxnRef.");
 
            using var db = new BuyOldBikeContext();
            var payment = db.Payments
                .AsNoTracking()
                .FirstOrDefault(p =>
                    p.TxnRef == verify.TxnRef &&
                    p.PaymentType == StatusConstants.PaymentType.VN_Pay &&
                    p.OrderId == null
                );
 
            if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch nạp ví theo TxnRef.");
 
            if (verify.Amount.HasValue)
            {
                var expected = VnPayRequestBuilder.ToVnPayAmount(payment.Amount ?? 0m);
                if (verify.Amount.Value != expected) throw new InvalidOperationException("Số tiền VNPay trả về không khớp.");
            }
 
            if (verify.IsSuccess)
            {
                _repo.MarkTopUpSuccess(verify.TxnRef, verify.TransactionNo);
                message = verify.Message ?? "Thanh toán thành công.";
                return true;
            }
 
            _repo.MarkTopUpFailed(verify.TxnRef);
            message = verify.Message ?? "Thanh toán không thành công.";
            return false;
        }
    }
}
