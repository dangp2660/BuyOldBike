using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_Presentation.Payments
{
    public static class VnPayReturnListener
    {
        public static async Task<Dictionary<string, string>> WaitForReturnAsync(string returnUrl, TimeSpan timeout)
        {
            var prefix = NormalizePrefix(returnUrl);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new InvalidOperationException("VnPay:ReturnUrl không hợp lệ.");
            }

            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    return await WaitForReturnViaTcpAsync(returnUrl, timeout);
                }

                throw new InvalidOperationException($"Không thể mở ReturnUrl: {prefix} ({ex.Message})");
            }

            try
            {
                var contextTask = listener.GetContextAsync();
                var completed = await Task.WhenAny(contextTask, Task.Delay(timeout));
                if (completed != contextTask)
                {
                    throw new TimeoutException();
                }

                var context = await contextTask;
                var query = ParseQuery(context.Request.Url?.Query);

                var html = "<html><body>Đã nhận kết quả thanh toán. Bạn có thể quay lại ứng dụng.</body></html>";
                var bytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();

                return query;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task<Dictionary<string, string>> WaitForReturnViaTcpAsync(string returnUrl, TimeSpan timeout)
        {
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) ||
                uri.Port <= 0)
            {
                throw new InvalidOperationException("VnPay:ReturnUrl không hợp lệ.");
            }

            var expectedPath = NormalizePath(string.IsNullOrWhiteSpace(uri.AbsolutePath) ? "/" : uri.AbsolutePath);
            var listener = new TcpListener(IPAddress.Loopback, uri.Port);

            try
            {
                listener.Start();
            }
            catch (SocketException ex) when (ex.ErrorCode == 10048)
            {
                throw new InvalidOperationException($"Port {uri.Port} đang được sử dụng. Hãy đổi VnPay:ReturnUrl sang port khác.");
            }

            try
            {
                var acceptTask = listener.AcceptTcpClientAsync();
                var completed = await Task.WhenAny(acceptTask, Task.Delay(timeout));
                if (completed != acceptTask)
                {
                    throw new TimeoutException();
                }

                using var client = await acceptTask;
                using var stream = client.GetStream();

                var requestText = await ReadHttpHeaderAsync(stream, 64 * 1024);
                var (pathAndQuery, _) = ParseRequestLine(requestText);

                var full = $"http://localhost{pathAndQuery}";
                if (!Uri.TryCreate(full, UriKind.Absolute, out var requestUri))
                {
                    throw new InvalidOperationException("Không đọc được URL trả về từ trình duyệt.");
                }

                var actualPath = NormalizePath(string.IsNullOrWhiteSpace(requestUri.AbsolutePath) ? "/" : requestUri.AbsolutePath);
                if (!actualPath.StartsWith(expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("URL trả về không khớp ReturnUrl.");
                }

                var query = ParseQuery(requestUri.Query);
                await WriteHttpOkAsync(stream);
                return query;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task<string> ReadHttpHeaderAsync(NetworkStream stream, int maxBytes)
        {
            var buffer = new byte[4096];
            var total = 0;
            using var ms = new MemoryStream();
            while (total < maxBytes)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;
                await ms.WriteAsync(buffer, 0, read);
                total += read;
                var text = Encoding.ASCII.GetString(ms.ToArray());
                if (text.Contains("\r\n\r\n", StringComparison.Ordinal)) return text;
            }
            return Encoding.ASCII.GetString(ms.ToArray());
        }

        private static (string pathAndQuery, string httpVersion) ParseRequestLine(string requestText)
        {
            var firstLineEnd = requestText.IndexOf("\r\n", StringComparison.Ordinal);
            var firstLine = firstLineEnd >= 0 ? requestText[..firstLineEnd] : requestText;
            var parts = firstLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Request không hợp lệ.");
            }
            var httpVersion = parts.Length >= 3 ? parts[2] : "HTTP/1.1";
            return (parts[1], httpVersion);
        }

        private static Task WriteHttpOkAsync(NetworkStream stream)
        {
            var body = "<html><body>Đã nhận kết quả thanh toán. Bạn có thể quay lại ứng dụng.</body></html>";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Connection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            return stream.WriteAsync(Combine(headerBytes, bodyBytes), 0, headerBytes.Length + bodyBytes.Length);
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        private static string NormalizePrefix(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            if (path.Length == 1 && path[0] == '/') return "/";
            return path.TrimEnd('/');
        }

        private static Dictionary<string, string> ParseQuery(string? queryString)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(queryString)) return result;

            var q = queryString.StartsWith("?", StringComparison.Ordinal) ? queryString[1..] : queryString;
            foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0].Replace("+", " "));
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1].Replace("+", " ")) : "";
                if (!string.IsNullOrWhiteSpace(key))
                {
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
