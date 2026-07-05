namespace PangyaAPI.IFF;

public readonly record struct IffSchemaCoverageResult(int RepresentedBytes, int UnrepresentedBytes, int RecordSize);

public static class IffSchemaCoverage
{
    public static IffSchemaCoverageResult Calculate(IffSchema schema, int recordSize)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (recordSize <= 0) throw new ArgumentOutOfRangeException(nameof(recordSize));

        byte[] coverage = new byte[recordSize];
        foreach (IffField field in schema.Fields)
        {
            if (IsCatchAllRawRecord(field, recordSize)) continue;
            if (field.Offset < 0 || field.Width <= 0 || field.Offset > recordSize - field.Width)
                throw new InvalidDataException($"Field '{field.Name}' exceeds the {recordSize}-byte record.");

            if (field.Type is IffFieldType.BitField or IffFieldType.BooleanBitField)
            {
                uint mask = field.BitMask
                    ?? throw new InvalidDataException($"Bit field '{field.Name}' does not define a mask.");
                for (int index = 0; index < field.Width; index++)
                    coverage[field.Offset + index] |= (byte)(mask >> (index * 8));
            }
            else
            {
                coverage.AsSpan(field.Offset, field.Width).Fill(byte.MaxValue);
            }
        }

        int represented = coverage.Count(value => value == byte.MaxValue);
        return new IffSchemaCoverageResult(represented, recordSize - represented, recordSize);
    }

    public static bool IsCatchAllRawRecord(IffField field, int recordSize) =>
        field.Type == IffFieldType.Raw && !field.IsEditable && field.Offset == 0 &&
        field.Width == recordSize && field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);
}
