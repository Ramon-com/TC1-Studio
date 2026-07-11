using System;
using System.IO;
using TC1.Core.BinaryObject;

// Write test BO file
var bo = new BinaryObjectFile();
bo.Root.NameHash = 0x2002CFD9;
bo.Root.Fields[0xAAAAAAAA] = new byte[] { 0x00, 0x00, 0x00, 0x40 };
bo.Root.Fields[0xBBBBBBBB] = new byte[0];
var child = new Node();
child.NameHash = 0x12345678;
child.Fields[0xCCCCCCCC] = new byte[] { 0x01 };
bo.Root.Children.Add(child);

var path = Path.Combine(Path.GetTempPath(), "test_bo.bin");
using (var fs = File.Create(path)) bo.Serialize(fs);

Console.WriteLine($"Written: {new FileInfo(path).Length} bytes");
Console.WriteLine($"Hash of file: {BitConverter.ToString(File.ReadAllBytes(path)).Replace("-", "")}");

// Read back and dump
using var fs2 = File.OpenRead(path);
var bo2 = new BinaryObjectFile();
bo2.Deserialize(fs2);
DumpNode(bo2.Root, 0);

// Round-trip verify
using var ms = new MemoryStream();
bo2.Serialize(ms);
var original = File.ReadAllBytes(path);
var roundtripped = ms.ToArray();
Console.WriteLine($"Round-trip OK: {original.AsSpan().SequenceEqual(roundtripped)}");

static void DumpNode(Node node, int indent)
{
    var p = new string(' ', indent * 2);
    Console.WriteLine($"{p}Node 0x{node.NameHash:X8} children={node.Children.Count} fields={node.Fields.Count}");
    foreach (var kv in node.Fields)
        Console.WriteLine($"{p}  F:0x{kv.Key:X8} = [{kv.Value.Length}] {BitConverter.ToString(kv.Value)}");
    foreach (var c in node.Children)
        DumpNode(c, indent + 1);
}
