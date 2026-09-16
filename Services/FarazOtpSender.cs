using System.Net.Http.Json;
using System.Text.Json;

namespace Kidamooz.Services;

public sealed class FarazOtpSender(HttpClient http, IConfiguration config) : IMemberOtpSender
{
    public async Task SendAsync(string mobile, string code, CancellationToken ct)
    {
        var key = config["Faraz:ApiKey"];
        var pattern = config["Faraz:PatternCode"];
        var line = config["Faraz:LineNumber"];
        var parameter = config["Faraz:CodeParameter"] ?? "code";
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(pattern)
            || !long.TryParse(line, out var number) || number <= 0 || string.IsNullOrWhiteSpace(parameter))
            throw new OtpDeliveryException("ارسال پیامک هنوز تنظیم نشده است.");
        if (code.Length != 6 || code.Any(c => c < '0' || c > '9'))
            throw new OtpDeliveryException("کد ورود باید شش‌رقمی باشد.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.iranpayamak.com/ws/v1/sms/pattern");
            request.Headers.Add("Api-Key", key);
            request.Content = JsonContent.Create(new
            {
                code = pattern,
                attributes = new Dictionary<string, string> { [parameter] = code },
                recipient = mobile,
                line_number = line,
                number_format = "english"
            });
            using var response = await http.SendAsync(request, ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!response.IsSuccessStatusCode || json.RootElement.GetProperty("status").GetString() != "success")
                throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
            or OperationCanceledException or InvalidOperationException or FormatException)
        {
            // Never expose provider bodies or exceptions containing credentials or OTPs.
            throw new OtpDeliveryException("ارسال پیامک ناموفق بود. کمی بعد دوباره تلاش کنید.");
        }
    }
}
