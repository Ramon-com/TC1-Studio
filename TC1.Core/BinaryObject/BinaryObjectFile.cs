using System.Collections.Generic;
using System.IO;

namespace TC1.Core.BinaryObject;

public class BinaryObjectFile
{
    public const uint Signature = 0x4643626E; // 'FCbn'

    public Node Root { get; set; } = new();

    public BinaryObjectFile Clone()
    {
        return new BinaryObjectFile { Root = Root?.Clone() };
    }

    public void Deserialize(Stream input)
    {
        var magic = input.ReadValueU32();
        if (magic != Signature)
            throw new System.FormatException("Invalid FCbn signature");

        var version = input.ReadValueU16();
        if (version != 2)
            throw new System.FormatException("Unsupported BinaryObject version");

        var flags = input.ReadValueU16();
        if (flags != 0)
            throw new System.FormatException("Unsupported BinaryObject flags");

        var totalObjectCount = input.ReadValueU32();
        var totalValueCount = input.ReadValueU32();

        Root = ReadNode(input);
    }

    public void Serialize(Stream output)
    {
        uint totalObjectCount = 1;
        uint totalValueCount = 0;

        using var data = new MemoryStream();
        WriteNode(data, Root, ref totalObjectCount, ref totalValueCount);
        data.Position = 0;

        output.WriteValueU32(Signature);
        output.WriteValueU16(2);
        output.WriteValueU16(0);
        output.WriteValueU32(totalObjectCount);
        output.WriteValueU32(totalValueCount);
        data.CopyTo(output);
    }

    private static Node ReadNode(Stream input)
    {
        var childCount = input.ReadCount(out var isOffset);
        if (isOffset)
            throw new System.NotImplementedException("Offset-based child references not yet supported");

        var node = new Node();
        node.NameHash = input.ReadValueU32();

        var valueCount = input.ReadCount(out isOffset);
        if (isOffset)
            throw new System.NotImplementedException("Offset-based field references not yet supported");

        for (uint i = 0; i < valueCount; i++)
        {
            var nameHash = input.ReadValueU32();
            var position = input.Position;
            var size = input.ReadCount(out isOffset);

            byte[] value;
            if (isOffset)
            {
                input.Seek(position - size, SeekOrigin.Begin);
                size = input.ReadCount(out isOffset);
                if (isOffset)
                    throw new System.FormatException("Nested offset in field size");
                value = new byte[size];
                input.ReadExactly(value);
                input.Seek(position, SeekOrigin.Begin);
                input.ReadCount(out isOffset);
            }
            else
            {
                value = new byte[size];
                input.ReadExactly(value);
            }

            node.Fields[nameHash] = value;
        }

        for (uint i = 0; i < childCount; i++)
        {
            node.Children.Add(ReadNode(input));
        }

        return node;
    }

    private static void WriteNode(Stream output, Node node, ref uint totalObjectCount, ref uint totalValueCount)
    {
        totalObjectCount += (uint)node.Children.Count;
        totalValueCount += (uint)node.Fields.Count;

        output.WriteCount((uint)node.Children.Count, false);
        output.WriteValueU32(node.NameHash);
        output.WriteCount((uint)node.Fields.Count, false);

        foreach (var kv in node.Fields)
        {
            output.WriteValueU32(kv.Key);
            output.WriteCount((uint)kv.Value.Length, false);
            output.Write(kv.Value);
        }

        foreach (var child in node.Children)
        {
            WriteNode(output, child, ref totalObjectCount, ref totalValueCount);
        }
    }
}
