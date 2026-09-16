# Kidamooz Backend Project Specification

This document describes the project as observed in its source code and configuration. It records the current implementation rather than future requirements.

## Role and Technology

This ASP.NET Core service targets .NET 9 and provides the API used by the administration panel and client application. It uses EF Core 9 with SQL Server for persistence and JWT for authentication. Dependencies are declared in `back.csproj`; startup and middleware order are defined in `Program.cs`; service registration lives under `Infrastructure/Startup`.

## Project Structure

- `Controllers/Admin` and `Controllers/Public`: administrative and public/member API entry points.
- `Services`: story, category, catalog, member, child-profile, favorite, contribution, notification, and reporting logic.
- `Repositories` and `Repositories/Interfaces`: data-access implementations and contracts.
- `Domain`, `DTOs`, and `Mapping`: domain models, transport contracts, and mappings.
- `Data/AppDbContext.cs` and `Data/Migrations`: database model and schema migrations.
- `Data/DbInitializer.cs`: startup migrations and seed data.
- `Infrastructure`: authentication, storage, notifications, and AI integrations.
- `Infrastructure/Startup`: registration, error and header handling, and startup callbacks.
- `tools/DbImport` and `tools/DbExport`: independent import/export projects excluded from the web project.

## Integrations and Configuration

The code integrates Liara-compatible S3 storage, Firebase notifications, Gemini story and image generation, and Edge TTS narration. Read configuration from the relevant options classes and `Infrastructure/Startup` files. Never record real credentials in source code or documentation.

Swagger is enabled in development. Application startup applies migrations and seed data, so local execution must use a test database.

## Story and Cover Generation from a Drawing

This section records the implementation observed on `2026-09-10`. Update it together with the code whenever this capability changes.

### Current Flow

1. The app uploads a drawing. The server verifies member identity and the daily quota, then creates a draft with the `generating` status.
2. The server uploads the drawing to media storage and sends the same image bytes to Gemini for story generation.
3. Gemini returns a Persian title and description, Persian story text, and an internal English cover prompt. It does not generate English title or description fields.
4. Narration generation starts after the story is received. Cover generation starts concurrently only when `generateCover=true`; the default flow makes no image-generation request.
5. By default, `CoverUrl` equals `DrawingUrl`, without a duplicate upload. A generated cover is uploaded only after explicit selection. The draft becomes `ready` after its content and URLs are stored.

The flow runs within the create request and does not currently return an immediate background-job identifier. Successful story and cover generation normally use two separate Gemini requests. Audio generation follows an independent path.

### App-to-Backend Request

```http
POST {apiBaseUrl}/api/v1/me/story-drafts
Authorization: Bearer <member-token>
X-Device-Id: <device-id>
Content-Type: multipart/form-data; boundary=<generated-boundary>
```

- The body contains a `drawing` file and an optional boolean `generateCover`. The server default is `false`; newer clients send the value explicitly. Older clients continue to use the original drawing.
- The current request does not send the child's age or name, story style, drawing description, or a custom instruction.
- The controller requires the `member` role. Member and device identifiers are not added to the Gemini request body.
- The request and multipart body limit is `15 * 1024 * 1024` bytes for the complete request.
- Empty files are rejected. Exceeding the daily quota returns `429`. Success returns `201 Created` with `StoryDraftDto`.

Sources: [app draft client](../android/src/app/core/services/story-draft-api.service.ts), [app request headers](../android/src/app/core/services/api.service.ts), [controller](Controllers/Public/StoryDraftsController.cs), and [generation workflow](Services/StoryDraftService.cs).

### Gemini Story Request

```http
POST {BaseUrl}/v1beta/models/{Model}:generateContent
x-goog-api-key: <secret-from-environment>
Content-Type: application/json; charset=utf-8
Accept: application/json
```

```json
{
  "contents": [{
    "parts": [
      { "text": "<story-prompt>" },
      { "inline_data": { "mime_type": "image/jpeg", "data": "<base64-drawing-bytes>" } }
    ]
  }],
  "generationConfig": {
    "temperature": 0.8,
    "responseMimeType": "application/json"
  }
}
```

The MIME type is inferred as JPEG, PNG, or WebP; JPEG is the fallback. Detection does not convert the image. The request sends Base64 bytes rather than the stored URL.

The prompt requires a gentle Persian story for ages 3–8, approximately 180–350 words, without Arabic diacritics, violence, or fear. It requests raw JSON with `titleFa`, `descriptionFa`, `storyScript`, and an English `coverPrompt`. Plain text is required, without HTML, links, or scripts.

Legacy `titleEn` and `descriptionEn` fields remain for compatibility and are populated from Persian text for new generations. Approval-time translation does not call an API. Prompt constraints are instructions to the model rather than independently validated guarantees.

The code parses the first part of the first candidate. Persian title and story must be non-empty. Limits are 300 characters for titles, 2,000 for descriptions, 8,000 for story text, and 1,000 for the cover prompt. If `coverPrompt` is empty, a fallback is derived from the Persian title. The request does not explicitly set `maxOutputTokens`, `responseSchema`, or safety settings.

Source: [GeminiStoryClient.cs](Infrastructure/Ai/GeminiStoryClient.cs).

### Gemini Cover Request

The create flow sends this request only when `generateCover=true`. A missing or false value avoids image-generation cost. Normal drawing selection sets `UsedFallbackCover=false`; failed requested generation sets it to `true`. Cover regeneration is also an explicit image request.

