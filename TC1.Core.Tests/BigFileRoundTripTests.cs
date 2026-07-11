using TC1.Core;
using TC1.Core.BigFile;
using FluentAssertions;

namespace TC1.Core.Tests;

public class BigFileRoundTripTests
{
    [Fact]
    public void Read_synthetic_fat_back()
    {
        var writer = new BigFileWriter { Version = 5, Platform = Platform.PC };
        writer.Entries.Add(new Entry
        {
            NameHash = 0xABCDEF0123456789UL,
            Offset = 256,
            CompressedSize = 100,
            UncompressedSize = 100,
            CompressionScheme = CompressionScheme.None,
        });
        writer.Entries.Add(new Entry
        {
            NameHash = 0x1234567890ABCDEFUL,
            Offset = 512,
            CompressedSize = 200,
            UncompressedSize = 0,
            CompressionScheme = CompressionScheme.LZO1x,
        });

        using var fat = new MemoryStream();
        writer.SerializeFat(fat);
        fat.Position = 0;

        var reader = new BigFileReader();
        reader.Deserialize(fat);

        reader.Version.Should().Be(5);
        reader.Platform.Should().Be(Platform.PC);
        reader.Entries.Should().HaveCount(2);

        reader.Entries[0].NameHash.Should().Be(0xABCDEF0123456789UL);
        reader.Entries[0].Offset.Should().Be(256);
        reader.Entries[0].CompressedSize.Should().Be(100);
        reader.Entries[0].UncompressedSize.Should().Be(100);
        reader.Entries[0].CompressionScheme.Should().Be(CompressionScheme.None);

        reader.Entries[1].NameHash.Should().Be(0x1234567890ABCDEFUL);
        reader.Entries[1].Offset.Should().Be(512);
        reader.Entries[1].CompressedSize.Should().Be(200);
        reader.Entries[1].CompressionScheme.Should().Be(CompressionScheme.LZO1x);
    }

    [Fact]
    public void Bad_signature_throws()
    {
        using var ms = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
        var reader = new BigFileReader();
        FluentActions.Invoking(() => reader.Deserialize(ms))
            .Should().Throw<FormatException>().WithMessage("*FAT2*");
    }

    [Fact]
    public void Version_2_supported()
    {
        using var ms = new MemoryStream();
        ms.WriteValueU32(0x46415432); // FAT2
        ms.WriteValueU32(2);          // version 2
        ms.WriteValueU32(0);          // 0 entries
        ms.Position = 0;

        var reader = new BigFileReader();
        reader.Deserialize(ms);
        reader.Version.Should().Be(2);
        reader.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Zero_entries_round_trips()
    {
        var writer = new BigFileWriter { Version = 5 };
        using var fat = new MemoryStream();
        writer.SerializeFat(fat);
        fat.Position = 0;

        var reader = new BigFileReader();
        reader.Deserialize(fat);
        reader.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Large_offset_entry_round_trips()
    {
        var writer = new BigFileWriter { Version = 5 };
        writer.Entries.Add(new Entry
        {
            NameHash = 1,
            Offset = 0x1FFFFFFFF, // 34-bit value
            CompressedSize = 50,
            UncompressedSize = 50,
            CompressionScheme = CompressionScheme.None,
        });

        using var fat = new MemoryStream();
        writer.SerializeFat(fat);
        fat.Position = 0;

        var reader = new BigFileReader();
        reader.Deserialize(fat);
        reader.Entries[0].Offset.Should().Be(0x1FFFFFFFF);
    }

    [Fact]
    public void Entry_count_round_trips()
    {
        var writer = new BigFileWriter { Version = 5 };
        for (int i = 0; i < 1000; i++)
        {
            writer.Entries.Add(new Entry
            {
                NameHash = (ulong)i,
                Offset = i * 100L,
                CompressedSize = (uint)(i * 10),
                UncompressedSize = (uint)(i * 10),
                CompressionScheme = CompressionScheme.None,
            });
        }

        using var fat = new MemoryStream();
        writer.SerializeFat(fat);
        fat.Position = 0;

        var reader = new BigFileReader();
        reader.Deserialize(fat);
        reader.Entries.Should().HaveCount(1000);
        reader.Entries[500].NameHash.Should().Be(500);
    }
}
