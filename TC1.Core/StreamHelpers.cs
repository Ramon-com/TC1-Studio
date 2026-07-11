using System;
using System.IO;
using System.Text;

namespace TC1.Core;

public static class StreamHelpers
{
    public static ushort ReadValueU16(this Stream stream)
    {
        Span<byte> buf = stackalloc byte[2];
        stream.ReadExactly(buf);
        return (ushort)(buf[0] | (buf[1] << 8));
    }

    public static uint ReadValueU32(this Stream stream)
    {
        Span<byte> buf = stackalloc byte[4];
        stream.ReadExactly(buf);
        return (uint)(buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24));
    }

    public static ulong ReadValueU64(this Stream stream)
    {
        Span<byte> buf = stackalloc byte[8];
        stream.ReadExactly(buf);
        return (ulong)buf[0] | ((ulong)buf[1] << 8) | ((ulong)buf[2] << 16) | ((ulong)buf[3] << 24) |
               ((ulong)buf[4] << 32) | ((ulong)buf[5] << 40) | ((ulong)buf[6] << 48) | ((ulong)buf[7] << 56);
    }

    public static float ReadValueF32(this Stream stream)
    {
        var val = stream.ReadValueU32();
        return BitConverter.Int32BitsToSingle((int)val);
    }

    public static void WriteValueU16(this Stream stream, ushort value)
    {
        Span<byte> buf = stackalloc byte[2];
        buf[0] = (byte)value;
        buf[1] = (byte)(value >> 8);
        stream.Write(buf);
    }

    public static void WriteValueU32(this Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        buf[0] = (byte)value;
        buf[1] = (byte)(value >> 8);
        buf[2] = (byte)(value >> 16);
        buf[3] = (byte)(value >> 24);
        stream.Write(buf);
    }

    public static void WriteValueU64(this Stream stream, ulong value)
    {
        Span<byte> buf = stackalloc byte[8];
        buf[0] = (byte)value;
        buf[1] = (byte)(value >> 8);
        buf[2] = (byte)(value >> 16);
        buf[3] = (byte)(value >> 24);
        buf[4] = (byte)(value >> 32);
        buf[5] = (byte)(value >> 40);
        buf[6] = (byte)(value >> 48);
        buf[7] = (byte)(value >> 56);
        stream.Write(buf);
    }

    public static void WriteValueF32(this Stream stream, float value)
    {
        var val = BitConverter.SingleToInt32Bits(value);
        stream.WriteValueU32((uint)val);
    }

    public static uint ReadCount(this Stream stream, out bool isOffset)
    {
        int b = stream.ReadByte();
        if (b < 0) throw new EndOfStreamException();

        isOffset = false;

        if (b < 0xFE)
            return (uint)b;

        isOffset = b != 0xFF;
        return stream.ReadValueU32();
    }

    public static void WriteCount(this Stream stream, uint value, bool isOffset)
    {
        if (isOffset || value >= 0xFE)
        {
            stream.WriteByte((byte)(isOffset ? 0xFE : 0xFF));
            stream.WriteValueU32(value);
        }
        else
        {
            stream.WriteByte((byte)(value & 0xFF));
        }
    }

    public static uint ReadPackedU32(this Stream stream)
    {
        int b = stream.ReadByte();
        if (b < 0) throw new EndOfStreamException();

        if (b < 0xFE)
            return (uint)b;

        if (b == 0xFE)
            throw new FormatException("Unexpected packed marker 0xFE");

        return stream.ReadValueU32();
    }

    public static string ReadStringASCII(this Stream stream, int length)
    {
        Span<byte> buf = length <= 256 ? stackalloc byte[length] : new byte[length];
        stream.ReadExactly(buf);
        int term = buf.IndexOf((byte)0);
        return Encoding.ASCII.GetString(buf.Slice(0, term >= 0 ? term : length));
    }

    public static void WriteStringASCII(this Stream stream, string value, int fixedLength)
    {
        Span<byte> buf = fixedLength <= 2048 ? stackalloc byte[fixedLength] : new byte[fixedLength];
        buf.Clear();
        int count = Math.Min(value.Length, fixedLength);
        Encoding.ASCII.GetBytes(value.AsSpan(0, count), buf);
        stream.Write(buf);
    }
}
