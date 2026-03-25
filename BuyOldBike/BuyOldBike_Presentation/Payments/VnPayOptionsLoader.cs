using System;
using BuyOldBike_BLL.Features.Payments.VnPay;
using Microsoft.Extensions.Configuration;

namespace BuyOldBike_Presentation.Payments
{
    public static class VnPayOptionsLoader
    {
        public static VnPayOptions LoadValidated()
        {
            var options = Load();
            Validate(options);
            return options;
        }

        public static VnPayOptions Load()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.local.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            return new VnPayOptions
            {
                BaseUrl = (config["VnPay:BaseUrl"] ?? "").Trim(),
                ApiUrl = (config["VnPay:ApiUrl"] ?? "").Trim(),
                TmnCode = (config["VnPay:TmnCode"] ?? "").Trim(),
                HashSecret = (config["VnPay:HashSecret"] ?? "").Trim(),
                ReturnUrl = (config["VnPay:ReturnUrl"] ?? "").Trim(),
                IpnUrl = (config["VnPay:IpnUrl"] ?? "").Trim()
            };
        }

        public static void Validate(VnPayOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.BaseUrl)) throw new InvalidOperationException("Thiếu VnPay:BaseUrl.");
            if (string.IsNullOrWhiteSpace(options.ReturnUrl)) throw new InvalidOperationException("Thiếu VnPay:ReturnUrl.");
            if (string.IsNullOrWhiteSpace(options.TmnCode) || options.TmnCode.Contains('<') || options.TmnCode.Contains('>'))
                throw new InvalidOperationException(
                    "VnPay:TmnCode chưa được cấu hình đúng. " +
                    "Hãy set trong appsettings.local.json hoặc biến môi trường VnPay__TmnCode."
                );
            if (string.IsNullOrWhiteSpace(options.HashSecret) || options.HashSecret.Contains('<') || options.HashSecret.Contains('>'))
                throw new InvalidOperationException(
                    "VnPay:HashSecret chưa được cấu hình đúng. " +
                    "Hãy set trong appsettings.local.json hoặc biến môi trường VnPay__HashSecret."
                );
        }
    }
}
