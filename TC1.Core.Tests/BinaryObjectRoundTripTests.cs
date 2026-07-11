using TC1.Core.BinaryObject;
using FluentAssertions;

namespace TC1.Core.Tests;

public class BinaryObjectRoundTripTests
{
    private static BinaryObjectFile MakeSimpleFile()
    {
        var bo = new BinaryObjectFile();
        bo.Root.NameHash = 0x2002CFD9;
        bo.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(3.14f);
        bo.Root.Fields[0xBBBBBBBB] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var child = new Node();
        child.NameHash = 0x12345678;
        child.Fields[0xCCCCCCCC] = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        bo.Root.Children.Add(child);
        return bo;
    }

    [Fact]
    public void Round_trip_byte_for_byte()
    {
        var bo = MakeSimpleFile();
        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var original = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(original);
        bo2.Deserialize(ms2);

        using var ms3 = new MemoryStream();
        bo2.Serialize(ms3);
        var roundtripped = ms3.ToArray();

        roundtripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Empty_root_node_round_trips()
    {
        var bo = new BinaryObjectFile();
        bo.Root.NameHash = 0;

        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var data = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(data);
        bo2.Deserialize(ms2);

        bo2.Root.NameHash.Should().Be(0);
        bo2.Root.Fields.Should().BeEmpty();
        bo2.Root.Children.Should().BeEmpty();
    }

    [Fact]
    public void Node_with_many_children_round_trips()
    {
        var bo = new BinaryObjectFile();
        for (int i = 0; i < 100; i++)
        {
            bo.Root.Children.Add(new Node { NameHash = (uint)i });
        }

        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var data = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(data);
        bo2.Deserialize(ms2);

        bo2.Root.Children.Should().HaveCount(100);
        for (int i = 0; i < 100; i++)
            bo2.Root.Children[i].NameHash.Should().Be((uint)i);
    }

    [Fact]
    public void Fields_with_various_sizes_round_trip()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = new byte[] { 0x01 };
        bo.Root.Fields[2] = new byte[] { 0x01, 0x02 };
        bo.Root.Fields[4] = BitConverter.GetBytes(1.0f);
        bo.Root.Fields[8] = new byte[8];
        bo.Root.Fields[12] = new byte[12];
        bo.Root.Fields[16] = new byte[16];
        bo.Root.Fields[255] = new byte[255];

        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var data = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(data);
        bo2.Deserialize(ms2);

        bo2.Root.Fields.Should().ContainKeys(1u, 2u, 4u, 8u, 12u, 16u, 255u);
        bo2.Root.Fields[4].Should().BeEquivalentTo(BitConverter.GetBytes(1.0f));
    }

    [Fact]
    public void Unknown_field_preserved()
    {
        var bo = new BinaryObjectFile();
        var unknownBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        bo.Root.Fields[0xDEADBEEF] = unknownBytes;

        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var data = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(data);
        bo2.Deserialize(ms2);

        bo2.Root.Fields[0xDEADBEEF].Should().BeEquivalentTo(unknownBytes);
    }

    [Fact]
    public void Deeply_nested_nodes_round_trip()
    {
        var bo = new BinaryObjectFile();
        var current = bo.Root;
        for (int i = 0; i < 50; i++)
        {
            var child = new Node { NameHash = (uint)i };
            current.Children.Add(child);
            current = child;
        }

        using var ms1 = new MemoryStream();
        bo.Serialize(ms1);
        var data = ms1.ToArray();

        var bo2 = new BinaryObjectFile();
        using var ms2 = new MemoryStream(data);
        bo2.Deserialize(ms2);

        var walk = bo2.Root;
        for (int i = 0; i < 50; i++)
        {
            walk.Children.Should().ContainSingle();
            walk = walk.Children[0];
            walk.NameHash.Should().Be((uint)i);
        }
    }

    [Fact]
    public void Clone_is_independent()
    {
        var bo = MakeSimpleFile();
        var clone = bo.Clone();

        clone.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(99.0f);

        bo.Root.Fields[0xAAAAAAAA].Should().BeEquivalentTo(BitConverter.GetBytes(3.14f));
    }
}