```http
POST {BaseUrl}/v1beta/models/{CoverImageModel}:generateContent
x-goog-api-key: <secret-from-environment>
Content-Type: application/json; charset=utf-8
Accept: application/json
```

The request asks for `TEXT` and `IMAGE` response modalities and uses this prompt:

```text
Create one children's book cover illustration.
Style: colorful, joyful, soft lighting, gentle characters, picture-book look.
Absolutely no text, letters, watermark, logo, or signature in the image.
Subject: {coverPrompt.Trim()}
```

The original drawing and complete story are not sent to the cover model. The final cover is connected to the drawing only through `coverPrompt`. Image size, aspect ratio, quality, temperature, and token limit are unspecified. Image bytes are extracted from `inlineData` or `inline_data`. Naming the stored file `.jpg` and assigning `image/jpeg` does not convert the returned bytes.

Source: [GeminiCoverImageGenerator.cs](Infrastructure/Ai/GeminiCoverImageGenerator.cs).

### Models, Settings, and Precedence

The default integration uses the dedicated Gemini route provided by [1xAi](https://1xai.ir/docs). The API key is sent through `x-goog-api-key` and never in the URL.

Configure a fresh 1xAi key through `GEMINI_API_KEY` or `Gemini__ApiKey`. Previously exposed keys must not be reused. The correct base URL is `https://1xai.ir/gemini`, without `/v1` or `/gemini/v1beta`.

Deployment observations from `2026-09-10` showed successful standalone text generation with `gemini-flash-latest` and image generation with `gemini-2.5-flash-image`. The observed durations, approximately 3.2 and 5.7 seconds, are not performance guarantees and do not replace an end-to-end member flow test.

- Current story model in `appsettings.json`: `gemini-2.0-flash`.
- Settings and client fallback: `gemini-flash-latest`.
- Default cover model: `gemini-2.5-flash-image`.
- Default base URL: `https://1xai.ir/gemini`.
- Docker Compose defaults may differ from local configuration.

Explicit configuration precedence is:

```text
ApiKey:          Gemini__ApiKey           -> GEMINI_API_KEY           -> configured value
Model:           Gemini__Model            -> GEMINI_MODEL             -> configured value
CoverImageModel: Gemini__CoverImageModel  -> GEMINI_COVER_IMAGE_MODEL -> configured value
BaseUrl:         Gemini__BaseUrl          -> GEMINI_BASE_URL          -> configured value
```

The first non-null value wins, with additional empty-value handling in clients. `deploy/ai-proxy` remains a legacy Google alternative and is not used by the default integration.

Sources: [default settings](Infrastructure/Ai/GeminiSettings.cs), [external-service registration](Infrastructure/Startup/ExternalServiceExtensions.cs), [deployment configuration](../deploy/docker-compose.yml), and [legacy proxy](../deploy/ai-proxy/worker.js).

### Usage, Timeouts, and Failure Behavior

Every valid Gemini response emits a `GeminiUsage` console event. `Operation` distinguishes `story`, `rewrite`, and `cover`. Token counters come directly from `usageMetadata`; totals are not recalculated. When available, `TraceId` correlates stages. Missing metadata is represented by `UsageAvailable=false`, not zero. Provider billing remains authoritative for requests that fail before a response.

```bash
docker logs --since 1h kidamooz-api 2>&1 | grep GeminiUsage
```

Usage is stored in container logs rather than the admin panel or database. Retention follows Docker configuration. Story text, drawings, API keys, and raw provider error bodies are excluded.

- The named `gemini` HTTP client has a two-minute timeout and sends `User-Agent: Kidamooz/1.0`.
- Cover generation during draft creation also has an 18-second cancellation timeout.
- Failed or empty requested cover generation falls back to the original drawing and sets `UsedFallbackCover=true`.
- Story-generation failure fails creation; non-cancellation errors set the draft to `failed`.
- Audio-generation failure may allow a `ready` draft with an empty audio URL.
- Gemini calls have no explicit retry loop. Rewrite and cover regeneration are separate operations.

Source: [StoryDraftService.cs](Services/StoryDraftService.cs).

## Build and Verification

```powershell
dotnet build back.csproj
dotnet run --project tools/GeminiContractChecks/GeminiContractChecks.csproj
```

The mocked contract checks cover story, rewrite, translation, and cover requests; API-key headers; absence of keys in URLs; drawing input shape; and cover fallback. They do not replace live provider testing.

Running `dotnet run --project back.csproj` requires valid SQL Server, JWT, and external-service configuration and has database effects. For API changes, verify responses, validation, administrator/member authorization, and client compatibility in a test environment.

Additional references: [admin integration](docs/ADMIN_INTEGRATION.md) and [Liara database](docs/LIARA_DATABASE.md). When documentation and code differ, current controller contracts and configuration are authoritative.

### Free Cover During Story Approval

The draft contract supports `coverChoice` values of `drawing` or `ai_free`. An explicit value disables legacy `generateCover` behavior; a free-cover request does not automatically generate an image. The choice is stored in `CoverChoice` and returned by the DTO. Administrator approval accepts an optional `CoverUrl`; for `ai_free`, publication remains blocked until the drawing is replaced with a new cover. Older clients without `coverChoice` continue to use `generateCover`. Migration: `20260911072847_StoryDraftCoverChoice`.

## Member SMS Authentication

One-time-password login, registration after phone verification, and profile-completion status are implemented. See [member-otp.md](docs/member-otp.md) for configuration and verification. The production SMS integration is not yet enabled.
