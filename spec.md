# مشخصات پروژه بک‌اند کیدآموز

این سند براساس کد و تنظیمات پروژه تهیه شده است؛ نام فایل مطابق درخواست `spec.md` است.

## نقش و فناوری

سرویس ASP.NET Core با هدف .NET 9، API مورد استفاده پنل مدیریت و اپ را فراهم می‌کند. دسترسی داده با EF Core 9 و SQL Server و احراز هویت با JWT انجام می‌شود. وابستگی‌ها در `back.csproj`، ترتیب راه‌اندازی و middlewareها در `Program.cs` و جزئیات ثبت سرویس‌ها در `Infrastructure/Startup` مشخص‌اند.

## ساختار

- `Controllers/Admin` و `Controllers/Public`: نقاط ورود API مدیریتی و عمومی/عضو.
- `Services`: منطق قصه، دسته‌بندی، کاتالوگ، عضو، پروفایل کودک، علاقه‌مندی، مشارکت، اعلان و گزارش‌ها.
- `Repositories` و `Repositories/Interfaces`: دسترسی داده و قراردادهای آن.
- `Domain`، `DTOs` و `Mapping`: مدل دامنه، قرارداد انتقال و تبدیل‌ها.
- `Data/AppDbContext.cs` و `Data/Migrations`: مدل پایگاه داده و تغییرات طرح.
- `Data/DbInitializer.cs`: migration و داده اولیه در شروع برنامه.
- `Infrastructure`: احراز هویت، ذخیره‌سازی، ارسال اعلان و اتصال‌های هوش مصنوعی.
- `Infrastructure/Startup`: ثبت سرویس‌های برنامه، API، ذخیره‌سازی و اتصال‌های خارجی، مدیریت خطا و هدرها و callbackهای شروع برنامه؛ ترتیب فراخوانی در `Program.cs` باقی مانده است.
- `tools/DbImport` و `tools/DbExport`: پروژه‌های مستقل ورود و خروج داده؛ از پروژه وب مستثنا هستند.

## اتصال‌ها و پیکربندی

کد شامل ذخیره‌سازی سازگار با S3 برای Liara، ارسال اعلان Firebase، تولید قصه و تصویر با Gemini و روایت صوتی با سرویس Edge TTS است. تنظیمات و متغیرهای محیطی را مطابق کلاس‌های تنظیمات و فایل‌های `Infrastructure/Startup` بخوانید؛ اعتبارنامه واقعی نباید در این سند ثبت شود.

Swagger در محیط توسعه فعال است. برنامه در محیط توسعه برای بازکردن مرورگر نیز اقدام می‌کند. راه‌اندازی، migration و seed را اجرا می‌کند، بنابراین اجرای محلی باید به پایگاه داده آزمایشی متصل باشد.

## تولید قصه و کاور از نقاشی

این بخش خط مبنای پیاده‌سازی مشاهده‌شده در تاریخ `2026-09-10` است؛ رفتار مطلوب آینده یا تأیید تنظیمات سرور زنده نیست. هنگام تغییر این قابلیت، قراردادها و رفتارهای این بخش را همراه کد به‌روز کنید.

### مسیر اجرای فعلی

1. اپ فایل نقاشی را به بک‌اند می‌فرستد؛ سرور هویت عضو و سهمیه روزانه را بررسی و پیش‌نویس را با وضعیت `generating` ثبت می‌کند.
2. سرور نقاشی را در ذخیره‌سازی رسانه بارگذاری می‌کند و همان بایت‌های تصویر را برای تولید قصه به Gemini می‌فرستد.
3. Gemini عنوان و توضیح فارسی، متن فارسی قصه و توضیح داخلی کاور را برمی‌گرداند؛ عنوان و توضیح انگلیسی تولید نمی‌شوند.
4. پس از دریافت قصه، روایت صوتی تولید می‌شود. فقط با `generateCover=true` تولید کاور نیز هم‌زمان آغاز می‌شود؛ پیش‌فرض هیچ درخواست تولید تصویر ندارد.
5. در حالت پیش‌فرض، `CoverUrl` همان `DrawingUrl` است و آپلود تکراری انجام نمی‌شود. کاور تولیدشده فقط در حالت انتخاب صریح بارگذاری می‌شود. آدرس‌ها و محتوای قصه ذخیره و وضعیت پیش‌نویس `ready` می‌شود.

این مسیر در همان درخواست ایجاد انجام می‌شود؛ قرارداد فعلی پاسخ فوریِ شناسه کار پس‌زمینه ندارد. تولید موفق کاور و قصه معمولاً دو درخواست جدا به Gemini دارد؛ تولید صوت مسیر مستقلی است.

