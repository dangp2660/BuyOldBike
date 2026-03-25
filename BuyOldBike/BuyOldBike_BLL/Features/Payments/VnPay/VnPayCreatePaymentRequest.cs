using System;

namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayCreatePaymentRequest
    {
        public decimal AmountVnd { get; set; }
        public string TxnRef { get; set; } = "";
        public string OrderInfo { get; set; } = "";
        public string IpAddr { get; set; } = "127.0.0.1";
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}