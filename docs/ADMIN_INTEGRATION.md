# اتصال پنل ادمین به بکند Kidamooz

این سند راهنمای عملی اتصال پروژه Angular ادمین به API بکند .NET است.

| پروژه | مسیر |
|-------|------|
| بکند | `D:\Projects\Kidamooz\Back` |
| پنل ادمین | `D:\Projects\Kidamooz\Admin` |

---

## پیش‌نیازها

- .NET 9 SDK
- SQL Server (روی `localhost`)
- Node.js + Angular CLI (برای پنل ادمین)
- دیتابیس `Kidamooz` با migration اعمال‌شده

---

## ۱. راه‌اندازی بکند

```powershell
cd D:\Projects\Kidamooz\Back

# اعمال migration (ساخت جداول)
dotnet ef database update

# اجرای API
dotnet run
```

| مورد | آدرس |
|------|------|
| API | `http://localhost:5042` |
| Swagger | `http://localhost:5042/swagger` |
| Admin API Base | `http://localhost:5042/api/v1/admin` |

### ادمین پیش‌فرض (seed اولین اجرا)

| فیلد | مقدار |
|------|-------|
| Email | `admin@kidamooz.com` |
| Password | `admin123` |

### ساخت ادمین دستی (CLI)

```powershell
dotnet run -- create-admin --email admin@kidamooz.com --password "YourPass123" --name "مدیر سیستم" --role admin
```

---

## ۲. تنظیم پنل ادمین

فایل `Admin/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5042/api/v1/admin',
  useMock: false,
  mobileAppDeepLink: 'kidamooz://story',
};
```

تغییرات ضروری:

1. `apiBaseUrl` → آدرس بکند (پورت `5042`)
2. `useMock: false` → فعال‌سازی درخواست‌های واقعی HTTP

اجرای پنل:

```powershell
cd D:\Projects\Kidamooz\Admin
npm install
ng serve
```

پنل: `http://localhost:4200`

---

## ۳. CORS

بکند در `appsettings.json` اجازه دسترسی از پورت Angular را دارد:

```json
"Cors": {
  "AdminOrigins": ["http://localhost:4200"]
}
```

اگر پنل روی پورت دیگری اجرا می‌شود، origin را اضافه کنید.

---

## ۴. احراز هویت (JWT)

### فلو لاگین

```text
1. POST /api/v1/admin/auth/login  →  { accessToken, refreshToken }
2. پنل توکن را در localStorage ذخیره می‌کند
3. auth.interceptor.ts هدر Authorization را به همه درخواست‌ها اضافه می‌کند
```

### Endpointها

| Method | Path | Auth |
|--------|------|------|
| POST | `/auth/login` | خیر |
| POST | `/auth/refresh` | خیر |
| POST | `/auth/logout` | اختیاری |

### نمونه لاگین

```http
POST http://localhost:5042/api/v1/admin/auth/login
Content-Type: application/json

{
  "email": "admin@kidamooz.com",
  "password": "admin123"
}
```

پاسخ:

```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "..."
}
```

### تست در Swagger

1. `POST /api/v1/admin/auth/login` را اجرا کنید
2. `accessToken` را کپی کنید
3. دکمه **Authorize** → `Bearer <token>`

---

## ۵. نگاشت سرویس‌های Angular به API

| سرویس Angular | Endpoint بکند | Mock |
|---------------|---------------|------|
| `AuthService.login` | `POST /auth/login` | `useMock` |
| `AuthService.logout` | `POST /auth/logout` | `useMock` |
| `CatalogService.getVersion` | `GET /catalog/version` | `useMock` |
| `CatalogService.getDashboardStats` | `GET /dashboard` | `useMock` |
| `CatalogService.rebuildVersion` | `POST /catalog/rebuild-version` | `useMock` |
| `CategoryService.*` | `/categories` | `useMock` |
| `StoryService.*` | `/stories` | `useMock` |
| `AudienceService.getSegments` | `GET /audience/segments` | mock data |
| `AudienceService.getUsers` | `GET /audience/users` | mock data |
| `MediaService.*` | `/media/upload-url`, `/media/confirm` | `useMock` |
| `AuditLogService.getAll` | `GET /audit-logs` | `useMock` |
| `AdminUserService.*` | `/users` (GET/POST/PUT password/DELETE) | — |

