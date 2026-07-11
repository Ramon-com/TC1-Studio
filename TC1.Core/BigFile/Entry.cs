using System.Diagnostics;

namespace TC1.Core.BigFile;

[DebuggerDisplay("{NameHash:X16} @{Offset} {CompressedSize}->{UncompressedSize} [{CompressionScheme}]")]
public struct Entry
{
    public ulong NameHash;
    public uint UncompressedSize;
    public uint CompressedSize;
    public long Offset;
    public CompressionScheme CompressionScheme;
}