### درخواست اپ به بک‌اند

```http
POST {apiBaseUrl}/api/v1/me/story-drafts
Authorization: Bearer <member-token>
X-Device-Id: <device-id>
Content-Type: multipart/form-data; boundary=<generated-boundary>
```

- بدنه شامل فیلد `drawing` از نوع فایل، همراه نام فایل و فیلد اختیاری `generateCover` از نوع boolean است. مقدار پیش‌فرض سرور `false` است؛ کلاینت جدید مقدار `true` یا `false` را صریح می‌فرستد. کلاینت قدیمی نیز بدون تغییر از نقاشی اصلی استفاده می‌کند. اپ آن را با `FormData` می‌سازد و boundary توسط کلاینت HTTP تعیین می‌شود.
- سن کودک، نام کودک، سبک قصه، توضیح نقاشی یا دستور سفارشی در درخواست ایجاد فعلی ارسال نمی‌شوند.
- کنترلر نقش `member` می‌خواهد. شناسه عضو از هویت احرازشده و شناسه دستگاه از هدر گرفته می‌شود؛ این شناسه‌ها در بدنه درخواست تولید Gemini قرار نمی‌گیرند.
- محدودیت اندازه درخواست و بدنه multipart در کنترلر `15 * 1024 * 1024` بایت است؛ این سقف کل درخواست است، نه تضمین پذیرش فایل دقیقاً ۱۵ مگابایتی.
- فایل خالی رد می‌شود. عبور از سهمیه روزانه پاسخ `429` می‌دهد. موفقیت با `201 Created` و `StoryDraftDto` برمی‌گردد.

منابع: [کلاینت پیش‌نویس اپ](../android/src/app/core/services/story-draft-api.service.ts)، [هدرهای درخواست اپ](../android/src/app/core/services/api.service.ts)، [کنترلر](Controllers/Public/StoryDraftsController.cs) و [گردش کار تولید](Services/StoryDraftService.cs).

### درخواست تولید قصه به Gemini

```http
POST {BaseUrl}/v1beta/models/{Model}:generateContent
x-goog-api-key: <secret-from-environment>
Content-Type: application/json; charset=utf-8
Accept: application/json
```

بدنه زیر شکل واقعی درخواست را نشان می‌دهد؛ مقادیر داخل `<...>` جایگزین توضیحی هستند:

```json
{
  "contents": [
    {
      "parts": [
        { "text": "<story-prompt>" },
        {
          "inline_data": {
            "mime_type": "image/jpeg",
            "data": "<base64-drawing-bytes>"
          }
        }
      ]
    }
  ],
  "generationConfig": {
    "temperature": 0.8,
    "responseMimeType": "application/json"
  }
}
```

نوع MIME از نوع فایل و در صورت نیاز پسوند آن تعیین می‌شود: `image/jpeg`، `image/png` یا `image/webp`؛ مسیر پیش‌فرض تشخیص، JPEG است. این تشخیص MIME به‌معنی تبدیل واقعی فرمت تصویر نیست. تصویر به‌صورت بایت‌های Base64 ارسال می‌شود، نه URL ذخیره‌سازی آن.

متن ثابت دستور در زمان ثبت این مشخصات:

```text
You are a children's story writer. The image is a child's drawing.
Create a short Persian story based on that drawing.

Rules:
- titleFa, descriptionFa, storyScript MUST be Persian (Farsi) WITHOUT Arabic diacritics (no tashkeel/harakat)
- Suitable for ages 3–8
- Gentle, joyful, no violence or fear
- storyScript is for reading aloud by a parent/child, about 1–2 minutes (roughly 180–350 Persian words)
- coverPrompt in English for a children's book illustration inspired by this drawing: colorful, joyful, children's book illustration style, no text on the image

Return ONLY raw JSON with no markdown:
{"titleFa":"...","descriptionFa":"...","storyScript":"...","coverPrompt":"..."}
Plain text only; no HTML, links, or scripts.
```

عنوان، توضیح و متن قصه فقط فارسی تولید می‌شوند. فیلدهای قدیمی `titleEn` و `descriptionEn` در DTO و دیتابیس برای سازگاری باقی‌اند و برای تولید جدید از متن فارسی پر می‌شوند. ترجمه خودکار هنگام تأیید نیز فراخوانی API ندارد؛ داده انگلیسی قدیمی موجود حفظ می‌شود. دستور داخلی `coverPrompt` برای مدل تصویر همچنان انگلیسی است. سن، تعداد کلمات، لحن و نبود اعراب در این مرحله دستور متنی به مدل هستند، نه پارامترهای مستقل API یا تضمین اعتبارسنجی خروجی.

