namespace TC1.Core.BigFile;

public enum CompressionScheme : byte
{
    None = 0,
    LZO1x = 1,
    Zlib = 2,
    Oodle = 3,
}
