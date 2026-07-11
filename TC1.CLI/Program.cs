using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TC1.Core.BigFile;
using TC1.Core.BinaryObject;
using TC1.Core.Hashing;

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (cmd)
{
    case "extract":
        DoExtract(args);
        break;
    case "repack":
        DoRepack(args);
        break;
    case "list":
        DoList(args);
        break;
    case "bo-dump":
        DoBoDump(args);
        break;
    case "hash":
        DoHash(args);
        break;
    default:
        PrintHelp();
        break;
}

static void PrintHelp()
{
    Console.WriteLine("TC1 Studio - CLI Tool");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  tc1 extract <fatfile> <datfile> <outputdir>");
    Console.WriteLine("  tc1 repack <indir> <fatfile> <datfile>");
    Console.WriteLine("  tc1 list <fatfile>");
    Console.WriteLine("  tc1 bo-dump <binfile>");
    Console.WriteLine("  tc1 hash <name>");
}

static void DoExtract(string[] args)
{
    if (args.Length < 4) { Console.WriteLine("Usage: tc1 extract <fatfile> <datfile> <outputdir>"); return; }

    var fatPath = args[1];
    var datPath = args[2];
    var outDir = args[3];

    using var fat = File.OpenRead(fatPath);
    using var dat = File.OpenRead(datPath);

    var reader = new BigFileReader();
    reader.Deserialize(fat);

    Directory.CreateDirectory(outDir);

    var dataReader = new DataReader(fat, dat, reader.Entries);

    for (int i = 0; i < reader.Entries.Count; i++)
    {
        var entry = reader.Entries[i];
        var ext = ".bin";
        var name = $"{i:D6}_{entry.NameHash:X16}{ext}";
        var path = Path.Combine(outDir, name);

        Console.WriteLine($"[{i}/{reader.Entries.Count}] {entry.NameHash:X16} @ {entry.Offset} ({entry.CompressedSize}B -> {entry.UncompressedSize}B) [{entry.CompressionScheme}]");

        var data = dataReader.ReadEntryData(entry);
        File.WriteAllBytes(path, data);
    }

    Console.WriteLine($"Extracted {reader.Entries.Count} entries to {outDir}");
}

static void DoRepack(string[] args)
{
    if (args.Length < 4) { Console.WriteLine("Usage: tc1 repack <indir> <fatfile> <datfile>"); return; }

    var inDir = args[1];
    var fatPath = args[2];
    var datPath = args[3];

    var files = Directory.GetFiles(inDir, "*.bin")
        .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f).Split('_')[0]))
        .ToList();

    using var fatOut = File.Create(fatPath);
    using var datOut = File.Create(datPath);

    var writer = new BigFileWriter();
    foreach (var file in files)
    {
        var data = File.ReadAllBytes(file);
        var nameHashStr = Path.GetFileNameWithoutExtension(file).Split('_')[1];
        var nameHash = Convert.ToUInt64(nameHashStr, 16);

        var entry = new Entry
        {
            NameHash = nameHash,
            Offset = datOut.Position,
            CompressedSize = (uint)data.Length,
            UncompressedSize = 0,
            CompressionScheme = CompressionScheme.None,
        };

        writer.Entries.Add(entry);
        datOut.Write(data);
    }

    writer.SerializeFat(fatOut);
    Console.WriteLine($"Repacked {files.Count} entries to {fatPath} / {datPath}");
}

static void DoList(string[] args)
{
    if (args.Length < 2) { Console.WriteLine("Usage: tc1 list <fatfile>"); return; }

    using var fat = File.OpenRead(args[1]);
    var reader = new BigFileReader();
    reader.Deserialize(fat);

    Console.WriteLine($"Version: {reader.Version}, Platform: {reader.Platform}, Entries: {reader.Entries.Count}");
    for (int i = 0; i < reader.Entries.Count; i++)
    {
        var e = reader.Entries[i];
        Console.WriteLine($"  [{i}] {e.NameHash:X16} off={e.Offset} comp={e.CompressedSize} uncomp={e.UncompressedSize} scheme={e.CompressionScheme}");
    }
}

static void DoBoDump(string[] args)
{
    if (args.Length < 2) { Console.WriteLine("Usage: tc1 bo-dump <binfile>"); return; }

    using var fs = File.OpenRead(args[1]);
    var bof = new BinaryObjectFile();
    bof.Deserialize(fs);

    DumpNode(bof.Root, 0);
}

static void DumpNode(Node node, int indent)
{
    var prefix = new string(' ', indent * 2);
    Console.WriteLine($"{prefix}Node 0x{node.NameHash:X8} ({node.Children.Count} children, {node.Fields.Count} fields)");

    foreach (var kv in node.Fields)
        Console.WriteLine($"{prefix}  Field 0x{kv.Key:X8} = {kv.Value.Length} bytes");

    foreach (var child in node.Children)
        DumpNode(child, indent + 1);
}

static void DoHash(string[] args)
{
    if (args.Length < 2) { Console.WriteLine("Usage: tc1 hash <name>"); return; }

    var name = args[1];
    Console.WriteLine($"CRC32:       0x{CRC32.Hash(name):X8}");
    Console.WriteLine($"CRC64 Jones: 0x{CRC64.Hash(name, true):X16}");
}
