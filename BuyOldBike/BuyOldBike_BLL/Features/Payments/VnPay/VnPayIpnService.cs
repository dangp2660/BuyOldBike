using BuyOldBike_BLL.Features.Payments.VnPay;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Payment;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Features.Payments
{
    public class VnPayIpnService
    {
        private readonly VnPayReturnVerifier _verifier = new VnPayReturnVerifier();
        private readonly DepositRepository _depositRepo = new DepositRepository();
        private readonly WalletTopUpRepository _walletTopUpRepo = new WalletTopUpRepository();

        public VnPayIpnResponse ProcessIpn(VnPayOptions options, IReadOnlyDictionary<string, string> queryParameters)
        {
            try
            {
                var verify = _verifier.Verify(options, queryParameters);
                if (!verify.IsValidSignature) return VnPayIpnResponse.InvalidSignature();
                if (string.IsNullOrWhiteSpace(verify.TxnRef)) return VnPayIpnResponse.OrderNotFound("Missing vnp_TxnRef");

                using var db = new BuyOldBikeContext();
                var payment = db.Payments
                    .AsNoTracking()
                    .Include(p => p.Order)
                    .FirstOrDefault(p =>
                        p.TxnRef == verify.TxnRef &&
                        p.PaymentType == StatusConstants.PaymentType.VN_Pay
                    );

                if (payment == null) return VnPayIpnResponse.OrderNotFound();

                if (verify.Amount.HasValue)
                {
                    var expected = VnPayRequestBuilder.ToVnPayAmount(payment.Amount ?? 0m);
                    if (verify.Amount.Value != expected) return VnPayIpnResponse.InvalidAmount();
                }

                if (!string.Equals(payment.Status, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal))
                {
                    return VnPayIpnResponse.AlreadyConfirmed();
                }

                if (verify.IsSuccess)
                {
                    if (payment.OrderId.HasValue)
                    {
                        _depositRepo.MaskDepositSuccess(payment.OrderId.Value, verify.TransactionNo);
                    }
                    else
                    {
                        _walletTopUpRepo.MarkTopUpSuccess(verify.TxnRef, verify.TransactionNo);
                    }

                    return VnPayIpnResponse.Ok();
                }

                if (payment.OrderId.HasValue)
                {
                    _depositRepo.MaskDepositFailed(payment.OrderId.Value);
                }
                else
                {
                    _walletTopUpRepo.MarkTopUpFailed(verify.TxnRef);
                }

                return VnPayIpnResponse.Ok();
            }
            catch
            {
                return VnPayIpnResponse.UnknownError();
            }
        }
    }
}

