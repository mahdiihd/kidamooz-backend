# دیتابیس SQL Server — لیارا (شبکه خصوصی)

## مشخصات سرویس

| مورد | مقدار |
|------|-------|
| هاست (شبکه خصوصی) | `kidamooz` |
| پورت (شبکه خصوصی) | `1433` |
| نام کاربری | `sa` |
| نسخه | SQL Server 2022-CU11 (Ubuntu 20.04) |
| نام دیتابیس | `myDB` |

## Connection String (Production)

```text
Server=kidamooz,1433;Database=myDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30
```

در پروژه: `appsettings.Production.json` (فایل gitignore — رمز واقعی اینجاست)

## نکته حیاتی — شبکه خصوصی

هاست `kidamooz` **فقط داخل شبکه خصوصی لیارا** resolve می‌شود.

```text
┌─────────────────┐     شبکه خصوصی     ┌──────────────────┐
│  Backend API    │ ──────────────────▶ │  SQL Server      │
│  (روی لیارا)    │   kidamooz:1433     │  myDB            │
└─────────────────┘                     └──────────────────┘
```

- بکند باید روی **لیارا** deploy شود و به همان شبکه خصوصی دیتابیس وصل باشد
- از لوکال (کامپیوتر خودتان) به `kidamooz` وصل **نمی‌شود**
- Development لوکال همچنان از `localhost` استفاده می‌کند

## تنظیم در پنل لیارا

متغیرهای محیطی اپ بکند:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=kidamooz,1433;Database=myDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30
```

اپ بکند و دیتابیس SQL باید در **یک شبکه خصوصی مشترک** لیارا باشند.

## Migration (ساخت جداول)

### روش ۱ — خودکار (پیشنهادی)

بکند را روی لیارا deploy کنید (متصل به شبکه خصوصی `kidamooz`). با اولین اجرا:

```text
DbInitializer → MigrateAsync() → ساخت جداول + seed
```

### روش ۲ — دستی با SQL Script

از لوکال به دیتابیس وصل نمی‌شود. فایل SQL آماده:

```text
Back/scripts/liara-initial-migration.sql
```

در **کنسول/ترمینال دیتابیس لیارا** (داخل پنل SQL Server) اجرا کنید:

```bash
sqlcmd -S kidamooz,1433 -Usa -P<PASSWORD> -C -d myDB -i liara-initial-migration.sql
```

### روش ۳ — EF از داخل لیارا

در shell اپ بکند روی لیارا:

```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
```

## تست اتصال (فقط روی لیارا)

```bash
sqlcmd -S kidamooz,1433 -Usa -P<PASSWORD> -C -Q "SELECT @@VERSION"
sqlcmd -S kidamooz,1433 -Usa -P<PASSWORD> -C -d myDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
```

## محیط Development (لوکال)

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=Kidamooz;Trusted_Connection=True;TrustServerCertificate=True"
}
```

```powershell
dotnet ef database update
dotnet run
```

## جداول

پس از migration این جداول ساخته می‌شوند:

`users`, `refresh_tokens`, `catalog_meta`, `categories`, `stories`, `story_chapters`, `audience_segments`, `app_users`, `story_audience_segments`, `story_audience_users`, `story_views_daily`, `audit_logs`

## امنیت

- `appsettings.Production.json` در `.gitignore` است
- رمز را در پنل لیارا به‌صورت env variable نگه دارید
- نمونه بدون رمز: `appsettings.Production.example.json`
