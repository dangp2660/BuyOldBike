using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Payments.VnPay
{
    public class VnPayRequestBuilder
    {
        public string BuildPaymetUrl(VnPayOptions options, VnPayCreatePaymentRequest request)
        {
            long amount = ToVnPayAmount(request.AmountVnd);
            var requestTime = request.CreateDate;
            if (requestTime.Kind == DateTimeKind.Unspecified) requestTime = DateTime.SpecifyKind(requestTime, DateTimeKind.Local);
            var gmt7 = GetGmt7TimeZone();
            var createDateGmt7 = gmt7 == null ? requestTime : TimeZoneInfo.ConvertTime(requestTime, gmt7);
            var expireDateGmt7 = createDateGmt7.AddMinutes(15);

            var createDate = createDateGmt7.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var expireDate = expireDateGmt7.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var vnParam = new SortedDictionary<string, string>(StringComparer.Ordinal)
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
                ["vnp_CreateDate"] = createDate,
                ["vnp_ExpireDate"] = expireDate
            };
            if (!string.IsNullOrWhiteSpace(options.IpnUrl))
            {
                vnParam["vnp_IpnUrl"] = options.IpnUrl;
            }
            var query = string.Join("&", vnParam
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => $"{UrlEncode(x.Key)}={UrlEncode(x.Value)}"));
            var secureHash = ComputeHmacSha512(options.HashSecret, query);
            return $"{options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        private static TimeZoneInfo? GetGmt7TimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } catch { }
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); } catch { }
            return null;
        }

        public static long ToVnPayAmount(decimal amontVnd)
        {
            decimal value = Math.Round(amontVnd * 100m, 0, MidpointRounding.AwayFromZero);
            return (long)value;
        }

        public static string ComputeHmacSha512(string key, string data)
        {
            var normalizedKey = (key ?? string.Empty).Trim();
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(normalizedKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var item in hash) { sb.Append(item.ToString("x2")); }
            return sb.ToString();
        }

        public static string UrlEncode(string? value)
        {
            return WebUtility.UrlEncode(value ?? "");
        }
    }
}
