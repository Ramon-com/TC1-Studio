using System;
using System.IO;
using FluentAssertions;
using TC1.Core.BigFile;
using TC1.Core.BinaryObject;
using TC1.Core.Compression;
using TC1.Studio.Services;
using TC1.Studio.ViewModels;

namespace TC1.Studio.Tests;

public class NewFeaturesTests
{
    [Fact]
    public void ParseHex_parses_spaced_and_unspaced()
    {
        PropertyGridViewModel.ParseHex("FF 00 80 3F").Should().Equal(new byte[] { 0xFF, 0x00, 0x80, 0x3F });
        PropertyGridViewModel.ParseHex("FF00803F").Should().Equal(new byte[] { 0xFF, 0x00, 0x80, 0x3F });
        PropertyGridViewModel.ParseHex("de ad").Should().Equal(new byte[] { 0xDE, 0xAD });
    }

    [Fact]
    public void ParseHex_throws_on_odd_length()
    {
        Action act = () => PropertyGridViewModel.ParseHex("FF0");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseHex_throws_on_invalid_digits()
    {
        Action act = () => PropertyGridViewModel.ParseHex("ZZ 00");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseHex_throws_on_empty()
    {
        Action act = () => PropertyGridViewModel.ParseHex("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddField_command_adds_and_undo_removes()
    {
        var node = new Node { NameHash = 1 };
        var stack = new CommandStack();
        var value = new byte[] { 0x01, 0x02 };

        stack.Execute(new AddFieldCommand(node, 0xDEADBEEF, value, "Test"));

        node.Fields.Should().ContainKey(0xDEADBEEF);
        node.Fields[0xDEADBEEF].Should().Equal(value);

        stack.Undo();
        node.Fields.Should().NotContainKey(0xDEADBEEF);

        stack.Redo();
        node.Fields.Should().ContainKey(0xDEADBEEF);
    }

    [Fact]
    public void RemoveField_command_removes_and_undo_restores()
    {
        var node = new Node { NameHash = 1 };
        node.Fields[0xAAAAAAAA] = new byte[] { 0x42 };
        var stack = new CommandStack();

        stack.Execute(new RemoveFieldCommand(node, 0xAAAAAAAA, new byte[] { 0x42 }, "Test"));

        node.Fields.Should().NotContainKey(0xAAAAAAAA);

        stack.Undo();
        node.Fields.Should().ContainKey(0xAAAAAAAA);
        node.Fields[0xAAAAAAAA].Should().Equal(new byte[] { 0x42 });
    }

    [Fact]
    public void Repack_uses_source_dat_for_unopened_entries()
    {
        var dir = Directory.CreateTempSubdirectory("tc1test").FullName;
        try
        {
            var datPath = Path.Combine(dir, "test.dat");
            var fatPath = Path.Combine(dir, "test.fat");

            var writer = new BigFileWriter();
            writer.Entries.Add(new Entry
            {
                NameHash = 0x1111111111111111,
                Offset = 0,
                CompressedSize = 4,
                UncompressedSize = 4,
                CompressionScheme = CompressionScheme.None,
            });
            writer.Entries.Add(new Entry
            {
                NameHash = 0x2222222222222222,
                Offset = 4,
                CompressedSize = 4,
                UncompressedSize = 4,
                CompressionScheme = CompressionScheme.None,
            });

            using (var datOut = File.Create(datPath))
            {
                datOut.Write(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });
                datOut.Write(new byte[] { 0x11, 0x22, 0x33, 0x44 });
            }
            using (var fatOut = File.Create(fatPath))
                writer.SerializeFat(fatOut);

            var service = new ArchiveService();
            service.Open(fatPath, datPath);

            // Open entry 0 only; entry 1 stays unopened so Repack must pull it from the dat.
            var data0 = service.ExtractEntry(0);
            new FileService().WriteExtractedFile(service.DataDir, 0, 0x1111111111111111, data0);

            var outFat = Path.Combine(dir, "out.fat");
            var outDat = Path.Combine(dir, "out.dat");
            service.Repack(outFat, outDat);

            using var fatIn = File.OpenRead(outFat);
            var reader = new BigFileReader();
            reader.Deserialize(fatIn);
            reader.Entries.Should().HaveCount(2);

            using var datIn = File.OpenRead(outDat);
            var dataReader = new DataReader(null, datIn, reader.Entries);
            dataReader.ReadEntryData(reader.Entries[0]).Should().Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });
            dataReader.ReadEntryData(reader.Entries[1]).Should().Equal(new byte[] { 0x11, 0x22, 0x33, 0x44 });
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ImportPack_loads_entries_and_counts()
    {
        var dir = Directory.CreateTempSubdirectory("tc1test").FullName;
        try
        {
            var pack = Path.Combine(dir, "pack.json");
            File.WriteAllText(pack, """
                {
                  "0xDEADBEEF": "Dummy",
                  "0x1122334455667788": "WideDummy"
                }
                """);

            var service = new HashService(Path.Combine(dir, "hashes"));
            service.ImportPack(pack).Should().Be(2);
            service.Resolve(0xDEADBEEF).Should().Be("Dummy");
            service.Resolve(0x1122334455667788UL).Should().Be("WideDummy");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ImportPack_copies_to_community_dir_and_persists()
    {
        var dir = Directory.CreateTempSubdirectory("tc1test").FullName;
        try
        {
            var pack = Path.Combine(dir, "my_pack.json");
            File.WriteAllText(pack, "{ \"0xCAFEBABE\": \"Claw\" }");

            var hashesDir = Path.Combine(dir, "hashes");
            var service = new HashService(hashesDir);
            service.ImportPack(pack);

            File.Exists(Path.Combine(hashesDir, "Community", "my_pack.json")).Should().BeTrue();

            var fresh = new HashService(hashesDir);
            fresh.Resolve(0xCAFEBABE).Should().Be("Claw");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ExportPack_round_trips_through_import()
    {
        var dir = Directory.CreateTempSubdirectory("tc1test").FullName;
        try
        {
            var hashesDir = Path.Combine(dir, "hashes");
            var service = new HashService(hashesDir);
            service.SaveUserHash(0x12345678u, "Life");
            service.SaveUserHash(0xAABBCCDDEEFF0011UL, "Universe");
            service.SaveUserHash(0x0000111122223333UL, "Everything");

            var pack = Path.Combine(dir, "out.json");
            HashService.WritePack(pack, service.GetEntries());

            var other = new HashService(Path.Combine(dir, "other"));
            other.ImportPack(pack).Should().Be(3);
            other.Resolve(0x12345678).Should().Be("Life");
            other.Resolve(0xAABBCCDDEEFF0011UL).Should().Be("Universe");
            other.Resolve(0x0000111122223333UL).Should().Be("Everything");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
