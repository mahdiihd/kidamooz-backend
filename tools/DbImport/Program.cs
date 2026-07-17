using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;

var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ConnectionStrings__Default")
      ?? throw new InvalidOperationException("Connection string required");

var inDir = args.Length > 1 ? args[1] : "/tmp/kidamooz-export";
if (!Directory.Exists(inDir))
    throw new DirectoryNotFoundException(inDir);

var manifestPath = Path.Combine(inDir, "manifest.json");
if (!File.Exists(manifestPath))
    throw new FileNotFoundException("manifest.json missing", manifestPath);

var manifest = JsonSerializer.Deserialize<Dictionary<string, int>>(
    await File.ReadAllTextAsync(manifestPath))
    ?? throw new InvalidOperationException("Invalid manifest");

await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();
Console.WriteLine($"Connected to {conn.DataSource}/{conn.Database}");

await using (var disable = new SqlCommand("""
    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
    """, conn))
{
    await disable.ExecuteNonQueryAsync();
}

var ordered = OrderTables(manifest.Keys.ToList());
foreach (var table in ordered)
{
    var safeName = table.Replace('.', '_');
    var path = Path.Combine(inDir, safeName + ".json");
    if (!File.Exists(path))
    {
        Console.WriteLine($"Skip missing {table}");
        continue;
    }

    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));
    if (doc.RootElement.ValueKind != JsonValueKind.Array)
        continue;

    await TruncateTable(conn, table);

    var imported = 0;
    foreach (var row in doc.RootElement.EnumerateArray())
    {
        if (row.ValueKind != JsonValueKind.Object)
            continue;
        await InsertRow(conn, table, row);
        imported++;
    }

    Console.WriteLine($"Imported {table}: {imported} rows (manifest {manifest[table]})");
}

await using (var enable = new SqlCommand("""
    EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
    """, conn))
{
    await enable.ExecuteNonQueryAsync();
}

Console.WriteLine("Done.");

static List<string> OrderTables(List<string> tables)
{
    var priority = new[]
    {
        "dbo.__EFMigrationsHistory",
        "dbo.Admins",
        "dbo.Categories",
        "dbo.Stories",
        "dbo.StoryCategories",
        "dbo.Devices",
        "dbo.Users",
        "dbo.UserDevices",
        "dbo.ListeningProgress",
        "dbo.Favorites",
        "dbo.AuditLogs",
        "dbo.Notifications",
        "dbo.RefreshTokens",
    };

    var ordered = new List<string>();
    foreach (var p in priority)
    {
        var match = tables.FirstOrDefault(t => string.Equals(t, p, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            ordered.Add(match);
    }

    foreach (var t in tables.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
    {
        if (!ordered.Contains(t, StringComparer.OrdinalIgnoreCase))
            ordered.Add(t);
    }

    return ordered;
}

static async Task TruncateTable(SqlConnection conn, string table)
{
    try
    {
        await using var cmd = new SqlCommand($"DELETE FROM {table}", conn);
        await cmd.ExecuteNonQueryAsync();
    }
    catch (SqlException ex)
    {
        Console.WriteLine($"DELETE {table} warning: {ex.Message}");
    }
}

static async Task InsertRow(SqlConnection conn, string table, JsonElement row)
{
    var cols = new List<string>();
    var vals = new List<object?>();
    foreach (var prop in row.EnumerateObject())
    {
        cols.Add($"[{prop.Name}]");
        vals.Add(JsonToDb(prop.Value));
    }

    if (cols.Count == 0)
        return;

    var colList = string.Join(", ", cols);
    var paramList = string.Join(", ", cols.Select((_, i) => $"@p{i}"));
    var sql = $"INSERT INTO {table} ({colList}) VALUES ({paramList})";

    await using var cmd = new SqlCommand(sql, conn);
    for (var i = 0; i < vals.Count; i++)
        cmd.Parameters.AddWithValue($"@p{i}", vals[i] ?? DBNull.Value);

    await cmd.ExecuteNonQueryAsync();
}

static object? JsonToDb(JsonElement el) => el.ValueKind switch
{
    JsonValueKind.Null => null,
    JsonValueKind.True => true,
    JsonValueKind.False => false,
    JsonValueKind.Number => el.TryGetInt64(out var l) ? l
        : el.TryGetDecimal(out var d) ? d
        : el.GetDouble(),
    JsonValueKind.String => ParseString(el.GetString()!),
    _ => el.GetRawText()
};

static object ParseString(string s)
{
    if (Guid.TryParse(s, out var g))
        return g;
    if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        return dto.UtcDateTime;
    if (LooksLikeBase64(s))
    {
        try { return Convert.FromBase64String(s); }
        catch { /* not bytes */ }
    }
    return s;
}

static bool LooksLikeBase64(string s) =>
    s.Length >= 16 && s.Length % 4 == 0 && s.All(c =>
        char.IsLetterOrDigit(c) || c is '+' or '/' or '=');
