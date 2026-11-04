using System.Runtime.InteropServices;
using System.Text;
using PangYa_Suite_Tools.Logging;
using PangyaAPI.IFF;
using PangyaAPI.UI;

namespace PangYa_Suite_Tools.Shop;

internal sealed record ShopCatalogLoadResult(
    IReadOnlyList<ShopCatalogItem> Items,
    int MissingIconCount);

internal sealed record ShopRegionProbe(
    string? Region,
    IffDocumentInfo? Document);

internal static class ShopCatalogLoader
{
    public static async Task<ShopRegionProbe> ProbeRegionAsync(string iffPath,
        CancellationToken cancellationToken)
    {
        await using IffContainer container = await IffContainer.OpenAsync(iffPath,
            cancellationToken: cancellationToken);

        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IffDocumentInfo? firstDocument = null;
        foreach (IffContainerEntry entry in container.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using Stream stream = await entry.OpenAsync(cancellationToken);
            await using IffReader reader = IffReader.Open(stream,
                Path.GetFileName(entry.Name), new(LeaveOpen: true, FallbackSchemaRegion: container.FileNameRegion));
            firstDocument ??= reader.Info;
            if (NormalizeSchemaRegion(reader.Info.Region) is { } region)
                regions.Add(region);
        }

        if (regions.Count == 1)
            return new ShopRegionProbe(regions.Single(), firstDocument);
        if (regions.Count == 0 && container.FileNameRegion is { } fileNameRegion)
            return new ShopRegionProbe(fileNameRegion, firstDocument);
        return new ShopRegionProbe(null, firstDocument);
    }

    public static async Task<ShopCatalogLoadResult> LoadAsync(string iffPath,
        PangyaFileImageProvider assets, CancellationToken cancellationToken) =>
        await LoadAsync(iffPath, assets, schemaRegion: null, cancellationToken);

