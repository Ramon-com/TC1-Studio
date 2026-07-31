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
        using var dat = File.OpenRead(DatPath);
        var reader = new DataReader(null, dat, CurrentReader.Entries);
        return reader.ReadEntryData(CurrentReader.Entries[index]);
    }

    public byte[][] ExtractAll()
    {
        using var dat = File.OpenRead(DatPath);
        var reader = new DataReader(null, dat, CurrentReader.Entries);
        var result = new byte[CurrentReader.Entries.Count][];
        for (int i = 0; i < CurrentReader.Entries.Count; i++)
            result[i] = reader.ReadEntryData(CurrentReader.Entries[i]);
        return result;
    }

    public void Repack(string outputFat, string outputDat)
    {
        var extractedDir = DataDir;
        if (!Directory.Exists(extractedDir))
            throw new DirectoryNotFoundException($"Extracted data not found: {extractedDir}");

        // Read source data for entries that were never opened before touching the
        // output files, so saving over the original archive works.
        var fallback = new Dictionary<int, byte[]>();
        if (File.Exists(DatPath))
        {
            using var sourceDat = File.OpenRead(DatPath);
            var dataReader = new DataReader(null, sourceDat, CurrentReader.Entries);
            for (int i = 0; i < CurrentReader.Entries.Count; i++)
            {
                var filePath = Path.Combine(extractedDir, $"{i:D6}_{CurrentReader.Entries[i].NameHash:X16}.bin");
                if (!File.Exists(filePath))
                    fallback[i] = dataReader.ReadEntryData(CurrentReader.Entries[i]);
            }
        }

        using var fatOut = File.Create(outputFat);
        using var datOut = File.Create(outputDat);

        var writer = new BigFileWriter();

        for (int i = 0; i < CurrentReader.Entries.Count; i++)
        {
            var entry = CurrentReader.Entries[i];
            var filePath = Path.Combine(extractedDir, $"{i:D6}_{entry.NameHash:X16}.bin");

            byte[] data;
            if (File.Exists(filePath))
                data = File.ReadAllBytes(filePath);
            else if (fallback.TryGetValue(i, out var fb))
                data = fb;
            else
                throw new FileNotFoundException($"Extracted entry not found and no source data: {filePath}");

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
