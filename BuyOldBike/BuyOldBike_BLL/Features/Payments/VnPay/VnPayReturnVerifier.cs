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

            var hashData = string.Join("&", filtered.Select(kvp => $"{VnPayRequestBuilder.UrlEncode(kvp.Key)}={VnPayRequestBuilder.UrlEncode(kvp.Value)}"));
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
