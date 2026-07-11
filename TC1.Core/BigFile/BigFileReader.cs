using System;
using System.Collections.Generic;
using System.IO;

namespace TC1.Core.BigFile;

public class BigFileReader
{
    public const uint Signature = 0x46415432; // 'FAT2'
    public int Version { get; private set; }
    public Platform Platform { get; private set; }
    public uint Unknown74 { get; private set; }
    public List<Entry> Entries { get; } = new();

    public void Deserialize(Stream input)
    {
        var magic = input.ReadValueU32();
        if (magic != Signature)
            throw new FormatException("Bad FAT2 signature");

        Version = (int)input.ReadValueU32();
        if (Version < 2 || Version > 9)
            throw new FormatException($"Unsupported FAT version: {Version}");

        if (Version >= 3)
        {
            Unknown74 = input.ReadValueU32();
            Platform = (Platform)(Unknown74 & 0xFF);
            Unknown74 >>= 8;
        }
        else
        {
            Platform = Platform.PC;
        }

        if (Version >= 9)
        {
            var subfatTotalCount = (int)input.ReadValueU32();
            var subfatCount = (int)input.ReadValueU32();
            if (subfatTotalCount < 0 || subfatCount < 0)
                throw new FormatException("Invalid subfat counts");
            for (int i = 0; i < subfatCount; i++)
            {
                var count = input.ReadValueU32();
                for (int j = 0; j < count; j++)
                {
                    var entry = ReadEntryV5(input);
                }
            }
        }

        var entryCount = input.ReadValueU32();
        Entries.Clear();
        for (uint i = 0; i < entryCount; i++)
        {
            Entries.Add(ReadEntryV5(input));
        }

        if (Version >= 7)
        {
            var unknownCount = input.ReadValueU32();
            for (uint i = 0; i < unknownCount; i++)
            {
                input.ReadExactly(new byte[16]);
            }
        }
    }

    private static Entry ReadEntryV5(Stream input)
    {
        var a = input.ReadValueU32();
        input.ReadValueU32(); // zeros / author
        var hash = input.ReadValueU64();
        var c = input.ReadValueU32();
        var d = input.ReadValueU32();

        var entry = new Entry
        {
            UncompressedSize = (a & 0xFFFFFFFCu) >> 2,
            CompressionScheme = (CompressionScheme)(a & 0x03u),
            NameHash = hash,
            CompressedSize = c & 0x3FFFFFFFu,
            Offset = ((long)d << 2) | ((c >> 30) & 3),
        };
        return entry;
    }
}
