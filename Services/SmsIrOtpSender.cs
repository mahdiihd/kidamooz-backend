using System.Net.Http.Json;
using System.Text.Json;

namespace Kidamooz.Services;

public sealed class SmsIrOtpSender(HttpClient http, IConfiguration config) : IMemberOtpSender
{
    public async Task SendAsync(string mobile, string code, CancellationToken ct)
    {
        var key = config["SmsIr:ApiKey"];
        var parameter = config["SmsIr:CodeParameter"];
        if (string.IsNullOrWhiteSpace(key) || key.Contains('*')
            || !int.TryParse(config["SmsIr:TemplateId"], out var templateId) || templateId <= 0
            || string.IsNullOrWhiteSpace(parameter))
            throw new OtpDeliveryException("ارسال پیامک هنوز تنظیم نشده است.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sms.ir/v1/send/verify");
            request.Headers.Add("x-api-key", key);
            request.Content = JsonContent.Create(new
            {
                mobile,
                templateId,
                parameters = new[] { new { name = parameter, value = code } }
            });
            using var response = await http.SendAsync(request, ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!response.IsSuccessStatusCode || json.RootElement.GetProperty("status").GetInt32() != 1)
                throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
            or OperationCanceledException or InvalidOperationException or FormatException)
        {
            // Provider bodies, headers and exceptions may contain credentials or OTPs.
            throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
        }
    }
}
