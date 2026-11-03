using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace PangyaAPI.Tests;

public sealed class LzPakCompatibilityTests
{
    private const string SuperSsLargeLz77Level3 =
        "AHXNJUuE4uryAKaBIGdDNLJuAEvimVRzdn/xAMx1mY0eq87bAJc5ZW7KmMNxAAtu+omkOy8UAG47GkfFFCTKAPYX6NyCOhocAOYKFofJQaM+AK+7oCiW87CKAC1PGB9x02RNAPEVhYH/dnvbAJQIy5miqDwDACUdrbwrY//AAJ9eV2qZmEJLAHHdecDpwUxwABhv9j35SWI9AMY8HtN3xor4ABwffUng++ZuAPHSNu6VsI6JAC3t8J30WA1AAK21UwySi2PKAEG7E3JySJgxAK9KjndeC+MnANKl9nlAulswALsXExyZVT8hAO/PiS78fsrmAK6QuNmj0JSfAEoxKCYMCYwJAJPohs2s+ng8AEtsMFyuUQyvALLiT6fFMI2ZAB2aiooLkwWZAJ6fOvrwhQyuAL8ZPm1AKheFAEdyU4gtj10UABVgAyF1WlCBAAiqIJWONp1eAPfNzmfpI8Q1ALpnKDZEhzhhAUDx";

    private const string SuperSsLargeLz772Level3 =
        "yHXNJUuE4uryyKaBIGdDNLJuyEvimVRzdn/xyMx1mY0eq87byJc5ZW7KmMNxyAtu+omkOy8UyG47GkfFFCTKyPYX6NyCOhocyOYKFofJQaM+yK+7oCiW87CKyC1PGB9x02RNyPEVhYH/dnvbyJQIy5miqDwDyCUdrbwrY//AyJ9eV2qZmEJLyHHdecDpwUxwyBhv9j35SWI9yMY8HtN3xor4yBwffUng++ZuyPHSNu6VsI6JyC3t8J30WA1AyK21UwySi2PKyEG7E3JySJgxyK9KjndeC+MnyNKl9nlAulswyLsXExyZVT8hyO/PiS78fsrmyK6QuNmj0JSfyEoxKCYMCYwJyJPohs2s+ng8yEtsMFyuUQyvyLLiT6fFMI2ZyB2aiooLkwWZyJ6fOvrwhQyuyL8ZPm1AKheFyEdyU4gtj10UyBVgAyF1WlCByAiqIJWONp1eyPfNzmfpI8Q1yLpnKDZEhzhhyQ9y";

    private const string LegacyLargeLz77Level3 =
        "AHXNJUuE4uryAKaBIGdDNLJuAEvimVRzdn/xAMx1mY0eq87bAJc5ZW7KmMNxAAtu+omkOy8UAG47GkfFFCTKAPYX6NyCOhocAOYKFofJQaM+AK+7oCiW87CKAC1PGB9x02RNAPEVhYH/dnvbAJQIy5miqDwDACUdrbwrY//AAJ9eV2qZmEJLAHHdecDpwUxwABhv9j35SWI9AMY8HtN3xor4ABwffUng++ZuAPHSNu6VsI6JAC3t8J30WA1AAK21UwySi2PKAEG7E3JySJgxAK9KjndeC+MnANKl9nlAulswALsXExyZVT8hAO/PiS78fsrmAK6QuNmj0JSfAEoxKCYMCYwJAJPohs2s+ng8AEtsMFyuUQyvALLiT6fFMI2ZAB2aiooLkwWZAJ6fOvrwhQyuAL8ZPm1AKheFAEdyU4gtj10UABVgAyF1WlCBAAiqIJWONp1eAPfNzmfpI8Q1ALpnKDZEhzhhAHXNJUuE4uryAKaBIGdDNLJuAEs=";

