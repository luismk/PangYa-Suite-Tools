#:project ../PangyaAPI/PangyaAPI.IFF/PangyaAPI.IFF.csproj
#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

// Read-only verification against a caller-selected PSP archive or loose IFF.
// Run: dotnet run --file scripts/verify-psp-iff.cs -- <path>
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PangyaAPI.IFF;

if (args.Length != 1) throw new ArgumentException("Supply the PSP IFF archive or loose table path.");
await using IffContainer container = await IffContainer.OpenAsync(args[0]);
int total = 0;
foreach (IffContainerEntry entry in container.Entries)
{
    await using Stream input = await entry.OpenAsync(default);
    using var original = new MemoryStream();
    await input.CopyToAsync(original);
    await using IffReader reader = IffReader.Open(input, entry.Name,
        new(LeaveOpen: true, FallbackSchemaRegion: container.FileNameRegion));
    if (reader.Info.Region != IffHeader.PspRegion || reader.Info.SchemaWarning is not null)
        throw new InvalidDataException($"{entry.Name}: {reader.Info.Region}: {reader.Info.SchemaWarning}");
    var records = new List<IffRecord>();
    await foreach (IffRecord record in reader.ReadRecordsAsync()) records.Add(record);
    using var output = new MemoryStream();
    await IffWriter.WriteAsync(output, reader.Info.Header, Enumerate(records));
    if (!original.ToArray().AsSpan().SequenceEqual(output.ToArray()))
        throw new InvalidDataException($"{entry.Name}: byte-for-byte round trip failed.");
    Console.WriteLine($"{entry.Name}: {records.Count} x {reader.Info.RecordSize} bytes; identical round trip");
    total += records.Count;
}
IffCatalog catalog = await IffCatalog.LoadAsync(args[0]);
Console.WriteLine($"Verified {catalog.Tables.Count} tables, {total} records; catalog indexed successfully.");

static async IAsyncEnumerable<IffRecord> Enumerate(IEnumerable<IffRecord> records)
{
    foreach (IffRecord record in records) yield return record;
    await Task.CompletedTask;
}
