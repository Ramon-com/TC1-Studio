using System;
using System.Collections.Generic;
using System.IO;
using TC1.Core.BigFile;
using TC1.Core.Compression;

namespace TC1.Studio.Services;

public class ArchiveService
{
    public BigFileReader CurrentReader { get; private set; }
    public string FatPath { get; private set; }
    public string DatPath { get; private set; }
    public string DataDir { get; private set; }
    public bool IsModified { get; set; }

    public void Open(string fatPath, string datPath)
    {
        FatPath = fatPath;
        DatPath = datPath;
        DataDir = Path.Combine(Path.GetDirectoryName(fatPath), Path.GetFileNameWithoutExtension(fatPath) + "_extracted");

        using var fat = File.OpenRead(fatPath);
        CurrentReader = new BigFileReader();
        CurrentReader.Deserialize(fat);

        IsModified = false;
    }

    public byte[] ExtractEntry(int index)
    {
        var entry = CurrentReader.Entries[index];
        using var dat = File.OpenRead(DatPath);
        var reader = new DataReader(null, dat, CurrentReader.Entries);
        return reader.ReadEntryData(entry);
    }

    public void Repack(string outputFat, string outputDat)
    {
        var extractedDir = DataDir;
        if (!Directory.Exists(extractedDir))
            throw new DirectoryNotFoundException($"Extracted data not found: {extractedDir}");

        using var fatOut = File.Create(outputFat);
        using var datOut = File.Create(outputDat);

        var writer = new BigFileWriter();

        for (int i = 0; i < CurrentReader.Entries.Count; i++)
        {
            var entry = CurrentReader.Entries[i];
            var filePath = Path.Combine(extractedDir, $"{i:D6}_{entry.NameHash:X16}.bin");
            var data = File.ReadAllBytes(filePath);

            var newEntry = new Entry
            {
                NameHash = entry.NameHash,
                Offset = datOut.Position,
            };

            if (entry.CompressionScheme == CompressionScheme.LZO1x && LZO.IsAvailable)
            {
                var compressed = LZO.Compress(data);
                newEntry.CompressedSize = (uint)compressed.Length;
                newEntry.UncompressedSize = (uint)data.Length;
                newEntry.CompressionScheme = CompressionScheme.LZO1x;
                datOut.Write(compressed);
            }
            else
            {
                newEntry.CompressedSize = (uint)data.Length;
                newEntry.UncompressedSize = 0;
                newEntry.CompressionScheme = CompressionScheme.None;
                datOut.Write(data);
            }

            writer.Entries.Add(newEntry);
        }

        writer.SerializeFat(fatOut);
        IsModified = false;
    }
}
