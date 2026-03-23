using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var app = builder.Build();

app.MapMethods("/vnpay/ipn", new[] { "GET", "POST" }, async (HttpRequest req) =>
{
    var options = LoadVnPayOptions(app.Configuration);
    var query = await ReadRequestParametersAsync(req);
    var ipnService = new VnPayIpnService();
    var response = ipnService.ProcessIpn(options, query);
    return Results.Json(response);
});

app.Run();

static VnPayOptions LoadVnPayOptions(IConfiguration config)
{
    var options = new VnPayOptions
    {
        BaseUrl = (config["VnPay:BaseUrl"] ?? "").Trim(),
        ApiUrl = (config["VnPay:ApiUrl"] ?? "").Trim(),
        TmnCode = (config["VnPay:TmnCode"] ?? "").Trim(),
        HashSecret = (config["VnPay:HashSecret"] ?? "").Trim(),
        ReturnUrl = (config["VnPay:ReturnUrl"] ?? "").Trim(),
        IpnUrl = (config["VnPay:IpnUrl"] ?? "").Trim()
    };

    if (string.IsNullOrWhiteSpace(options.TmnCode)) throw new InvalidOperationException("Thiếu VnPay:TmnCode.");
    if (string.IsNullOrWhiteSpace(options.HashSecret)) throw new InvalidOperationException("Thiếu VnPay:HashSecret.");
    if (string.Equals(options.HashSecret, "CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        options.HashSecret.Contains('<') || options.HashSecret.Contains('>'))
        throw new InvalidOperationException("VnPay:HashSecret chưa được cấu hình đúng. Hãy set appsettings.local.json hoặc biến môi trường VnPay__HashSecret.");
    return options;
}

static async Task<Dictionary<string, string>> ReadRequestParametersAsync(HttpRequest req)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var kvp in req.Query)
    {
        result[kvp.Key] = kvp.Value.ToString();
    }

    if (string.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase) && req.HasFormContentType)
    {
        var form = await req.ReadFormAsync();
        foreach (var kvp in form)
        {
            result[kvp.Key] = kvp.Value.ToString();
        }
    }

    return result;
}
