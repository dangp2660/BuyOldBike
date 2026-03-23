namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayOptions
    {
        public string BaseUrl { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public string TmnCode { get; set; } = "";
        public string HashSecret { get; set; } = "";
        public string ReturnUrl { get; set; } = "";
        public string IpnUrl { get; set; } = "";
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string OrderType { get; set; } = "other";
    }
}
