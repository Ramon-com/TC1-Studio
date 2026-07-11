using System;
using System.Runtime.InteropServices;

namespace TC1.Core.Compression;

public static class LZO
{
    private const string DllName = "lzo_64.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lzo_init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lzo1x_decompress_safe(
        byte[] src, int src_len,
        byte[] dst, ref int dst_len,
        byte[] wrkmem);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lzo1x_1_compress(
        byte[] src, int src_len,
        byte[] dst, ref int dst_len,
        byte[] wrkmem);

    private static readonly bool _initialized;
    private static readonly byte[] _workMemory = new byte[16384L * 4];

    static LZO()
    {
        try
        {
            _initialized = lzo_init() == 0;
        }
        catch
        {
            _initialized = false;
        }
    }

    public static bool IsAvailable => _initialized;

    public static byte[] Decompress(byte[] compressed, int compressedSize, int decompressedSize)
    {
        if (!_initialized)
            throw new InvalidOperationException("LZO library not available");

        var result = new byte[decompressedSize];
        int dstLen = decompressedSize;
        int ret = lzo1x_decompress_safe(compressed, compressedSize, result, ref dstLen, _workMemory);
        if (ret != 0)
            throw new InvalidOperationException($"LZO decompression failed with code {ret}");
        return result;
    }

    public static byte[] Compress(byte[] data)
    {
        if (!_initialized)
            throw new InvalidOperationException("LZO library not available");

        var compressed = new byte[data.Length + data.Length / 16 + 64 + 3];
        int dstLen = compressed.Length;
        int ret = lzo1x_1_compress(data, data.Length, compressed, ref dstLen, _workMemory);
        if (ret != 0)
            throw new InvalidOperationException($"LZO compression failed with code {ret}");

        var result = new byte[dstLen];
        Array.Copy(compressed, result, dstLen);
        return result;
    }
}
