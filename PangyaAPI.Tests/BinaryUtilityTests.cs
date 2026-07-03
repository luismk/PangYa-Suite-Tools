using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.Tests;

public sealed class BinaryUtilityTests
{
    [Fact]
    public void FixedStringRead_RejectsTruncatedInput()
    {
        using var reader = new PangyaBinaryReader(new MemoryStream([1, 2]), Encoding.ASCII);

        Assert.False(reader.ReadPStr(out string value, 4));
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void StructRead_RoundTripsAndRejectsTruncation()
    {
        byte[] data = [0x44, 0x33, 0x22, 0x11, 0x66, 0x55];
        using (var reader = new PangyaBinaryReader(data))
        {
            SampleStruct value = reader.ReadStruct<SampleStruct>();
            Assert.Equal(0x11223344, value.Number);
            Assert.Equal(0x5566, value.Code);
        }

        using var truncated = new PangyaBinaryReader(data[..^1]);
        Assert.Throws<EndOfStreamException>(() => truncated.ReadStruct<SampleStruct>());
    }

    [Fact]
    public void WriteTime_UsesZeroRecordForNullAndFieldsForValue()
    {
        using var stream = new MemoryStream();
        using (var writer = new PangyaBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            Assert.True(writer.WriteTime(null));
            Assert.True(writer.WriteTime(new DateTime(2026, 7, 2, 12, 34, 56, 789)));
        }

        byte[] data = stream.ToArray();
        Assert.Equal(32, data.Length);
        Assert.All(data[..16], value => Assert.Equal(0, value));
        Assert.Equal(2026, BitConverter.ToUInt16(data, 16));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SampleStruct
    {
        public int Number;
        public short Code;
    }
}
