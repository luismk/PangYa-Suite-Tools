using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace PangyaAPI.IFF;

public enum IffFieldType { Boolean, Byte, UInt16, Int16, UInt32, Int32, BitField, BooleanBitField, ZeroBoolean, FixedString, DateTime, Raw }

public sealed record IffField(
    string Name, int Offset, int Width, IffFieldType Type, bool IsEditable = true,
    Encoding? Encoding = null, long? Minimum = null, long? Maximum = null,
    uint? BitMask = null, int BitShift = 0)
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
            IffFieldType.UInt32 => BinaryPrimitives.ReadUInt32LittleEndian(value),
            IffFieldType.Int32 => BinaryPrimitives.ReadInt32LittleEndian(value),
            IffFieldType.BitField => ReadBitField(value),
            IffFieldType.BooleanBitField => ReadBooleanBitField(value),
            IffFieldType.ZeroBoolean => ReadUnsigned(value) == 0,
            IffFieldType.FixedString => DecodeString(value, stringEncoding),
            IffFieldType.DateTime => DecodeDate(value),
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
            case IffFieldType.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(target, checked((uint)CheckedInteger(input))); break;
            case IffFieldType.Int32: BinaryPrimitives.WriteInt32LittleEndian(target, checked((int)CheckedInteger(input))); break;
            case IffFieldType.BitField: WriteBitField(target, input); break;
            case IffFieldType.BooleanBitField: WriteBooleanBitField(target, input); break;
            case IffFieldType.ZeroBoolean: throw new InvalidOperationException($"Derived field '{Name}' is read-only.");
            case IffFieldType.FixedString: EncodeString(target, Convert.ToString(input, CultureInfo.InvariantCulture) ?? string.Empty, stringEncoding); break;
            case IffFieldType.DateTime: EncodeDate(target, input); break;
            default: throw new InvalidOperationException($"Raw field '{Name}' cannot be edited.");
        }
    }

    private uint ReadBitField(ReadOnlySpan<byte> value)
    {
        ValidateBitField(value.Length);
        return (BinaryPrimitives.ReadUInt32LittleEndian(value) & BitMask!.Value) >> BitShift;
    }

    private bool ReadBooleanBitField(ReadOnlySpan<byte> value)
    {
        ValidateBitField(value.Length, requireSingleBit: true);
        return (ReadUnsigned(value) & BitMask!.Value) != 0;
    }

    private void WriteBitField(Span<byte> target, object? input)
    {
        ValidateBitField(target.Length, requireUInt32: true);
        uint mask = BitMask!.Value;
        uint maximum = mask >> BitShift;
        long converted = CheckedInteger(input);
        if (converted < 0 || (ulong)converted > maximum)
            throw new ArgumentOutOfRangeException(Name, converted, $"'{Name}' must be between 0 and {maximum}.");
        uint current = BinaryPrimitives.ReadUInt32LittleEndian(target);
        uint replacement = ((uint)converted << BitShift) & mask;
        BinaryPrimitives.WriteUInt32LittleEndian(target, (current & ~mask) | replacement);
    }

    private void WriteBooleanBitField(Span<byte> target, object? input)
    {
        ValidateBitField(target.Length, requireSingleBit: true);
        uint mask = BitMask!.Value;
        uint current = ReadUnsigned(target);
        uint replacement = Convert.ToBoolean(input, CultureInfo.InvariantCulture) ? current | mask : current & ~mask;
        WriteUnsigned(target, replacement);
    }

    private void ValidateBitField(int width, bool requireSingleBit = false, bool requireUInt32 = false)
    {
        uint widthMask = width switch { 1 => byte.MaxValue, 2 => ushort.MaxValue, 4 => uint.MaxValue, _ => 0 };
        if (widthMask == 0 || requireUInt32 && width != sizeof(uint) || BitMask is not uint mask || mask == 0 ||
            (mask & ~widthMask) != 0 || BitShift is < 0 or > 31 || (mask >> BitShift) == 0 ||
            requireSingleBit && (mask & (mask - 1)) != 0)
            throw new InvalidDataException($"Field '{Name}' has an invalid bit-field definition.");
    }

    private static uint ReadUnsigned(ReadOnlySpan<byte> value) => value.Length switch
    {
        1 => value[0],
        2 => BinaryPrimitives.ReadUInt16LittleEndian(value),
        4 => BinaryPrimitives.ReadUInt32LittleEndian(value),
        _ => throw new InvalidDataException("Bit fields must occupy one, two, or four bytes.")
    };

    private static void WriteUnsigned(Span<byte> target, uint value)
    {
        switch (target.Length)
        {
            case 1: target[0] = checked((byte)value); break;
            case 2: BinaryPrimitives.WriteUInt16LittleEndian(target, checked((ushort)value)); break;
            case 4: BinaryPrimitives.WriteUInt32LittleEndian(target, value); break;
            default: throw new InvalidDataException("Bit fields must occupy one, two, or four bytes.");
        }
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

public sealed record IffSchema(string Name, int MinimumRecordSize, IReadOnlyList<IffField> Fields, bool IsEditable = true);

public static class IffSchemaRegistry
{
    private static readonly Encoding Latin = Encoding.Latin1;
    private static readonly Dictionary<string, int> MinimumThailandSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AuxPart.iff"] = 176, ["Ball.iff"] = 764, ["Caddie.iff"] = 200, ["CaddieItem.iff"] = 236,
        ["CadieMagicBox.iff"] = 104, ["CadieMagicBoxRandom.iff"] = 16, ["Card.iff"] = 328,
        ["Character.iff"] = 372, ["Club.iff"] = 196, ["ClubSet.iff"] = 179, ["Course.iff"] = 312,
        ["CutinInfomation.iff"] = 208, ["Desc.iff"] = 516, ["Enchant.iff"] = 16,
        ["Furniture.iff"] = 464, ["FurnitureAbility.iff"] = 60, ["HairStyle.iff"] = 148,
        ["Item.iff"] = 196, ["Mascot.iff"] = 256, ["Match.iff"] = 332, ["OfflineShop.iff"] = 200,
        ["Part.iff"] = 512, ["QuestDrop.iff"] = 244, ["SetItem.iff"] = 220, ["Skin.iff"] = 208,
        ["TikiPointTable.iff"] = 48, ["TikiRecipe.iff"] = 52, ["TikiSpecialTable.iff"] = 60
    };

    private static readonly HashSet<string> BaseSchemas = new(StringComparer.OrdinalIgnoreCase)
    {
        "AuxPart.iff", "Ball.iff", "Caddie.iff", "CaddieItem.iff", "Card.iff", "Character.iff", "Club.iff", "ClubSet.iff",
        "Course.iff", "Furniture.iff", "HairStyle.iff", "Item.iff", "Mascot.iff", "OfflineShop.iff", "Part.iff",
        "QuestDrop.iff", "SetItem.iff", "Skin.iff"
    };

    public static IffSchema? Resolve(string fileName, IffHeader header, int recordSize)
    {
        fileName = Path.GetFileName(fileName);
        if (!MinimumThailandSizes.TryGetValue(fileName, out int minimum)) return null;
        int delta = header.Region == "JP" && BaseSchemas.Contains(fileName) ? 48 : 0;
        if (recordSize < minimum + delta) return null;

        var fields = new List<IffField>();
        if (BaseSchemas.Contains(fileName))
        {
            fields.AddRange(BaseFields(header.Region));
            fields.AddRange(KnownExtensionFields(fileName, header.Region));
        }
        else
        {
            fields.AddRange(KnownStandaloneFields(fileName, header.Region));
            if (fields.Count == 0 && recordSize >= 4) fields.Add(new IffField("TypeId", 0, 4, IffFieldType.UInt32));
        }
        fields.Add(new IffField("Raw record", 0, recordSize, IffFieldType.Raw, false));
        return new IffSchema(Path.GetFileNameWithoutExtension(fileName), minimum + delta, fields);
    }

    private static IEnumerable<IffField> KnownExtensionFields(string fileName, string region)
    {
        // gens uses conflicting hard-coded offsets for JP extension fields. Keep those
        // bytes opaque until a JP fixture confirms the layout rather than risking edits.
        if (region != "TH") yield break;
        const int text = 40;
        int start = 64 + text * 2;
        if (fileName.Equals("Item.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("SecondaryIcon", start, 40, IffFieldType.FixedString, Encoding: Latin);
            for (int i = 0; i < 6; i++) yield return new IffField(i == 0 ? "Amount" : $"Unknown{i + 32}", start + 40 + i * 2, 2, IffFieldType.UInt16);
        }
        else if (fileName.Equals("Skin.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("SecondaryIcon", start, text, IffFieldType.FixedString, Encoding: Latin);
            string[] names = ["Amount", "Unknown35", "Unknown39", "Unknown43", "Unknown47", "Unknown49"];
            for (int i = 0; i < names.Length; i++) yield return new IffField(names[i], start + text + i * 4, 4, IffFieldType.Int32);
        }
        else if (fileName.Equals("Card.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("Rarity", start, 1, IffFieldType.Byte);
            yield return new IffField("CardImage", start + 1, text, IffFieldType.FixedString, Encoding: Latin);
            string[] stats = ["Power", "Control", "Accuracy", "Spin", "Curve", "BonusType", "BonusAmount"];
            int statsOffset = start + 1 + text + 1;
            for (int i = 0; i < stats.Length; i++) yield return new IffField(stats[i], statsOffset + i * 2, 2, IffFieldType.Int16);
            int images = statsOffset + stats.Length * 2;
            yield return new IffField("SubIcon", images, text, IffFieldType.FixedString, Encoding: Latin);
            yield return new IffField("SlotImage", images + text, text, IffFieldType.FixedString, Encoding: Latin);
            yield return new IffField("BuffImage", images + text * 2, text, IffFieldType.FixedString, Encoding: Latin);
            foreach (IffField field in UInt16Fields(images + text * 3,
                "BonusTime", "CardPack", "CardNumber", "Unknown43")) yield return field;
        }
        else if (fileName.Equals("Character.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("Model", start, text, IffFieldType.FixedString, Encoding: Latin);
            yield return new IffField("Texture", start + text, text, IffFieldType.FixedString, Encoding: Latin);
            yield return new IffField("SecondaryIcon", start + text * 2, text, IffFieldType.FixedString, Encoding: Latin);
            yield return new IffField("ShopImage", start + text * 3, text, IffFieldType.FixedString, Encoding: Latin);
            foreach (IffField field in UInt16Fields(start + text * 4,
                "InitialPower", "InitialControl", "InitialAccuracy", "InitialSpin", "InitialCurve")) yield return field;
            foreach (IffField field in ByteFields(start + text * 4 + 10,
                "Unknown37", "Unknown38", "Unknown39", "Unknown43", "Unknown44", "Unknown45")) yield return field;
            yield return new IffField("Unknown46", start + text * 4 + 16, 4, IffFieldType.UInt32);
            foreach (IffField field in ByteFields(start + text * 4 + 20,
                "BasePower", "BaseControl", "BaseAccuracy", "BaseSpin", "BaseCurve")) yield return field;
            yield return new IffField("Unknown52", start + text * 4 + 25, 4, IffFieldType.UInt32);
            yield return new IffField("Unknown53", start + text * 4 + 29, 1, IffFieldType.Byte);
            foreach (IffField field in UInt16Fields(start + text * 4 + 30,
                Enumerable.Range(55, 19).Select(i => $"Unknown{i}").ToArray())) yield return field;
        }
        else if (fileName.Equals("AuxPart.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt16Fields(start,
                "Amount", "Unknown33", "Unknown34", "Unknown35", "Unknown36")) yield return field;
            foreach (IffField field in ByteFields(start + 10,
                "Power", "Control", "Accuracy", "Spin", "Curve", "PowerSlot", "ControlSlot", "AccuracySlot", "SpinSlot", "CurveSlot")) yield return field;
            foreach (IffField field in UInt16Fields(start + 20,
                "DriveUp", "ItemDropUp", "ComboUp", "PangUp", "ExperienceUp", "Unknown47")) yield return field;
        }
        else if (fileName.Equals("Ball.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("SecondaryIcon", start, text);
            foreach (IffField field in UInt16Fields(start + text, "Unknown14", "Unknown33", "Unknown34", "Unknown35")) yield return field;
            int graphics = start + text + 8;
            for (int i = 0; i < 14; i++) yield return TextField($"Graphic{i + 1}", graphics + i * text, text);
            foreach (IffField field in UInt16Fields(graphics + 14 * text,
                "Unknown36", "Unknown37", "Unknown38", "Unknown39", "Unknown40", "Unknown41")) yield return field;
        }
        else if (fileName.Equals("Caddie.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("Salary", start, 4, IffFieldType.UInt32);
            yield return TextField("SecondaryIcon", start + 4, text);
            foreach (IffField field in UInt16Fields(start + 4 + text,
                "Power", "Control", "Accuracy", "Spin", "Curve", "Unknown39")) yield return field;
        }
        else if (fileName.Equals("CaddieItem.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("Graphic1", start, 40);
            yield return TextField("Graphic2", start + 40, 40);
            foreach (IffField field in UInt16Fields(start + 80,
                "Price1Day", "Unknown33", "Price7Days", "Price30Days", "Amount", "Unknown37")) yield return field;
        }
        else if (fileName.Equals("Club.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("SecondaryIcon", start, 40);
            foreach (IffField field in UInt16Fields(start + 40,
                "ClubNumber", "Unknown33", "Unknown34", "Unknown35", "Unknown36", "Unknown37")) yield return field;
        }
        else if (fileName.Equals("ClubSet.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(start, "Wood", "Iron", "Wedge", "Putter")) yield return field;
            string[] stats = ["InitialPower", "InitialControl", "InitialAccuracy", "InitialSpin", "InitialCurve",
                "MaximumPower", "MaximumControl", "MaximumAccuracy", "MaximumSpin", "MaximumCurve"];
            for (int i = 0; i < stats.Length; i++) yield return new IffField(stats[i], start + 16 + i * 2, 1, IffFieldType.Byte);
        }
        else if (fileName.Equals("Course.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("EnglishName", start, 40);
            yield return TextField("ThaiName", start + 40, 40);
            yield return new IffField("CourseFlags", start + 80, 1, IffFieldType.Byte);
            yield return TextField("XmlFile", start + 81, 40);
            yield return new IffField("Unknown34", start + 121, 2, IffFieldType.UInt16);
            yield return new IffField("Unknown35", start + 123, 1, IffFieldType.Byte);
            yield return new IffField("Unknown36", start + 124, 4, IffFieldType.UInt32);
            yield return TextField("SequenceFile", start + 128, 40);
        }
        else if (fileName.Equals("Furniture.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("SecondaryIcon", start, text);
            yield return new IffField("Amount", start + 40, 2, IffFieldType.UInt16);
            yield return new IffField("Unknown33", start + 42, 1, IffFieldType.Byte);
            yield return new IffField("Unknown60", start + 43, 1, IffFieldType.Byte);
            foreach (IffField field in UInt16Fields(start + 44, "Unknown34", "Unknown35", "Unknown36", "Unknown37")) yield return field;
            foreach (IffField field in ByteFields(start + 52, Enumerable.Range(38, 16).Select(i => $"Unknown{i}").ToArray())) yield return field;
            for (int i = 0; i < 6; i++) yield return TextField($"Sprite{i + 3}", start + 68 + i * 40, 40);
            foreach (IffField field in UInt16Fields(start + 308,
                "Unknown54", "Unknown55", "Unknown56", "Unknown57", "Unknown58", "Unknown59")) yield return field;
        }
        else if (fileName.Equals("HairStyle.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in ByteFields(start, "ColorId", "CharacterNumber", "Unknown35", "Unknown36")) yield return field;
        }
        else if (fileName.Equals("Mascot.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("SecondaryIcon", start, text);
            yield return TextField("TertiaryIcon", start + 40, text);
            yield return new IffField("Price1Day", start + 80, 2, IffFieldType.UInt16);
            yield return new IffField("Price15Days", start + 82, 4, IffFieldType.UInt32);
            yield return new IffField("Price30Days", start + 86, 4, IffFieldType.UInt32);
            foreach (IffField field in UInt16Fields(start + 90,
                "Unknown37", "Unknown38", "Unknown39", "Unknown40", "Unknown41", "Unknown42")) yield return field;
            foreach (IffField field in ByteFields(start + 102, Enumerable.Range(43, 10).Select(i => $"Unknown{i}").ToArray())) yield return field;
        }
        else if (fileName.Equals("OfflineShop.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(start, "ReferenceCount", "Reference1", "Reference2", "Reference3", "Reference4",
                "Reference5", "Reference6", "Reference7", "Reference8", "Reference9", "Reference10", "Reference11", "Reference12")) yield return field;
            yield return new IffField("Reference13 (ambiguous Java mapping)", start + 50, 6, IffFieldType.Raw, false);
        }
        else if (fileName.Equals("Part.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return TextField("SecondaryIcon", start, text);
            foreach (IffField field in ByteFields(start + 40, "PartCategory", "Stat2", "Stat3", "Stat4", "Stat5", "Stat6",
                "Stat7", "Stat8", "Stat9", "Stat10", "Stat11", "Stat12")) yield return field;
            for (int i = 0; i < 6; i++) yield return TextField($"Sprite{i + 3}", start + 52 + i * 40, 40);
            foreach (IffField field in UInt16Fields(start + 292, "PowerUp", "ControlUp", "AccuracyUp", "SpinUp", "CurveUp",
                "PowerSlotUp", "ControlSlotUp", "AccuracySlotUp", "SpinSlotUp", "CurveSlotUp")) yield return field;
            yield return TextField("UnknownString1", start + 312, 40);
            yield return new IffField("EquipWith1", start + 352, 4, IffFieldType.UInt32);
            yield return new IffField("EquipWith2", start + 356, 4, IffFieldType.UInt32);
            foreach (IffField field in UInt16Fields(start + 360, "Unknown67", "Unknown68", "Unknown69", "Unknown70")) yield return field;
        }
        else if (fileName.Equals("QuestDrop.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt16Fields(start, Enumerable.Range(20, 13).Select(i => $"Unknown{i}")
                .Concat(["Unknown44", "Unknown45", "Unknown46"]).ToArray())) yield return field;
            yield return TextField("SecondaryIcon", start + 32, 40);
            foreach (IffField field in UInt16Fields(start + 72, "Amount", "Unknown33", "Unknown34", "Unknown35", "Unknown36",
                "Unknown37", "Unknown38", "Unknown39", "Unknown40", "Unknown41", "Unknown42")) yield return field;
            yield return new IffField("Unknown43", start + 94, 1, IffFieldType.Byte);
            yield return new IffField("Unknown47", start + 95, 1, IffFieldType.Byte);
            foreach (IffField field in UInt16Fields(start + 96, "Unknown48", "Unknown49")) yield return field;
        }
        else if (fileName.Equals("SetItem.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(start, Enumerable.Range(0, 11).Select(i => i == 0 ? "ItemCount" : $"Item{i}").ToArray())) yield return field;
            foreach (IffField field in UInt16Fields(start + 44, Enumerable.Range(1, 10).Select(i => $"Item{i}Count")
                .Concat(Enumerable.Range(42, 6).Select(i => $"Unknown{i}")).ToArray())) yield return field;
        }
    }

    private static IEnumerable<IffField> KnownStandaloneFields(string fileName, string region)
    {
        if (fileName.Equals("CadieMagicBox.iff", StringComparison.OrdinalIgnoreCase))
        {
            string[] names = ["Index", "Valid", "ShowOnPage", "Unknown4", "RequiredLevel", "ProductItem", "ProductCount",
                "Item1", "Item2", "Item3", "Item4", "Item1Count", "Item2Count", "Item3Count", "Item4Count"];
            foreach (IffField field in UInt32Fields(0, names)) yield return field;
            foreach (IffField field in UInt32Fields(60, Enumerable.Range(16, 11).Select(i => $"Unknown{i}").ToArray())) yield return field;
        }
        else if (fileName.Equals("CadieMagicBoxRandom.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(0, "Item1Count", "Item1", "Item2Count", "Item2")) yield return field;
        }
        else if (fileName.Equals("CutinInfomation.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(0, "Valid", "ItemId", "Unknown3", "Unknown4", "Unknown5", "Unknown6", "CharacterId")) yield return field;
            yield return TextField("CharacterPicture", 28, 40);
            yield return new IffField("CharacterPictureLayer", 68, 4, IffFieldType.UInt32);
            yield return TextField("BackgroundPicture", 72, 40);
            yield return new IffField("BackgroundLayer", 112, 4, IffFieldType.UInt32);
            yield return TextField("OverlayPicture", 116, 40);
            yield return new IffField("OverlayLayer", 156, 4, IffFieldType.UInt32);
            yield return TextField("UnknownPicture", 160, 40);
            yield return new IffField("Unknown11", 200, 4, IffFieldType.UInt32);
            yield return new IffField("Unknown12", 204, 4, IffFieldType.UInt32);
        }
        else if (fileName.Equals("Desc.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("ItemId", 0, 4, IffFieldType.UInt32);
            yield return TextField("Description", 4, 512);
        }
        else if (fileName.Equals("Enchant.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(0, "Valid", "EnchantId", "Price", "Unknown1")) yield return field;
        }
        else if (fileName.Equals("FurnitureAbility.iff", StringComparison.OrdinalIgnoreCase))
        {
            foreach (IffField field in UInt32Fields(0, "Valid", "ItemId", "Number1", "Number2", "Number3", "Number4", "Number5")) yield return field;
            foreach (IffField field in UInt16Fields(28, "Year", "Month", "DayOfWeek", "Day", "Hour", "Minute", "Second", "Millisecond")) yield return field;
            foreach (IffField field in ByteFields(44, Enumerable.Range(6, 16).Select(i => $"Number{i}").ToArray())) yield return field;
        }
        else if (fileName.Equals("Match.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("Valid", 0, 1, IffFieldType.Boolean);
            yield return new IffField("ItemId", 4, 4, IffFieldType.UInt32);
            yield return TextField("Name", 8, 80);
            yield return new IffField("RequiredLevel", 88, 1, IffFieldType.Byte);
            if (region == "TH")
            {
                for (int i = 0; i < 6; i++) yield return TextField(i == 0 ? "Icon" : $"Icon{i + 1}", 89 + i * 40, 40);
                foreach (IffField field in ByteFields(329, "Unknown2", "Unknown3", "Unknown4")) yield return field;
            }
        }
        else if (fileName.Equals("TikiPointTable.iff", StringComparison.OrdinalIgnoreCase) ||
                 fileName.Equals("TikiRecipe.iff", StringComparison.OrdinalIgnoreCase) ||
                 fileName.Equals("TikiSpecialTable.iff", StringComparison.OrdinalIgnoreCase))
        {
            yield return new IffField("Id", 0, 4, IffFieldType.UInt32);
            yield return new IffField("ItemId", 4, 4, IffFieldType.UInt32);
            yield return new IffField("RequiredLevel", 8, 1, IffFieldType.Byte);
            int byteCount = fileName.StartsWith("TikiPoint", StringComparison.OrdinalIgnoreCase) ? 39 :
                fileName.StartsWith("TikiRecipe", StringComparison.OrdinalIgnoreCase) ? 43 : 51;
            foreach (IffField field in ByteFields(9, Enumerable.Range(2, byteCount).Select(i => $"Unknown{i}").ToArray())) yield return field;
        }
    }

    private static IffField TextField(string name, int offset, int width) =>
        new(name, offset, width, IffFieldType.FixedString, Encoding: Latin);

    private static IEnumerable<IffField> UInt32Fields(int offset, params string[] names)
    {
        for (int i = 0; i < names.Length; i++) yield return new IffField(names[i], offset + i * 4, 4, IffFieldType.UInt32);
    }

    private static IEnumerable<IffField> UInt16Fields(int offset, params string[] names)
    {
        for (int i = 0; i < names.Length; i++) yield return new IffField(names[i], offset + i * 2, 2, IffFieldType.UInt16);
    }

    private static IEnumerable<IffField> ByteFields(int offset, params string[] names)
    {
        for (int i = 0; i < names.Length; i++) yield return new IffField(names[i], offset + i, 1, IffFieldType.Byte);
    }

    private static IEnumerable<IffField> BaseFields(string region)
    {
        int text = region == "JP" ? 64 : 40;
        int level = 8 + text;
        int icon = level + 1;
        int prices = icon + text + 3;
        yield return new IffField("Enabled", 0, 1, IffFieldType.Boolean);
        yield return new IffField("ItemId", 4, 4, IffFieldType.UInt32);
        yield return new IffField("IFF Type", 4, 4, IffFieldType.BitField, BitMask: 0xFC000000u, BitShift: 26);
        yield return new IffField("Character Serial", 4, 4, IffFieldType.BitField, BitMask: 0x03FC0000u, BitShift: 18);
        yield return new IffField("Position", 4, 4, IffFieldType.BitField, BitMask: 0x0003E000u, BitShift: 13);
        yield return new IffField("Group", 4, 4, IffFieldType.BitField, BitMask: 0x00001800u, BitShift: 11);
        yield return new IffField("Type", 4, 4, IffFieldType.BitField, BitMask: 0x00000600u, BitShift: 9);
        yield return new IffField("Serial", 4, 4, IffFieldType.BitField, BitMask: 0x000001FFu);
        yield return new IffField("Name", 8, text, IffFieldType.FixedString, Encoding: Latin);
        yield return new IffField("LevelFlags", level, 1, IffFieldType.Byte, false);
        yield return new IffField("Icon", icon, text, IffFieldType.FixedString, Encoding: Latin);
        yield return new IffField("Price", prices, 4, IffFieldType.UInt32);
        yield return new IffField("DiscountPrice", prices + 4, 4, IffFieldType.UInt32);
        yield return new IffField("UsedPrice", prices + 8, 4, IffFieldType.UInt32);
        yield return new IffField("MoneyFlags", prices + 12, 1, IffFieldType.Byte, false);
        yield return new IffField("Cookies", prices + 12, 1, IffFieldType.BooleanBitField, BitMask: 0x01);
        yield return new IffField("Pang", prices + 12, 1, IffFieldType.BooleanBitField, BitMask: 0x02);
        yield return new IffField("Free", prices + 12, 1, IffFieldType.ZeroBoolean, false);
        yield return new IffField("ShopFlags", prices + 13, 1, IffFieldType.Byte, false);
        yield return new IffField("InStock", prices + 13, 1, IffFieldType.BooleanBitField, BitMask: 0x01);
        yield return new IffField("DisableGift", prices + 13, 1, IffFieldType.BooleanBitField, BitMask: 0x02);
        yield return new IffField("ShowSpecial", prices + 13, 1, IffFieldType.BooleanBitField, BitMask: 0x04);
        yield return new IffField("ShowNew", prices + 13, 1, IffFieldType.BooleanBitField, BitMask: 0x08);
        yield return new IffField("ShowHot", prices + 13, 1, IffFieldType.BooleanBitField, BitMask: 0x10);
        yield return new IffField("TimeFlag", prices + 14, 1, IffFieldType.Byte);
        yield return new IffField("Time", prices + 15, 1, IffFieldType.Byte);
        yield return new IffField("Point", prices + 16, 4, IffFieldType.UInt32);
        yield return new IffField("StartDate", prices + 20, 16, IffFieldType.DateTime);
        yield return new IffField("EndDate", prices + 36, 16, IffFieldType.DateTime);
    }
}