پاسخ مورد انتظار از متن خروجی مدل:

```json
{
  "titleFa": "...",
  "descriptionFa": "...",
  "storyScript": "...",
  "coverPrompt": "..."
}
```

کد، متن اولین بخش اولین candidate را استخراج و JSON آن را تجزیه می‌کند؛ عنوان فارسی و متن قصه نباید خالی باشند. پاک‌سازی متن و سقف طول اعمال می‌شود: عنوان‌ها ۳۰۰، توضیح‌ها ۲۰۰۰، متن قصه ۸۰۰۰ و دستور کاور ۱۰۰۰ نویسه. اگر `coverPrompt` خالی باشد، توضیح پیش‌فرض براساس عنوان فارسی ساخته می‌شود. برای درخواست قصه، `maxOutputTokens`، `responseSchema` و تنظیمات ایمنی صریح ارسال نشده‌اند.

منبع: [GeminiStoryClient.cs](Infrastructure/Ai/GeminiStoryClient.cs).

### درخواست تولید کاور به Gemini

این درخواست در مسیر ایجاد فقط با انتخاب صریح `generateCover=true` ارسال می‌شود. نبود یا false بودن آن، تولید تصویر و هزینه آن را حذف می‌کند. انتخاب عادی نقاشی خطا نیست و `UsedFallbackCover=false` دارد؛ فقط شکست تولید کاورِ درخواستی باعث `UsedFallbackCover=true` می‌شود. مسیر بازتولید کاور مستقل همچنان درخواست صریح تولید تصویر محسوب می‌شود. این انتخاب تعرفه یا پرداخت جدیدی برای کاربر تعریف نمی‌کند.

```http
POST {BaseUrl}/v1beta/models/{CoverImageModel}:generateContent
x-goog-api-key: <secret-from-environment>
Content-Type: application/json; charset=utf-8
Accept: application/json
```

```json
{
  "contents": [
    {
      "parts": [
        { "text": "<cover-prompt-template-with-subject>" }
      ]
    }
  ],
  "generationConfig": {
    "responseModalities": ["TEXT", "IMAGE"]
  }
}
```

قالب دقیق متن درخواست:

```text
Create one children's book cover illustration.
Style: colorful, joyful, soft lighting, gentle characters, picture-book look.
Absolutely no text, letters, watermark, logo, or signature in the image.
Subject: {coverPrompt.Trim()}
```

متغیر `coverPrompt` از خروجی مرحله تولید قصه می‌آید. خود نقاشی و متن کامل قصه به مدل کاور فرستاده نمی‌شوند؛ اتصال معنایی تصویر نهایی با نقاشی از طریق همین توضیح متنی است. اندازه، نسبت تصویر، کیفیت، `temperature` و سقف توکن کاور در درخواست تعیین نشده‌اند.

بایت‌های تصویر از بخش‌های پاسخ با نام `inlineData` یا `inline_data` استخراج می‌شوند. در مسیر فعلی ذخیره کاور تولیدی، نام فایل با پسوند `.jpg` و نوع `image/jpeg` ارسال می‌شود؛ تعیین این نام و نوع به‌تنهایی تبدیل فرمت بایت‌های خروجی مدل نیست.

منبع: [GeminiCoverImageGenerator.cs](Infrastructure/Ai/GeminiCoverImageGenerator.cs).

### مدل‌ها، تنظیمات و تقدم مقادیر

