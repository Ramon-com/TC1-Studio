using TC1.Core.Hashing;
using FluentAssertions;

namespace TC1.Core.Tests;

public class Crc64Tests
{
    [Fact]
    public void Jones_empty_input_returns_zero()
    {
        var hash = CRC64.Hash("", jones: true);
        hash.Should().Be(0UL);
    }

    [Fact]
    public void Jones_deterministic()
    {
        var hash1 = CRC64.Hash("abc", jones: true);
        var hash2 = CRC64.Hash("abc", jones: true);
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Jones_not_same_as_standard()
    {
        var jones = CRC64.Hash("abc", jones: true);
        var std = CRC64.Hash("abc", jones: false);
        jones.Should().NotBe(std);
    }

    [Fact]
    public void Different_inputs_produce_different_hashes()
    {
        var h1 = CRC64.Hash("RainPrecipitationAmount", jones: true);
        var h2 = CRC64.Hash("RoadWetness", jones: true);
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void BigFile_name_hash_consistency()
    {
        var hash1 = CRC64.Hash(@"global\weather\rain.bin", jones: true);
        var hash2 = CRC64.Hash(@"global\weather\rain.bin", jones: true);
        hash1.Should().Be(hash2);
    }
}
