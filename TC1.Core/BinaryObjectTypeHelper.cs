using System;

namespace TC1.Core;

public static class BinaryObjectTypeHelper
{
    public static string DetectType(byte[] data)
    {
        if (data == null || data.Length == 0) return "empty";
        if (data.Length == 4) return "float";
        if (data.Length == 1) return "byte";
        if (data.Length == 2) return "int16";
        if (data.Length == 8)
        {
            var d = BitConverter.ToDouble(data);
            if (double.IsFinite(d) && (d == 0 || Math.Abs(d) is < 1e20 and > 1e-20 || Math.Abs(Math.Abs(d) - 1) < 0.001))
                return "float64";
            return "bytes[8]";
        }
        if (data.Length == 12)
        {
            var f1 = BitConverter.ToSingle(data, 0);
            var f2 = BitConverter.ToSingle(data, 4);
            var f3 = BitConverter.ToSingle(data, 8);
            if (IsLikelyFloat(f1) && IsLikelyFloat(f2) && IsLikelyFloat(f3))
                return "vec3";
            return "bytes[12]";
        }
        if (data.Length == 16)
        {
            var f1 = BitConverter.ToSingle(data, 0);
            var f2 = BitConverter.ToSingle(data, 4);
            var f3 = BitConverter.ToSingle(data, 8);
            var f4 = BitConverter.ToSingle(data, 12);
            if (IsLikelyFloat(f1) && IsLikelyFloat(f2) && IsLikelyFloat(f3) && IsLikelyFloat(f4))
                return "vec4";
            return "bytes[16]";
        }
        if (data.Length == 3) return "vec3_byte";
        return $"bytes[{data.Length}]";
    }

    public static string FormatValue(byte[] data, string type)
    {
        if (data == null || data.Length == 0) return "(empty)";
        try
        {
            return type switch
            {
                "float" => BitConverter.ToSingle(data).ToString("G"),
                "float64" => BitConverter.ToDouble(data).ToString("G"),
                "byte" => data[0].ToString(),
                "int16" => BitConverter.ToInt16(data).ToString(),
                "vec3" => $"{BitConverter.ToSingle(data, 0):G}, {BitConverter.ToSingle(data, 4):G}, {BitConverter.ToSingle(data, 8):G}",
                "vec4" => $"{BitConverter.ToSingle(data, 0):G}, {BitConverter.ToSingle(data, 4):G}, {BitConverter.ToSingle(data, 8):G}, {BitConverter.ToSingle(data, 12):G}",
                _ => BitConverter.ToString(data).Replace("-", " "),
            };
        }
        catch
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }
    }

    private static bool IsLikelyFloat(float f) =>
        float.IsFinite(f) && (f == 0 || Math.Abs(f) < 1e10f);
}