اتصال پیش‌فرض در این بازنگری به مسیر اختصاصی Gemini در 1xAi تغییر کرد. بدنه‌های قصه، بازنویسی، ترجمه و کاور و نام مدل‌ها حفظ شده‌اند؛ کلید در هدر `x-goog-api-key` ارسال می‌شود و دیگر در URL قرار نمی‌گیرد. مستند مرجع: [1xAi](https://1xai.ir/docs).

برای فعال‌سازی، کلید تازه 1xAi را در متغیر محیطی `GEMINI_API_KEY` یا `Gemini__ApiKey` تنظیم کنید. کلید قبلی Google برای درگاه جدید مناسب نیست. کلید قبلاً افشاشده نباید استفاده شود. اگر متغیر `Gemini__ApiKey` از قبل تنظیم است، بر `GEMINI_API_KEY` اولویت دارد. مقدار `GEMINI_BASE_URL` یا `Gemini__BaseUrl` قدیمی نیز پیش‌فرض جدید را بازنویسی می‌کند؛ مقدار مناسب برای این اتصال `https://1xai.ir/gemini` است، نه `/v1` و نه `/gemini/v1beta`.

آزمون قرارداد با پاسخ شبیه‌سازی‌شده، ساختار درخواست و پردازش پاسخ را بررسی می‌کند؛ دسترسی واقعی حساب، موجودی و پشتیبانی عملی مدل تصویر تنها با آزمون زنده قابل تأیید است.

نتیجه استقرار `2026-09-10`: بک‌اند روی سرور به مسیر 1xAi متصل شد. آزمون مستقل تولید متن با `gemini-flash-latest` و تولید تصویر با `gemini-2.5-flash-image` موفق بود (تقریباً ۳٫۲ و ۵٫۷ ثانیه). این زمان‌ها فقط مشاهده یک آزمون هستند، نه تضمین کارایی. کلید فقط در تنظیمات خصوصی سرور نگهداری می‌شود. این آزمون‌ها جایگزین آزمایش کامل ساخت قصه از اپ با حساب عضو نیستند.

- مقدار مدل قصه در `appsettings.json` فعلی: `gemini-2.0-flash`.
- پیش‌فرض کلاس تنظیمات و مقدار جایگزین مدل قصه در کلاینت: `gemini-flash-latest`.
- پیش‌فرض مدل کاور: `gemini-2.5-flash-image`.
- آدرس پایه پیش‌فرض: `https://1xai.ir/gemini`؛ درخواست‌ها از مسیر اختصاصی Gemini در 1xAi عبور می‌کنند.
- پیش‌فرض‌های Docker Compose برای قصه و کاور به‌ترتیب `gemini-flash-latest` و `gemini-2.5-flash-image` هستند؛ بنابراین مدل محلی و استقرار الزاماً یکسان نیستند.

پس از خواندن بخش `Gemini` از پیکربندی، متغیرهای محیطی صریح به ترتیب زیر روی تنظیمات اعمال می‌شوند:

```text
ApiKey:          Gemini__ApiKey          -> GEMINI_API_KEY          -> configured value
Model:           Gemini__Model           -> GEMINI_MODEL            -> configured value
CoverImageModel: Gemini__CoverImageModel  -> GEMINI_COVER_IMAGE_MODEL -> configured value
BaseUrl:         Gemini__BaseUrl         -> GEMINI_BASE_URL         -> configured value
```

اولویت به اولین مقدار غیر null است؛ کنترل مقادیر خالی در کلاینت‌ها نیز وجود دارد. مقدار نهایی محیط سرور زنده در این بررسی خوانده نشده است. مسیر پیش‌فرض جدید از 1xAi استفاده می‌کند. پوشه `deploy/ai-proxy` همچنان یک مسیر جایگزین قدیمی برای Google است و در اتصال پیش‌فرض جدید استفاده نمی‌شود. هیچ کلید واقعی در این سند ثبت نمی‌شود.

منابع: [تنظیمات پیش‌فرض](Infrastructure/Ai/GeminiSettings.cs)، [ثبت کلاینت و متغیرهای محیطی](Infrastructure/Startup/ExternalServiceExtensions.cs)، [پیکربندی استقرار](../deploy/docker-compose.yml) و [پروکسی](../deploy/ai-proxy/worker.js).

### زمان انتظار و رفتار شکست

مصرف هر پاسخ معتبر Gemini با رویداد `GeminiUsage` در لاگ کنسول بک‌اند ثبت می‌شود. فیلد `Operation` مرحله را با `story`، `rewrite` یا `cover` جدا می‌کند. شمارنده‌های `PromptTokens`، `OutputTokens`، `ThinkingTokens`، `CachedTokens` و `TotalTokens` مستقیماً از `usageMetadata` پاسخ می‌آیند؛ مجموع دوباره محاسبه نمی‌شود. `TraceId` درخواست، در صورت وجود، دو مرحله یک درخواست را مرتبط می‌کند. نبود شمارنده برابر صفر نیست و با مقدار خالی و `UsageAvailable=false` برای نبود metadata گزارش می‌شود. در خطای HTTP یا لغو قبل از دریافت پاسخ، شمارش دقیق در دسترس نیست؛ گزارش ارائه‌دهنده برای هزینه این موارد ملاک است.

بعد از انتشار این تغییر، روی سرور می‌توان گزارش را دید:

```bash
docker logs --since 1h kidamooz-api 2>&1 | grep GeminiUsage
```

این گزارش در لاگ کانتینر است، نه در پنل ادمین یا جدول دیتابیس؛ ماندگاری آن تابع تنظیمات نگهداری لاگ Docker است. متن قصه، نقاشی و کلید در رویداد مصرف ثبت نمی‌شوند؛ ثبت بدنه خام خطای ارائه‌دهنده نیز حذف شده است.

- کلاینت HTTP با نام `gemini` مهلت دو دقیقه دارد.
- کلاینت هدر `User-Agent: Kidamooz/1.0` می‌فرستد. در بررسی سرور، درخواست Python با هدر پیش‌فرض پاسخ 403 گرفت، ولی درخواست با این هدر موفق بود؛ این مشاهده جایگزین آزمون تولید واقعی نیست.
- تولید کاور در گردش ایجاد پیش‌نویس، علاوه بر مهلت کلاینت، توکن لغو متصل به درخواست با مهلت ۱۸ ثانیه دارد.
- در شکست تولید کاور، نبود تصویر در پاسخ یا پایان مهلت داخلی کاور، بایت‌های نقاشی اصلی به‌عنوان کاور استفاده می‌شوند و `UsedFallbackCover` برابر `true` می‌شود. لغو درخواست اصلی با این حالت جایگزین یکسان نیست.
- شکست معمول تولید قصه مسیر ایجاد را ناموفق می‌کند؛ خطاهای غیرلغو در سرویس، وضعیت `failed` و پیام خطا را ثبت می‌کنند.
- شکست غیرلغو تولید صوت می‌تواند با ادامه فرایند و آدرس صوت خالی همراه باشد؛ وضعیت `ready` به‌تنهایی تضمین وجود صوت نیست.
- در این مسیر، حلقه تلاش مجدد صریح برای درخواست‌های Gemini تعریف نشده است. مسیرهای بازنویسی قصه و بازتولید کاور جدا هستند و جزو درخواست ایجاد اولیه نیستند.

منبع: [StoryDraftService.cs](Services/StoryDraftService.cs). برای تغییرات آینده، همراه هر تغییر مدل، دستور، ورودی یا زمان انتظار، اثر آن بر خروجی قصه، ارتباط کاور با نقاشی، حالت جایگزین و سازگاری کلاینت اپ بررسی و در همین بخش ثبت شود.

## ساخت و بررسی

از پوشه `Back` اجرا کنید:

```powershell
dotnet build back.csproj
```

آزمون قرارداد اتصال Gemini با HTTP شبیه‌سازی‌شده، بدون اجرای API، دیتابیس یا مصرف اعتبار:

```powershell
dotnet run --project tools/GeminiContractChecks/GeminiContractChecks.csproj
```

این آزمون مسیرهای قصه، بازنویسی، ترجمه و کاور، هدر کلید، نبود کلید در URL، قالب ورودی نقاشی و رفتار شکست کاور را بررسی می‌کند. این ابزار مستقل جایگزین آزمون زنده ارائه‌دهنده نیست.

اجرای API با `dotnet run --project back.csproj` به تنظیم درست SQL Server، JWT و سرویس‌های مورد استفاده نیاز دارد و اثر پایگاه داده دارد. پروژه آزمون اختصاصی در بررسی فعلی پیدا نشد. برای تغییر API، پاسخ، اعتبارسنجی، دسترسی مدیر/عضو و سازگاری مصرف‌کنندگان را در محیط آزمایشی بررسی کنید.

منابع تکمیلی: [اتصال پنل](docs/ADMIN_INTEGRATION.md) و [پایگاه داده Liara](docs/LIARA_DATABASE.md). در اختلاف مستندات با کد، قرارداد فعلی کنترلرها و تنظیمات ملاک است. این سند تأیید موفقیت ساخت یا آزمون یکپارچه نیست.

### جلد رایگان هنگام تأیید قصه

قرارداد جدید ساخت پیش‌نویس coverChoice با مقدار drawing یا ai_free است. مقدار صریح، generateCover قدیمی را غیرفعال می‌کند؛ درخواست رایگان تولید تصویر خودکار ندارد. انتخاب در ستون CoverChoice ذخیره می‌شود. DTO خروجی همین انتخاب را برمی‌گرداند. تأیید مدیر CoverUrl اختیاری می‌پذیرد و برای ai_free تا جایگزینی نقاشی با جلد جدید، انتشار را متوقف می‌کند. کلاینت‌های قدیمی بدون coverChoice همچنان از generateCover استفاده می‌کنند. migration: 20260911072847_StoryDraftCoverChoice.


## ورود پیامکی اعضا
ورود با کد یک‌بارمصرف، ثبت‌نام پس از تأیید شماره و وضعیت تکمیل پروفایل پیاده‌سازی شده است. راهنمای تنظیم و آزمون: [member-otp.md](docs/member-otp.md). اتصال پیامکی تولیدی هنوز فعال نشده است.