    public static async Task<ShopCatalogLoadResult> LoadAsync(string iffPath,
        PangyaFileImageProvider assets, string? schemaRegion,
        CancellationToken cancellationToken)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        var items = new List<ShopCatalogItem>();
        int missingIconCount = 0;
        await using IffContainer container = await IffContainer.OpenAsync(iffPath,
            cancellationToken: cancellationToken);
        foreach (IffContainerEntry entry in container.Entries.OrderBy(entry => entry.Name,
                     StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using Stream stream = await entry.OpenAsync(cancellationToken);
            await using IffReader reader = IffReader.Open(stream,
                Path.GetFileName(entry.Name), new(LeaveOpen: true,
                    SchemaRegion: schemaRegion,
                    FallbackSchemaRegion: container.FileNameRegion));
            IffSchema? schema = reader.Info.Schema;
            if (schema?.BaseReference is not { } baseReference ||
                !baseReference.Name.Equals("Common",
                    StringComparison.OrdinalIgnoreCase))
                continue;
            string[] required =
            [
                "ItemId", "Name", "Icon", "Price", "DiscountPrice",
                "UsedPrice", "IsCash", "ShopFlags", "MoneyFlags",
                "TimeFlag", "Time", "StartDate", "EndDate"
            ];
            if (schema is null || required.Any(name => !schema.Fields.Any(field =>
                    field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
                continue;
            await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken))
            {
                uint itemId =
                    Convert.ToUInt32(record.GetValue("ItemId", encoding));
                string name = Convert.ToString(record.GetValue("Name", encoding))?.Trim()
                              ?? string.Empty;
                string icon = Convert.ToString(record.GetValue("Icon", encoding))?.Trim()
                              ?? string.Empty;
                uint price = Convert.ToUInt32(record.GetValue("Price", encoding));
                uint discount =
                    Convert.ToUInt32(record.GetValue("DiscountPrice", encoding));
                string iconResource = Path.GetFileNameWithoutExtension(icon);
                string? iconPath = string.IsNullOrWhiteSpace(iconResource)
                    ? null
                    : assets.TryResolvePath(iconResource);
                if (iconPath is null)
                    missingIconCount++;
                items.Add(new ShopCatalogItem(Path.GetFileNameWithoutExtension(entry.Name),
                    itemId, name.Length == 0 ? $"0x{itemId:X8}" : name, icon,
                    price, discount,
                    Convert.ToUInt32(record.GetValue("UsedPrice", encoding)),
                    (bool)record.GetValue("IsCash", encoding)!,
                    iconPath ?? string.Empty, entry.Name,
                    record.Index,
                    Convert.ToByte(record.GetValue("ShopFlags", encoding)),
                    Convert.ToByte(record.GetValue("MoneyFlags", encoding)),
                    Convert.ToByte(record.GetValue("TimeFlag", encoding)),
                    Convert.ToByte(record.GetValue("Time", encoding)),
                    record.GetValue("StartDate", encoding) as DateTime?,
                    record.GetValue("EndDate", encoding) as DateTime?));
            }
        }
        return new ShopCatalogLoadResult(items, missingIconCount);
    }

    private static string? NormalizeSchemaRegion(string region)
    {
        if (region == IffHeader.Japan983Region) return region;
        if (region.Equals("TH", StringComparison.OrdinalIgnoreCase)) return "TH";
        if (region.Equals("JP", StringComparison.OrdinalIgnoreCase) ||
            region.StartsWith("Japan", StringComparison.OrdinalIgnoreCase)) return "JP";
        if (region.Equals("Global", StringComparison.OrdinalIgnoreCase) ||
            region.StartsWith("Global_", StringComparison.OrdinalIgnoreCase)) return "Global";
        return null;
    }
}

internal static class ShopCatalogEditor
{
    public static async Task SaveAsync(string iffPath, ShopCatalogItem item,
        string? iconId, uint price, uint discountPrice, uint rentalPrice,
        byte shopFlags, byte moneyFlags, byte timeFlag, byte time,
        DateTime? startDate, DateTime? endDate,
        CancellationToken cancellationToken = default,
        string? schemaRegion = null)
    {
        if (string.IsNullOrWhiteSpace(item.EntryName) || item.RecordIndex < 0)
            throw new InvalidOperationException(
                "The catalog item is not linked to an IFF record.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        await using IffContainer container = await IffContainer.OpenAsync(iffPath,
            cancellationToken: cancellationToken);
        IffContainerEntry entry = container.Entries.Single(candidate =>
            candidate.Name.Equals(item.EntryName, StringComparison.OrdinalIgnoreCase));
        var records = new List<IffRecord>();
        IffHeader header;
        await using (Stream stream = await entry.OpenAsync(cancellationToken))
        await using (IffReader reader = IffReader.Open(stream,
            Path.GetFileName(entry.Name), new(LeaveOpen: true,
                SchemaRegion: schemaRegion,
                FallbackSchemaRegion: container.FileNameRegion)))
        {
            header = reader.Info.Header;
            await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken))
                records.Add(record);
        }
        IffRecord target = records.Single(record =>
            record.Index == item.RecordIndex &&
            Convert.ToUInt32(record.GetValue("ItemId", encoding)) == item.ItemId);
        target.SetValue("Price", price, encoding);
        target.SetValue("DiscountPrice", discountPrice, encoding);
        target.SetValue("UsedPrice", rentalPrice, encoding);
        SetFlagByte(target, "ShopFlags", shopFlags, encoding);
        SetFlagByte(target, "MoneyFlags", moneyFlags, encoding);
        target.SetValue("TimeFlag", timeFlag, encoding);
        target.SetValue("Time", time, encoding);
        target.SetValue("StartDate", startDate, encoding);
        target.SetValue("EndDate", endDate, encoding);
        if (!string.IsNullOrWhiteSpace(iconId))
            target.SetValue("Icon", iconId, encoding);
        await container.SaveEntryAsync(entry.Name, header, records, cancellationToken);
        AppLogger.Instance.Log("Shop",
            $"Saved item 0x{item.ItemId:X8} in '{entry.Name}' to '{iffPath}'.");
    }

    private static void SetFlagByte(IffRecord record, string aggregateName,
        byte value, Encoding encoding)
    {
        IffSchema schema = record.Schema ??
                           throw new InvalidDataException(
                               "The IFF record has no schema.");
        IffField aggregate = schema.Fields.Single(field =>
            field.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase));
        if (aggregate.Type != IffFieldType.Byte || aggregate.Width != 1 ||
            !MemoryMarshal.TryGetArray(record.Bytes,
                out ArraySegment<byte> bytes) || bytes.Array is null)
            throw new InvalidDataException(
                $"The aggregate flag field '{aggregateName}' is not a writable byte.");
        bytes.Array[bytes.Offset + aggregate.Offset] = value;
    }
}
