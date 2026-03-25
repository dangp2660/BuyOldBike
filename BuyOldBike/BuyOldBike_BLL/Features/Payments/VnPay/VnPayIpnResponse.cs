namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public sealed class VnPayIpnResponse
    {
        public string RspCode { get; init; } = "";
        public string Message { get; init; } = "";

        public static VnPayIpnResponse Ok(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "00", Message = message ?? "Confirm Success" };
        }

        public static VnPayIpnResponse OrderNotFound(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "01", Message = message ?? "Order not found" };
        }

        public static VnPayIpnResponse AlreadyConfirmed(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "02", Message = message ?? "Order already confirmed" };
        }

        public static VnPayIpnResponse InvalidAmount(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "04", Message = message ?? "Invalid amount" };
        }

        public static VnPayIpnResponse InvalidSignature(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "97", Message = message ?? "Invalid signature" };
        }

        public static VnPayIpnResponse UnknownError(string? message = null)
        {
            return new VnPayIpnResponse { RspCode = "99", Message = message ?? "Unknown error" };
        }
    }
}

