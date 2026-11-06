using System.Text;

namespace PangyaAPI.IFF;

public sealed class IffRecord
{
    private readonly byte[] _bytes;
    public IffSchema? Schema { get; private set; }
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

    public static IffRecord CreateCopy(int index, ReadOnlyMemory<byte> bytes, IffSchema? schema)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (bytes.Length <= 0) throw new ArgumentException("A copied IFF record must contain data.", nameof(bytes));
        return new IffRecord(index, bytes.ToArray(), schema) { IsDirty = true };
    }

    public object? GetValue(string fieldName, Encoding? stringEncoding = null) =>
        Find(fieldName).GetValue(_bytes, stringEncoding);

    public bool GetBoolean(string fieldName) => GetTyped<bool>(fieldName);
    public byte GetByte(string fieldName) => GetTyped<byte>(fieldName);
    public sbyte GetSByte(string fieldName) => GetTyped<sbyte>(fieldName);
    public ushort GetUInt16(string fieldName) => GetTyped<ushort>(fieldName);
    public short GetInt16(string fieldName) => GetTyped<short>(fieldName);
    public uint GetUInt32(string fieldName) => GetTyped<uint>(fieldName);
    public int GetInt32(string fieldName) => GetTyped<int>(fieldName);
    public ulong GetUInt64(string fieldName) => GetTyped<ulong>(fieldName);
    public long GetInt64(string fieldName) => GetTyped<long>(fieldName);
    public float GetSingle(string fieldName) => GetTyped<float>(fieldName);
    public string GetString(string fieldName, Encoding? stringEncoding = null) =>
        GetTyped<string>(fieldName, stringEncoding);

    public bool TryGetValue(string fieldName, out object? value, Encoding? stringEncoding = null)
    {
        if (!TryGetField(fieldName, out IffField? field) || field is null)
        {
            value = null;
            return false;
        }

        value = field.GetValue(_bytes, stringEncoding);
        return true;
    }

    public bool TryGetField(string fieldName, out IffField? field)
    {
        field = Schema?.Fields.FirstOrDefault(candidate =>
            candidate.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        return field is not null;
    }

    public void SetValue(string fieldName, object? value, Encoding? stringEncoding = null)
    {
        IffField field = Find(fieldName);
        SetValue(field, value, stringEncoding);
    }

    public void SetValue(IffField field, object? value, Encoding? stringEncoding = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        byte[] before = _bytes.AsSpan(field.Offset, field.Width).ToArray();
        field.SetValue(_bytes, value, stringEncoding);
        IsDirty |= !before.AsSpan().SequenceEqual(_bytes.AsSpan(field.Offset, field.Width));
    }

    public void AcceptChanges() => IsDirty = false;

    public void UpdateSchema(IffSchema? schema) => Schema = schema;

    internal void ReplaceBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != _bytes.Length)
            throw new ArgumentException("Patched IFF record data must preserve the target record size.", nameof(bytes));
        if (bytes.SequenceEqual(_bytes)) return;
        bytes.CopyTo(_bytes);
        IsDirty = true;
    }

    private IffField Find(string name) => Schema?.Fields.FirstOrDefault(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"The IFF field '{name}' does not exist.");

    private T GetTyped<T>(string fieldName, Encoding? stringEncoding = null)
    {
        object value = Find(fieldName).GetValue(_bytes, stringEncoding);
        return value is T typed
            ? typed
            : throw new InvalidDataException($"IFF field '{fieldName}' is {value.GetType().Name}, not {typeof(T).Name}.");
    }
}

public sealed record IffDocumentInfo(string FileName, string Region, int RecordSize, IffSchema? Schema, IffHeader Header,
    string? SchemaWarning = null);

public sealed record IffReaderOptions(int MaximumRecordSize = 1024 * 1024, int MaximumRecordCount = ushort.MaxValue,
    bool LeaveOpen = false, IIffSchemaProvider? SchemaProvider = null, string? SchemaRegion = null,
    string? FallbackSchemaRegion = null);
