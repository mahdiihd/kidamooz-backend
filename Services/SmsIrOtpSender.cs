using System.Net.Http.Json;
using System.Text.Json;

namespace Kidamooz.Services;

public sealed class SmsIrOtpSender(HttpClient http, IConfiguration config) : IMemberOtpSender
{
    public async Task SendAsync(string mobile, string code, CancellationToken ct)
    {
        var key = config["SmsIr:ApiKey"];
        var mode = config["SmsIr:Mode"] ?? "bulk";
        var parameter = config["SmsIr:CodeParameter"];
        var hasTemplate = int.TryParse(config["SmsIr:TemplateId"], out var templateId) && templateId > 0;
        var hasLine = long.TryParse(config["SmsIr:LineNumber"], out var lineNumber) && lineNumber > 0;
        var verify = mode.Equals("verify", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(key) || key.Contains('*') || (verify
                ? !hasTemplate || string.IsNullOrWhiteSpace(parameter)
                : !mode.Equals("bulk", StringComparison.OrdinalIgnoreCase) || !hasLine))
            throw new OtpDeliveryException("ارسال پیامک هنوز تنظیم نشده است.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                verify ? "https://api.sms.ir/v1/send/verify" : "https://api.sms.ir/v1/send/bulk");
            request.Headers.Add("x-api-key", key);
            request.Content = verify
                ? JsonContent.Create(new
                {
                    mobile, templateId,
                    parameters = new[] { new { name = parameter, value = code } }
                })
                : JsonContent.Create(new
                {
                    lineNumber,
                    messageText = $"کیدینگو\nکد ورود شما: {code}\nاعتبار: ۲ دقیقه\nاین کد را در اختیار دیگران قرار ندهید.",
                    mobiles = new[] { mobile },
                    sendDateTime = (long?)null
                });
            using var response = await http.SendAsync(request, ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!response.IsSuccessStatusCode || json.RootElement.GetProperty("status").GetInt32() != 1)
                throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
            var data = json.RootElement.GetProperty("data");
            if (verify)
            {
                if (data.GetProperty("messageId").GetInt64() <= 0)
                    throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
            }
            else
            {
                var ids = data.GetProperty("messageIds");
                if (ids.ValueKind != JsonValueKind.Array || ids.GetArrayLength() != 1
                    || ids[0].ValueKind != JsonValueKind.Number || !ids[0].TryGetInt64(out var id) || id <= 0)
                    throw new OtpDeliveryException("پیامک برای این شماره پذیرفته نشد. تنظیمات دریافت پیامک را بررسی کنید.");
            }
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
