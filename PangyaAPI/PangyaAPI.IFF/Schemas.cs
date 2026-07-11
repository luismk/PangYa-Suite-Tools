using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace PangyaAPI.IFF;

public enum IffFieldType { Boolean, Byte, UInt16, Int16, UInt32, Int32, Single, BitField, BooleanBitField, ZeroBoolean, FixedString, LongString, DateTime, Raw, ByteRangeBoolean, ItemIdReference, Icon, Sound }

public sealed record IffField(
    string Name, int Offset, int Width, IffFieldType Type, bool IsEditable = true,
    Encoding? Encoding = null, long? Minimum = null, long? Maximum = null,
    uint? BitMask = null, int BitShift = 0, bool IsVisible = true,
    IffFieldReference? Reference = null,
    string? IconPath = null,
    string? SoundPath = null)
{
    public object GetValue(ReadOnlySpan<byte> record, Encoding? stringEncoding = null)
    {
        EnsureRange(record.Length);
        ReadOnlySpan<byte> value = record.Slice(Offset, Width);
        return Type switch
        {
            IffFieldType.Boolean => value[0] != 0,
            IffFieldType.Byte => value[0],
            IffFieldType.UInt16 => BinaryPrimitives.ReadUInt16LittleEndian(value),
            IffFieldType.Int16 => BinaryPrimitives.ReadInt16LittleEndian(value),
            IffFieldType.UInt32 or IffFieldType.ItemIdReference => BinaryPrimitives.ReadUInt32LittleEndian(value),
            IffFieldType.Int32 => BinaryPrimitives.ReadInt32LittleEndian(value),
            IffFieldType.Single => BinaryPrimitives.ReadSingleLittleEndian(value),
            IffFieldType.BitField => ReadBitField(value),
            IffFieldType.BooleanBitField => ReadBooleanBitField(value),
            IffFieldType.ZeroBoolean => ReadUnsigned(value) == 0,
            IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.Sound => DecodeString(value, stringEncoding),
            IffFieldType.DateTime => DecodeDate(value),
            IffFieldType.ByteRangeBoolean => value.IndexOfAnyExcept((byte)0) >= 0,
            _ => Convert.ToHexString(value)
        };
    }

    public void SetValue(Span<byte> record, object? input, Encoding? stringEncoding = null)
    {
        if (!IsEditable) throw new InvalidOperationException($"Field '{Name}' is read-only.");
        EnsureRange(record.Length);
        Span<byte> target = record.Slice(Offset, Width);
        switch (Type)
        {
            case IffFieldType.Boolean: target[0] = Convert.ToBoolean(input, CultureInfo.InvariantCulture) ? (byte)1 : (byte)0; break;
            case IffFieldType.Byte: target[0] = checked((byte)CheckedInteger(input)); break;
            case IffFieldType.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(target, checked((ushort)CheckedInteger(input))); break;
            case IffFieldType.Int16: BinaryPrimitives.WriteInt16LittleEndian(target, checked((short)CheckedInteger(input))); break;
            case IffFieldType.UInt32:
            case IffFieldType.ItemIdReference:
                BinaryPrimitives.WriteUInt32LittleEndian(target, checked((uint)CheckedInteger(input)));
                break;
            case IffFieldType.Int32: BinaryPrimitives.WriteInt32LittleEndian(target, checked((int)CheckedInteger(input))); break;
            case IffFieldType.Single: BinaryPrimitives.WriteSingleLittleEndian(target, Convert.ToSingle(input, CultureInfo.InvariantCulture)); break;
            case IffFieldType.BitField: WriteBitField(target, input); break;
            case IffFieldType.BooleanBitField: WriteBooleanBitField(target, input); break;
            case IffFieldType.ZeroBoolean: throw new InvalidOperationException($"Derived field '{Name}' is read-only.");
            case IffFieldType.FixedString:
            case IffFieldType.LongString:
            case IffFieldType.Icon:
            case IffFieldType.Sound:
                EncodeString(target, Convert.ToString(input, CultureInfo.InvariantCulture) ?? string.Empty, stringEncoding);
                break;
            case IffFieldType.DateTime: EncodeDate(target, input); break;
            case IffFieldType.Raw: WriteRaw(target, input); break;
            case IffFieldType.ByteRangeBoolean:
                target.Clear();
                if (Convert.ToBoolean(input, CultureInfo.InvariantCulture)) target[0] = 1;
                break;
            default: throw new InvalidOperationException($"Field '{Name}' cannot be edited.");
        }
    }

    private void WriteRaw(Span<byte> target, object? input)
    {
        string text = (Convert.ToString(input, CultureInfo.InvariantCulture) ?? string.Empty)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        if (text.Length != target.Length * 2)
            throw new ArgumentException($"'{Name}' requires exactly {target.Length * 2} hexadecimal characters.", nameof(input));
        try
        {
            Convert.FromHexString(text).CopyTo(target);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"'{Name}' must contain hexadecimal bytes.", nameof(input), ex);
        }
    }

    private uint ReadBitField(ReadOnlySpan<byte> value)
    {
        ValidateBitField(value.Length);
        return (ReadUnsigned(value) & BitMask!.Value) >> BitShift;
    }

    private bool ReadBooleanBitField(ReadOnlySpan<byte> value)
    {
        ValidateBitField(value.Length, requireSingleBit: true);
        return (ReadUnsigned(value) & BitMask!.Value) != 0;
    }

    private void WriteBitField(Span<byte> target, object? input)
    {
        ValidateBitField(target.Length);
        uint mask = BitMask!.Value;
        uint maximum = mask >> BitShift;
        long converted = CheckedInteger(input);
        if (converted < 0 || (ulong)converted > maximum)
            throw new ArgumentOutOfRangeException(Name, converted, $"'{Name}' must be between 0 and {maximum}.");
        uint current = ReadUnsigned(target);
        uint replacement = ((uint)converted << BitShift) & mask;
        WriteUnsigned(target, (current & ~mask) | replacement);
    }

    private void WriteBooleanBitField(Span<byte> target, object? input)
    {
        ValidateBitField(target.Length, requireSingleBit: true);
        uint mask = BitMask!.Value;
        uint current = ReadUnsigned(target);
        uint replacement = Convert.ToBoolean(input, CultureInfo.InvariantCulture) ? current | mask : current & ~mask;
        WriteUnsigned(target, replacement);
    }

    private void ValidateBitField(int width, bool requireSingleBit = false)
    {
        uint widthMask = BitFieldWidthMask(width);
        if (widthMask == 0 || BitMask is not uint mask || mask == 0 ||
            (mask & ~widthMask) != 0 || BitShift is < 0 or > 31 || (mask >> BitShift) == 0 ||
            requireSingleBit && (mask & (mask - 1)) != 0)
            throw new InvalidDataException($"Field '{Name}' has an invalid bit-field definition.");
    }

    private static uint BitFieldWidthMask(int width) => width switch
    {
        1 => byte.MaxValue,
        2 => ushort.MaxValue,
        3 => 0x00FF_FFFFu,
        4 => uint.MaxValue,
        _ => 0
    };

    private static uint ReadUnsigned(ReadOnlySpan<byte> value)
    {
        if (value.Length is < 1 or > 4)
            throw new InvalidDataException("Bit fields must occupy one to four bytes.");
        uint result = 0;
        for (int index = 0; index < value.Length; index++)
            result |= (uint)value[index] << (index * 8);
        return result;
    }

    private static void WriteUnsigned(Span<byte> target, uint value)
    {
        if (target.Length is < 1 or > 4)
            throw new InvalidDataException("Bit fields must occupy one to four bytes.");
        for (int index = 0; index < target.Length; index++)
            target[index] = (byte)(value >> (index * 8));
    }

    private long CheckedInteger(object? input)
    {
        long value = Convert.ToInt64(input, CultureInfo.InvariantCulture);
        if (Minimum is long min && value < min || Maximum is long max && value > max)
            throw new ArgumentOutOfRangeException(Name, value, $"Value is outside the allowed range for '{Name}'.");
        return value;
    }

    private string DecodeString(ReadOnlySpan<byte> bytes, Encoding? stringEncoding)
    {
        int zero = bytes.IndexOf((byte)0);
        return (stringEncoding ?? Encoding ?? System.Text.Encoding.Latin1).GetString(zero < 0 ? bytes : bytes[..zero]);
    }

    private void EncodeString(Span<byte> destination, string value, Encoding? stringEncoding)
    {
        Encoding encoding = stringEncoding ?? Encoding ?? System.Text.Encoding.Latin1;
        int byteCount = encoding.GetByteCount(value);
        if (byteCount >= destination.Length)
            throw new ArgumentException($"'{Name}' must fit in {destination.Length - 1} encoded bytes.", nameof(value));
        destination.Clear();
        encoding.GetBytes(value, destination);
    }

    private static object DecodeDate(ReadOnlySpan<byte> value)
    {
        if (value.Length != 16) return Convert.ToHexString(value);
        ushort year = BinaryPrimitives.ReadUInt16LittleEndian(value);
        if (year == 0) return string.Empty;
        try
        {
            return new DateTime(year, BinaryPrimitives.ReadUInt16LittleEndian(value[2..]),
                BinaryPrimitives.ReadUInt16LittleEndian(value[6..]), BinaryPrimitives.ReadUInt16LittleEndian(value[8..]),
                BinaryPrimitives.ReadUInt16LittleEndian(value[10..]), BinaryPrimitives.ReadUInt16LittleEndian(value[12..]),
                BinaryPrimitives.ReadUInt16LittleEndian(value[14..]));
        }
        catch (ArgumentOutOfRangeException) { return Convert.ToHexString(value); }
    }

    private static void EncodeDate(Span<byte> value, object? input)
    {
        if (value.Length != 16) throw new InvalidDataException("PangYa dates occupy 16 bytes.");
        if (input is null || string.IsNullOrWhiteSpace(Convert.ToString(input, CultureInfo.InvariantCulture)))
        {
            value.Clear();
            return;
        }
        DateTime date = input is DateTime typed ? typed : DateTime.Parse(Convert.ToString(input, CultureInfo.InvariantCulture)!, CultureInfo.CurrentCulture);
        value.Clear();
        ushort[] parts = [(ushort)date.Year, (ushort)date.Month, (ushort)date.DayOfWeek, (ushort)date.Day,
            (ushort)date.Hour, (ushort)date.Minute, (ushort)date.Second, (ushort)date.Millisecond];
        for (int i = 0; i < parts.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(value[(i * 2)..], parts[i]);
    }

    private void EnsureRange(int recordLength)
    {
        if (Offset < 0 || Width <= 0 || Offset > recordLength - Width)
            throw new InvalidDataException($"Field '{Name}' exceeds the {recordLength}-byte record.");
    }
}

public sealed record IffFieldReference(
    string TargetFile,
    string TargetKeyField = "ItemId",
    string DisplayField = "Name",
    string IconField = "Icon",
    bool? PickerEnabled = null);

public sealed record IffSchema(
    string Name,
    int MinimumRecordSize,
    IReadOnlyList<IffField> Fields,
    bool IsEditable = true,
    int DefaultStringSize = 32,
    IffSchemaUiDefinition? Ui = null,
    int DefaultLongStringSize = 512);
