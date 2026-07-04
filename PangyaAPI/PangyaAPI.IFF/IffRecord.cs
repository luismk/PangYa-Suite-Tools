using System.Text;

namespace PangyaAPI.IFF;

public sealed class IffRecord
{
    private readonly byte[] _bytes;
    public IffSchema? Schema { get; }
    public int Index { get; }
    public bool IsDirty { get; private set; }
    public ReadOnlyMemory<byte> Bytes => _bytes;

    internal IffRecord(int index, byte[] bytes, IffSchema? schema) => (Index, _bytes, Schema) = (index, bytes, schema);

    public static IffRecord CreateBlank(int index, int recordSize, IffSchema? schema)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (recordSize <= 0) throw new ArgumentOutOfRangeException(nameof(recordSize));
        return new IffRecord(index, new byte[recordSize], schema) { IsDirty = true };
    }

    public object? GetValue(string fieldName, Encoding? stringEncoding = null) =>
        Find(fieldName).GetValue(_bytes, stringEncoding);

    public void SetValue(string fieldName, object? value, Encoding? stringEncoding = null)
    {
        IffField field = Find(fieldName);
        byte[] before = _bytes.AsSpan(field.Offset, field.Width).ToArray();
        field.SetValue(_bytes, value, stringEncoding);
        IsDirty |= !before.AsSpan().SequenceEqual(_bytes.AsSpan(field.Offset, field.Width));
    }

    public void AcceptChanges() => IsDirty = false;

    private IffField Find(string name) => Schema?.Fields.FirstOrDefault(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"The IFF field '{name}' does not exist.");
}

public sealed record IffDocumentInfo(string FileName, string Region, int RecordSize, IffSchema? Schema, IffHeader Header);

public sealed record IffReaderOptions(int MaximumRecordSize = 1024 * 1024, ushort MaximumRecordCount = ushort.MaxValue, bool LeaveOpen = false);
