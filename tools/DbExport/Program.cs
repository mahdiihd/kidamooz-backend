using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ConnectionStrings__Default")
      ?? throw new InvalidOperationException("Connection string required");

var outDir = args.Length > 1 ? args[1] : "/tmp/kidamooz-export";
Directory.CreateDirectory(outDir);

await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();
Console.WriteLine($"Connected to {conn.Database}");

var tables = new List<string>();
await using (var cmd = new SqlCommand("""
    SELECT TABLE_SCHEMA + '.' + TABLE_NAME
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'BASE TABLE'
    ORDER BY TABLE_SCHEMA, TABLE_NAME
    """, conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
        tables.Add(reader.GetString(0));
}

Console.WriteLine($"Tables: {tables.Count}");
var manifest = new Dictionary<string, int>();

foreach (var table in tables)
{
    var safeName = table.Replace('.', '_');
    var path = Path.Combine(outDir, safeName + ".json");
    var rows = new List<Dictionary<string, object?>>();

    await using var cmd = new SqlCommand($"SELECT * FROM {table}", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (reader.IsDBNull(i))
            {
                row[name] = null;
                continue;
            }

            var value = reader.GetValue(i);
            row[name] = value switch
            {
                DateTime dt => dt.ToString("o"),
                DateTimeOffset dto => dto.ToString("o"),
                byte[] bytes => Convert.ToBase64String(bytes),
                Guid g => g.ToString(),
                _ => value
            };
        }
        rows.Add(row);
    }

    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions
    {
        WriteIndented = false
    }));
    manifest[table] = rows.Count;
    Console.WriteLine($"Exported {table}: {rows.Count} rows");
}

await File.WriteAllTextAsync(
    Path.Combine(outDir, "manifest.json"),
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Done. Output: {outDir}");
