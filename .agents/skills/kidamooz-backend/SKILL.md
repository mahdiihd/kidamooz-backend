---
name: kidamooz-backend
description: Develop Kidamooz ASP.NET Core endpoints, services, EF Core data changes, and client contracts. Use for changes within Back.
---

# توسعه API کیدآموز

ابتدا [راهنمای پروژه](../../../AGENTS.md) و [مشخصات](../../../spec.md) را بخوانید.

1. کنترلر مربوط در `Controllers/Admin` یا `Controllers/Public` را پیدا کنید و مسیر سرویس، repository و DTO را دنبال کنید.
2. پیش از تغییر قرارداد، مصرف آن را در سرویس‌های پروژه‌های همسایه `Admin` و `android` جست‌وجو کنید. شکل پاسخ، خطا، صفحه‌بندی و دسترسی را با کد مصرف‌کننده تطبیق دهید.
3. وابستگی جدید را با طول عمر مناسب و مطابق وابستگی‌های فعلی در `Program.cs` ثبت کنید؛ قراردادهای لایه موجود را حفظ کنید.
4. برای تغییر داده، مدل، پیکربندی `AppDbContext`، migration و seed مرتبط را با هم بررسی کنید. اعمال migration به پایگاه داده را با تولید فایل migration یکسان ندانید.
5. `dotnet build back.csproj` را اجرا کنید. رفتار endpoint را در محیط آزمایشی با سناریوی مجاز، غیرمجاز، ورودی نامعتبر و حالت مرزی مرتبط بررسی کنید.

شروع برنامه به‌صورت خودکار migration و seed اجرا می‌کند. ساخت پروژه جایگزین بررسی رفتاری نیست و اجرای ابزارهای import/export نیز آزمون بی‌اثر محسوب نمی‌شود. برای اتصال‌های خارجی، از تنظیمات و interfaceهای موجود استفاده کنید و اعتبارنامه را در کد یا گزارش ثبت نکنید.
