using System;
using System.Collections.Generic;
using System.IO;
using TC1.Core.Compression;

namespace TC1.Core.BigFile;

public class BigFileWriter
{
    public const uint Signature = 0x46415432; // 'FAT2'
    public int Version { get; set; } = 5;
    public Platform Platform { get; set; } = Platform.PC;
    public uint Unknown74 { get; set; }
    public List<Entry> Entries { get; } = new();

    public void Repack(Stream fatInput, Stream datInput, Stream fatOutput, Stream datOutput)
    {
        var reader = new BigFileReader();
        reader.Deserialize(fatInput);

        var dataReader = new DataReader(fatInput, datInput, reader.Entries);
        fatOutput.Position = 0;
        datOutput.Position = 0;

        var newEntries = new List<Entry>();

        foreach (var entry in reader.Entries)
        {
            var data = dataReader.ReadEntryData(entry);

            var newEntry = new Entry
            {
                NameHash = entry.NameHash,
                Offset = datOutput.Position,
            };

            if (entry.CompressionScheme == CompressionScheme.None)
            {
                newEntry.CompressedSize = (uint)data.Length;
                newEntry.UncompressedSize = 0;
                newEntry.CompressionScheme = CompressionScheme.None;
                datOutput.Write(data);
            }
            else if (entry.CompressionScheme == CompressionScheme.LZO1x && LZO.IsAvailable)
            {
                var compressed = LZO.Compress(data);
                newEntry.CompressedSize = (uint)compressed.Length;
                newEntry.UncompressedSize = (uint)data.Length;
                newEntry.CompressionScheme = CompressionScheme.LZO1x;
                datOutput.Write(compressed);
            }
            else
            {
                throw new NotSupportedException(
                    $"Compression scheme {entry.CompressionScheme} not supported for repacking");
            }

            newEntries.Add(newEntry);
        }

        Entries.Clear();
        Entries.AddRange(newEntries);

        SerializeFat(fatOutput);
    }

    public void SerializeFat(Stream output)
    {
        output.WriteValueU32(Signature);
        output.WriteValueU32((uint)Version);

        if (Version >= 3)
        {
            var platform = (uint)Platform & 0xFF;
            platform |= Unknown74 << 8;
            output.WriteValueU32(platform);
        }

        output.WriteValueU32((uint)Entries.Count);
        foreach (var entry in Entries)
        {
            WriteEntryV5(output, entry);
        }

        if (Version >= 7)
        {
            output.WriteValueU32(0);
        }
    }

    private static void WriteEntryV5(Stream output, Entry entry)
    {
        uint a = 0;
        a |= ((entry.UncompressedSize << 2) & 0xFFFFFFFCu);
        a |= (uint)((byte)entry.CompressionScheme & 0x03u);

        uint c = entry.CompressedSize & 0x3FFFFFFFu;
        c |= (uint)(entry.Offset & 3) << 30;

        uint d = (uint)(entry.Offset >> 2);

        output.WriteValueU32(a);
        output.WriteValueU32(0); // author / zeros
        output.WriteValueU64(entry.NameHash);
        output.WriteValueU32(c);
        output.WriteValueU32(d);
    }
}
