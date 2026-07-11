using TC1.Core;
using FluentAssertions;

namespace TC1.Core.Tests;

public class BinaryObjectTypeHelperTests
{
    [Fact]
    public void Float_4_bytes()
    {
        var data = BitConverter.GetBytes(3.14f);
        BinaryObjectTypeHelper.DetectType(data).Should().Be("float");
    }

    [Fact]
    public void Float64_8_bytes()
    {
        var data = BitConverter.GetBytes(Math.PI);
        BinaryObjectTypeHelper.DetectType(data).Should().Be("float64");
    }

    [Fact]
    public void Vec3_12_bytes()
    {
        var data = new byte[12];
        BitConverter.GetBytes(1.0f).CopyTo(data, 0);
        BitConverter.GetBytes(2.0f).CopyTo(data, 4);
        BitConverter.GetBytes(3.0f).CopyTo(data, 8);
        BinaryObjectTypeHelper.DetectType(data).Should().Be("vec3");
    }

    [Fact]
    public void Vec4_16_bytes()
    {
        var data = new byte[16];
        BitConverter.GetBytes(1.0f).CopyTo(data, 0);
        BitConverter.GetBytes(2.0f).CopyTo(data, 4);
        BitConverter.GetBytes(3.0f).CopyTo(data, 8);
        BitConverter.GetBytes(4.0f).CopyTo(data, 12);
        BinaryObjectTypeHelper.DetectType(data).Should().Be("vec4");
    }

    [Fact]
    public void Byte_1_byte()
    {
        BinaryObjectTypeHelper.DetectType(new byte[] { 0x42 }).Should().Be("byte");
    }

    [Fact]
    public void Int16_2_bytes()
    {
        BinaryObjectTypeHelper.DetectType(new byte[] { 0x00, 0x01 }).Should().Be("int16");
    }

    [Fact]
    public void Empty_bytes()
    {
        BinaryObjectTypeHelper.DetectType([]).Should().Be("empty");
    }

    [Fact]
    public void Non_float_8_bytes_returns_bytes()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        BinaryObjectTypeHelper.DetectType(data).Should().Be("bytes[8]");
    }

    [Fact]
    public void Non_float_12_bytes_returns_bytes()
    {
        var data = new byte[] { 0x00, 0x00, 0x80, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 };
        BinaryObjectTypeHelper.DetectType(data).Should().Be("bytes[12]");
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x80, 0x3F }, "float", "1")]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x40 }, "float", "2")]
    [InlineData(new byte[] { 0x42 }, "byte", "66")]
    public void FormatValue_known_types(byte[] data, string type, string expected)
    {
        BinaryObjectTypeHelper.FormatValue(data, type).Should().Be(expected);
    }
}
