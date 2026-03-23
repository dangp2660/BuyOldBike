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