---

## ۶. قرارداد JSON

### LocalizedText

```json
{ "fa": "عنوان فارسی", "en": "English title" }
```

### StoryAccess

```json
{
  "visibility": "public",
  "audience": {
    "segmentIds": [],
    "userIds": []
  }
}
```

### پاسخ لیست قصه

```json
{
  "items": [ /* Story[] */ ],
  "total": 10
}
```

### Audit Log

فیلد زمان در پاسخ: `timestamp` (نه `createdAt`)

---

## ۷. آپلود رسانه (Liara)

فایل‌ها روی **Liara Object Storage** ذخیره می‌شوند؛ فقط URL در SQL Server نگه‌داری می‌شود.

### فلو

```text
1. POST /media/upload-url
   Body: { fileName, contentType, mediaType }
   mediaType: cover | audio | icon

2. PUT مستقیم به uploadUrl (از فرانت، بدون عبور از API)

3. POST /media/confirm
   Body: { publicUrl, mediaType }

4. publicUrl در story/category ذخیره شود
```

### تنظیم Liara در بکند

`Back/appsettings.json`:

```json
"Liara": {
  "EndpointUrl": "https://storage.iran.liara.site",
  "AccessKey": "YOUR_ACCESS_KEY",
  "SecretKey": "YOUR_SECRET_KEY",
  "BucketName": "kidamooz-media",
  "PublicBaseUrl": "https://kidamooz-media.storage.iran.liara.site"
}
```

### CORS باکت (الزامی برای آپلود از مرورگر)

برای PUT مستقیم از پنل ادمین، روی باکت Liara قانون CORS تنظیم کنید:

```json
{
  "CORSRules": [
    {
      "AllowedOrigins": [
        "http://localhost:4200",
        "https://kidamooz-front.liara.run"
      ],
      "AllowedMethods": ["GET", "PUT", "HEAD"],
      "AllowedHeaders": ["*"],
      "ExposeHeaders": ["ETag"],
      "MaxAgeSeconds": 3600
    }
  ]
}
```

```powershell
aws s3api put-bucket-cors --bucket kid --cors-configuration file://cors.json --endpoint-url https://storage.c2.liara.site
```

> presigned URL بدون `ContentType` در امضا ساخته می‌شود تا مرورگر بتواند PUT بزند؛ نوع فایل قبل از صدور URL در بکند اعتبارسنجی می‌شود.

---

## ۸. مدیریت کاربران ادمین (API)

فقط نقش `admin` با JWT معتبر:

| Method | Path | توضیح |
|--------|------|-------|
| GET | `/users` | لیست ادمین‌ها |
| POST | `/users` | ساخت ادمین |
| PUT | `/users/{id}/password` | تغییر رمز |
| DELETE | `/users/{id}` | حذف ادمین |

```http
POST /api/v1/admin/users
Authorization: Bearer <token>

{
  "email": "editor@kidamooz.com",
  "password": "SecurePass123",
  "displayName": "ویرایشگر",
  "role": "editor"
}
```

```http
DELETE /api/v1/admin/users/{id}
Authorization: Bearer <token>
```

نقش‌ها: `admin` | `editor`

محدودیت حذف:
- نمی‌توانید حساب خودتان را حذف کنید
- حداقل یک ادمین باید باقی بماند

---

## ۹. چک‌لیست یکپارچگی

- [ ] SQL Server روشن و `dotnet ef database update` اجرا شده
- [ ] بکند روی `http://localhost:5042` در حال اجراست
- [ ] `environment.ts` → `useMock: false`
- [ ] `apiBaseUrl` به پورت درست اشاره می‌کند
- [ ] CORS شامل `http://localhost:4200` است
- [ ] لاگین با `admin@kidamooz.com` موفق است
- [ ] داشبورد آمار برمی‌گرداند
- [ ] CRUD دسته‌بندی و قصه کار می‌کند
- [ ] آپلود فایل (بعد از تنظیم Liara) کار می‌کند