    private const string LegacyLargeLz772Level3 =
        "yHXNJUuE4uryyKaBIGdDNLJuyEvimVRzdn/xyMx1mY0eq87byJc5ZW7KmMNxyAtu+omkOy8UyG47GkfFFCTKyPYX6NyCOhocyOYKFofJQaM+yK+7oCiW87CKyC1PGB9x02RNyPEVhYH/dnvbyJQIy5miqDwDyCUdrbwrY//AyJ9eV2qZmEJLyHHdecDpwUxwyBhv9j35SWI9yMY8HtN3xor4yBwffUng++ZuyPHSNu6VsI6JyC3t8J30WA1AyK21UwySi2PKyEG7E3JySJgxyK9KjndeC+MnyNKl9nlAulswyLsXExyZVT8hyO/PiS78fsrmyK6QuNmj0JSfyEoxKCYMCYwJyJPohs2s+ng8yEtsMFyuUQyvyLLiT6fFMI2ZyB2aiooLkwWZyJ6fOvrwhQyuyL8ZPm1AKheFyEdyU4gtj10UyBVgAyF1WlCByAiqIJWONp1eyPfNzmfpI8Q1yLpnKDZEhzhhyHXNJUuE4uryyKaBIGdDNLJuyEs=";

    private const string CandidateLimitLz77 =
        "AEFCQ2FiY2RlAGZnaGlqa2xtqm4REAAEEAEEEAIEEKoDBBAEBBAFBBAGBBCqBwQQCAQQCQQQCgQQqgsEEAwEEA0EEA4EEKoPBBAQBBARBBASBBCqEwQQFAQQFQQQFgQQqhcEEBgEEBkEEBoEEKobBBAcBBAdBBAeBBCqHwQQIAQQIQQQIgQQqiMEECQEECUEECYEEAYnBBCxwA==";

    private const string CandidateLimitLz772 =
        "yEFCQ2FiY2RlyGZnaGlqa2xtYm4m4gAz4gEz4gIz4mIDM+IEM+IFM+IGM+JiBzPiCDPiCTPiCjPiYgsz4gwz4g0z4g4z4mIPM+IQM+IRM+ISM+JiEzPiFDPiFTPiFjPiYhcz4hgz4hkz4hoz4mIbM+IcM+IdM+IeM+JiHzPiIDPiITPiIjPiYiMz4iQz4iUz4iYz4s4nS5P+Qw==";

