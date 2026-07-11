using TC1.Core.Hashing;
using FluentAssertions;

namespace TC1.Core.Tests;

public class Crc32Tests
{
    [Fact]
    public void RainPrecipitationAmount_known_vector()
    {
        var hash = CRC32.Hash("RainPrecipitationAmount");
        hash.Should().Be(0x2002CFD9);
    }

    [Fact]
    public void Empty_string_returns_all_ones()
    {
        var hash = CRC32.Hash("");
        hash.Should().Be(0x00000000);
    }

    [Theory]
    [InlineData("", 0x00000000)]
    [InlineData("A", 0xD3D99E8B)]
    [InlineData("123456789", 0xCBF43926)]
    public void Known_vectors(string input, uint expected)
    {
        var hash = CRC32.Hash(input);
        hash.Should().Be(expected);
    }
}
