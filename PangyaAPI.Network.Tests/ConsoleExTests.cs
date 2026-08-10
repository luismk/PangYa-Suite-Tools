using PangyaAPI.Network.PangyaUtil;
using Xunit;

namespace PangyaAPI.Network.Tests;

public sealed class ConsoleExTests
{
    [Theory]
    [InlineData("AuthServer", "Auth Server")]
    [InlineData("LoginServer", "Login Server")]
    [InlineData("GameServer", "Game Server")]
    [InlineData("MessengerServer", "Messenger Server")]
    [InlineData("RankingServer", "Ranking Server")]
    public void Log_PrintsRoleCreditAndRepositoryOnce(string runtimeType, string role)
    {
        var original = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            ConsoleEx.Log(runtimeType);
        }
        finally
        {
            Console.SetOut(original);
        }

        var banner = output.ToString();
        Assert.Equal(1, CountOccurrences(banner, role));
        Assert.Equal(1, CountOccurrences(banner, "Credits: luismk"));
        Assert.Equal(1, CountOccurrences(banner, "https://github.com/Robert-LTH/PangYa-Server"));
        Assert.DoesNotContain("SERVIDOR DE DESENVOLVIMENTO", banner, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string expected)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(expected, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += expected.Length;
        }
        return count;
    }
}