    public static TheoryData<byte, string, string> LegacyLevels => new()
    {
        { 0, "AGFiY2RlZmdoAEFCQ0RFRkdIAGFiY2RlZmdo", "yGFiY2RlZmdoyEFCQ0RFRkdIyGFiY2RlZmdo" },
        { 1, "AGFiY2RlZmdoAEFCQ0RFRkdIAGFiY2RlZmdo", "yGFiY2RlZmdoyEFCQ0RFRkdIyGFiY2RlZmdo" },
        { 2, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 3, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 4, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 5, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 6, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 7, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 8, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 9, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
    };

    [Theory]
    [MemberData(nameof(LegacyLevels))]
    public void Compress_MatchesLegacySuiteToolsAtEveryLevel(
        byte level, string expectedLz77, string expectedLz772)
    {
        byte[] source = Encoding.ASCII.GetBytes("abcdefghABCDEFGHabcdefgh");

        Assert.Equal(Convert.FromBase64String(expectedLz77), Lz77.Compress(source, level));
        Assert.Equal(Convert.FromBase64String(expectedLz772), Lz772.Compress(source, level));
    }

    [Fact]
    public void Level3_UsesLegacySmallerWindow()
    {
        byte[] source = CreateDistantMatchFixture();

        Assert.Equal(Convert.FromBase64String(LegacyLargeLz77Level3), Lz77.Compress(source, 3));
        Assert.Equal(Convert.FromBase64String(SuperSsLargeLz77Level3), Lz77.Compress(source, 4));
        Assert.Equal(Convert.FromBase64String(LegacyLargeLz772Level3), Lz772.Compress(source, 3));
        Assert.Equal(Convert.FromBase64String(SuperSsLargeLz772Level3), Lz772.Compress(source, 4));
    }

    [Fact]
    public void EqualLengthMatches_SelectLatestReferenceCandidate()
    {
        byte[] source = Encoding.ASCII.GetBytes("abcXabcYabcZ");

        Assert.Equal(Convert.FromBase64String("UGFiY1gEEFkEEFo="), Lz77.Compress(source));
        Assert.Equal(Convert.FromBase64String("mGFiY1gwEFkwEFo="), Lz772.Compress(source));
    }

    [Fact]
    public void CandidateLimit_MatchesLegacySuiteTools()
    {
        byte[] source = CreateCandidateLimitFixture();

        Assert.Equal(Convert.FromBase64String(CandidateLimitLz77), Lz77.Compress(source));
        Assert.Equal(Convert.FromBase64String(CandidateLimitLz772), Lz772.Compress(source));
    }

    [Fact]
    public void LegacyAndSuperSsVectors_DecompressToOriginalBytes()
    {
        byte[] source = CreateDistantMatchFixture();

        foreach (string vector in new[] { LegacyLargeLz77Level3, SuperSsLargeLz77Level3 })
        {
            byte[] compressed = Convert.FromBase64String(vector);
            Assert.Equal(source, Lz77.Decompress(compressed, (uint)source.Length, (uint)compressed.Length));
        }

        foreach (string vector in new[] { LegacyLargeLz772Level3, SuperSsLargeLz772Level3 })
        {
            byte[] compressed = Convert.FromBase64String(vector);
            Assert.Equal(source, Lz772.Decompress(compressed, (uint)source.Length, (uint)compressed.Length));
        }
    }

    [Fact]
    public void PakWriter_MatchesLegacyCompressedPayloadAndArchiveHash()
    {
        using var temp = new TemporaryDirectory();
        string sourceDirectory = temp.Combine("legacy-source");
        Directory.CreateDirectory(sourceDirectory);
        byte[] source = CreateCandidateLimitFixture();
        File.WriteAllBytes(Path.Combine(sourceDirectory, "legacy.bin"), source);
        string pakPath = temp.Combine("legacy.pak");

        var writer = new PakWriter
        {
            EntryVersion = PakFileEntryVersion.V3,
            EntryType = PakFileEntryType.LZ772,
            CompressLevel = 5,
            LocationKeys = PakKeys.JP,
            Author = "LegacyCompression"
        };
        writer.CreateFromDirectoryContents(sourceDirectory, pakPath);

        Assert.Equal(
            "81E9718B466C40ADDABFA6858DAD02FC9EB19B2365E3A1C530847D19A2E43B08",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pakPath))));

        using var reader = new PakReader(pakPath);
        reader.Parse(PakKeys.JP);
        PakFileEntry entry = Assert.Single(reader.Entries);
        Assert.Equal(Convert.FromBase64String(CandidateLimitLz772),
            reader.ReadCompressedEntryBytes(entry));
    }

    [Fact]
    public void OversizedCompressedSize_ReturnsSafePartialOutput()
    {
        Assert.Equal([0x41, 0, 0, 0], Lz77.Decompress([0, 0x41], 4, 100));
        Assert.Equal([0x41, 0, 0, 0], Lz772.Decompress([0xC8, 0x41], 4, 100));
    }

    private static byte[] CreateDistantMatchFixture()
    {
        var source = new List<byte>(337);
        uint state = 0x12345678;
        for (int index = 0; index < 320; index++)
        {
            state = unchecked((state * 1664525) + 1013904223);
            source.Add((byte)(state >> 24));
        }

        source.AddRange(source.Take(17));
        return source.ToArray();
    }

    private static byte[] CreateCandidateLimitFixture()
    {
        var source = new List<byte>();
        byte[] target = Encoding.ASCII.GetBytes("ABCabcdefghijklmn");
        source.AddRange(target);
        for (int index = 0; index < 40; index++)
        {
            source.AddRange([0x41, 0x42, 0x43, (byte)index]);
        }
        source.AddRange(target);
        return source.ToArray();
    }
}