---

## ۱۰. عیب‌یابی

### `address already in use` (پورت 5042)

```powershell
Get-NetTCPConnection -LocalPort 5042 | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

### CORS error در مرورگر

- origin پنل را در `Cors:AdminOrigins` اضافه کنید
- بکند را restart کنید

### 401 Unauthorized

- توکن منقضی شده → دوباره login
- `Authorization: Bearer <token>` در درخواست‌ها

### اتصال SQL Server

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=Kidamooz;Trusted_Connection=True;TrustServerCertificate=True"
}
```

با SQL Auth:

```json
"Default": "Server=localhost;Database=Kidamooz;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

### پنل هنوز mock است

`useMock` باید `false` باشد. سرویس‌ها شرط `environment.useMock` دارند.

---

## ۱۱. Production (لیارا)

### دیتابیس SQL Server لیارا (شبکه خصوصی)

| مورد | مقدار |
|------|-------|
| هاست | `kidamooz` (فقط داخل شبکه خصوصی لیارا) |
| پورت | `1433` |
| کاربر | `sa` |
| دیتابیس | `myDB` |
| نسخه | SQL Server 2022-CU11 |

جزئیات کامل: [`LIARA_DATABASE.md`](./LIARA_DATABASE.md)

| محیط | Connection String |
|------|-------------------|
| Development | `Server=localhost;Database=Kidamooz;Trusted_Connection=True;TrustServerCertificate=True` |
| Production | `Server=kidamooz,1433;Database=myDB;User Id=sa;Password=***;Encrypt=False;TrustServerCertificate=True` |

**بکند باید روی لیارا deploy شود** تا به هاست `kidamooz` دسترسی داشته باشد. از لوکال وصل نمی‌شود.

تنظیمات production در `appsettings.Production.json` است (این فایل در `.gitignore` قرار دارد).

روی سرور لیارا:

```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
dotnet run
```

یا migration خودکار با اولین اجرای API (`DbInitializer` → `MigrateAsync`).

تست اتصال **فقط از داخل شبکه لیارا**:

```bash
sqlcmd -S kidamooz,1433 -Usa -P<PASSWORD> -C -Q "SELECT DB_NAME()"
```

از لوکال به هاست `kidamooz` وصل نمی‌شود — طبیعی است.

### متغیر محیطی (پیشنهادی برای deploy)

به‌جای ذخیره رمز در فایل، در پنل لیارا:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=kidamooz,1433;Database=myDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=True
```

### API و CORS

| محیط | apiBaseUrl |
|------|------------|
| Dev | `http://localhost:5042/api/v1/admin` |
| Prod | `https://kidamooz-back.liara.run/api/v1/admin` |

**پنل ادمین (Angular):**

| فایل | محیط |
|------|------|
| `Admin/src/environments/environment.ts` | Development — `localhost:5042` |
| `Admin/src/environments/environment.production.ts` | Production — `kidamooz-back.liara.run` |

Build پروداکشن پنل:

```powershell
cd D:\Projects\Kidamooz\Admin
ng build --configuration production
```

- `Jwt:Secret` را در production عوض کنید
- Liara Object Storage credentials را از env variable بخوانید
- HTTPS فعال باشد
- `Cors:AdminOrigins` را به دامنه(های) واقعی پنل ادمین محدود کنید

متغیر محیطی CORS روی لیارا (چند origin):

```text
Cors__AdminOrigins__0=https://admin.kidamooz.com
Cors__AdminOrigins__1=http://localhost:4200
```

---

## فایل‌های مرجع

| فایل | موضوع |
|------|--------|
| `Admin/src/environments/environment.ts` | base URL و mock |
| `Admin/src/app/core/interceptors/auth.interceptor.ts` | JWT header |
| `Admin/src/app/core/services/*.service.ts` | قرارداد HTTP |
| `Back/Controllers/Admin/*.cs` | endpointهای ادمین |
| `Back/docs/../Admin/docs/BACKEND_DOTNET.md` | قرارداد کامل بکند |
