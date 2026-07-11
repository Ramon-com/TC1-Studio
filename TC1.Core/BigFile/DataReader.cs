using System;
using System.Collections.Generic;
using System.IO;

namespace TC1.Core.BigFile;

public class DataReader
{
    private readonly Stream _fatStream;
    private readonly Stream _datStream;
    private readonly List<Entry> _entries;

    public DataReader(Stream fatStream, Stream datStream, List<Entry> entries)
    {
        _fatStream = fatStream;
        _datStream = datStream;
        _entries = entries;
    }

    public byte[] ReadEntryData(Entry entry)
    {
        _datStream.Position = entry.Offset;

        if (entry.CompressionScheme == CompressionScheme.None)
        {
            var data = new byte[entry.CompressedSize];
            _datStream.ReadExactly(data);
            return data;
        }

        var compressed = new byte[entry.CompressedSize];
        _datStream.ReadExactly(compressed);

        return Compression.LZO.Decompress(compressed, (int)entry.CompressedSize, (int)entry.UncompressedSize);
    }
}
