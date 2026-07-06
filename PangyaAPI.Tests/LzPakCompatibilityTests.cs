using PangyaAPI.Utilities.Cryptography;
using System.Text;

namespace PangyaAPI.Tests;

public sealed class LzPakCompatibilityTests
{
    private const string LargeLz77Level3 =
        "AHXNJUuE4uryAKaBIGdDNLJuAEvimVRzdn/xAMx1mY0eq87bAJc5ZW7KmMNxAAtu+omkOy8UAG47GkfFFCTKAPYX6NyCOhocAOYKFofJQaM+AK+7oCiW87CKAC1PGB9x02RNAPEVhYH/dnvbAJQIy5miqDwDACUdrbwrY//AAJ9eV2qZmEJLAHHdecDpwUxwABhv9j35SWI9AMY8HtN3xor4ABwffUng++ZuAPHSNu6VsI6JAC3t8J30WA1AAK21UwySi2PKAEG7E3JySJgxAK9KjndeC+MnANKl9nlAulswALsXExyZVT8hAO/PiS78fsrmAK6QuNmj0JSfAEoxKCYMCYwJAJPohs2s+ng8AEtsMFyuUQyvALLiT6fFMI2ZAB2aiooLkwWZAJ6fOvrwhQyuAL8ZPm1AKheFAEdyU4gtj10UABVgAyF1WlCBAAiqIJWONp1eAPfNzmfpI8Q1ALpnKDZEhzhhAUDx";

    private const string LargeLz772Level3 =
        "yHXNJUuE4uryyKaBIGdDNLJuyEvimVRzdn/xyMx1mY0eq87byJc5ZW7KmMNxyAtu+omkOy8UyG47GkfFFCTKyPYX6NyCOhocyOYKFofJQaM+yK+7oCiW87CKyC1PGB9x02RNyPEVhYH/dnvbyJQIy5miqDwDyCUdrbwrY//AyJ9eV2qZmEJLyHHdecDpwUxwyBhv9j35SWI9yMY8HtN3xor4yBwffUng++ZuyPHSNu6VsI6JyC3t8J30WA1AyK21UwySi2PKyEG7E3JySJgxyK9KjndeC+MnyNKl9nlAulswyLsXExyZVT8hyO/PiS78fsrmyK6QuNmj0JSfyEoxKCYMCYwJyJPohs2s+ng8yEtsMFyuUQyvyLLiT6fFMI2ZyB2aiooLkwWZyJ6fOvrwhQyuyL8ZPm1AKheFyEdyU4gtj10UyBVgAyF1WlCByAiqIJWONp1eyPfNzmfpI8Q1yLpnKDZEhzhhyQ9y";

    public static TheoryData<byte, string, string> ReferenceLevels => new()
    {
        { 0, "AGFiY2RlZmdoAEFCQ0RFRkdIAGFiY2RlZmdo", "yGFiY2RlZmdoyEFCQ0RFRkdIyGFiY2RlZmdo" },
        { 1, "AGFiY2RlZmdoAEFCQ0RFRkdIAGFiY2RlZmdo", "yGFiY2RlZmdoyEFCQ0RFRkdIyGFiY2RlZmdo" },
        { 2, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 3, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 4, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
        { 5, "AGFiY2RlZmdoAEFCQ0RFRkdIARBg", "yGFiY2RlZmdoyEFCQ0RFRkdIyV/j" },
    };

    [Theory]
    [MemberData(nameof(ReferenceLevels))]
    public void Compress_MatchesSuperSsReferenceAtEveryLevel(
        byte level, string expectedLz77, string expectedLz772)
    {
        byte[] source = Encoding.ASCII.GetBytes("abcdefghABCDEFGHabcdefgh");

        Assert.Equal(Convert.FromBase64String(expectedLz77), Lz77.Compress(source, level));
        Assert.Equal(Convert.FromBase64String(expectedLz772), Lz772.Compress(source, level));
    }

    [Fact]
    public void Level3_UsesSuperSsFallThroughWindow()
    {
        byte[] source = CreateDistantMatchFixture();

        byte[] expectedLz77 = Convert.FromBase64String(LargeLz77Level3);
        byte[] expectedLz772 = Convert.FromBase64String(LargeLz772Level3);
        Assert.Equal(expectedLz77, Lz77.Compress(source, 3));
        Assert.Equal(expectedLz77, Lz77.Compress(source, 4));
        Assert.Equal(expectedLz772, Lz772.Compress(source, 3));
        Assert.Equal(expectedLz772, Lz772.Compress(source, 4));
    }

    [Fact]
    public void EqualLengthMatches_SelectLatestReferenceCandidate()
    {
        byte[] source = Encoding.ASCII.GetBytes("abcXabcYabcZ");

        Assert.Equal(Convert.FromBase64String("UGFiY1gEEFkEEFo="), Lz77.Compress(source));
        Assert.Equal(Convert.FromBase64String("mGFiY1gwEFkwEFo="), Lz772.Compress(source));
    }

    [Fact]
    public void ReferenceVectors_DecompressToOriginalBytes()
    {
        byte[] source = CreateDistantMatchFixture();
        byte[] lz77 = Convert.FromBase64String(LargeLz77Level3);
        byte[] lz772 = Convert.FromBase64String(LargeLz772Level3);

        Assert.Equal(source, Lz77.Decompress(lz77, (uint)source.Length, (uint)lz77.Length));
        Assert.Equal(source, Lz772.Decompress(lz772, (uint)source.Length, (uint)lz772.Length));
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
}
