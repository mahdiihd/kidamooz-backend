# Faraz pattern OTP

The optional `FarazOtpSender` uses POST `https://api.iranpayamak.com/ws/v1/sms/pattern`.
OTP generation remains six digits with the existing expiry, hashing and rate limits.

Configure server environment variables only:

```dotenv
Otp__Provider=faraz
Faraz__ApiKey=REPLACE_WITH_PRIVATE_KEY
Faraz__PatternCode=ueOjayM8zI
Faraz__CodeParameter=code
Faraz__LineNumber=90008361
```

The user confirmed approval of pattern `ueOjayM8zI` on 2026-09-16. It replaces
the previously rejected pattern `lkzzwvIk4h`. The approved message is:

```text
کد ورود به کیدینگو @kidingo.ir #code
```

The Android app uses the SMS User Consent API so locally signed APKs and the
store-signed build can read the same OTP after the user approves Android's
one-time prompt. The pattern can keep the production signing hash on its own line:

```text
کد ورود به کیدینگو: #code
148jxVFK2Cz
```

Faraz may append `لغو11` after this text. The User Consent path detects the
six-digit OTP without depending on the hash or requesting broad SMS permissions.
Android asks the user to share that single message, then the app fills and submits
the code. The hash remains useful if a future release restores fully automatic SMS
Retriever behavior; it belongs to package `com.kidamooz.app` and one signing
certificate only.

Keep `Faraz__CodeParameter=code`; the provider substitutes this attribute into
`#code`. Apply the new pattern code to the server environment before restarting
the API. Updating this document alone does not change a running server.

Provider acceptance does not establish handset delivery. Use the provider delivery
report and a consented recipient to validate live delivery after approval.

Verification: `dotnet run --project tools/MemberAuthChecks/MemberAuthChecks.csproj`
uses a fake HTTP handler and sends no SMS. It checks the pattern payload, six-digit
requirement, HTTP/semantic failures, malformed responses and error redaction.
