using System;
using System.Collections.Generic;
using System.IO;
using TC1.Core;
using TC1.Core.BinaryObject;

namespace TC1.Studio.Services;

public enum DiffKind { Unchanged, Added, Removed, Changed }

public class FieldDiff
{
    public uint NameHash { get; set; }
    public string ResolvedName { get; set; }
    public string TypeHint { get; set; }
    public byte[] OldValue { get; set; }
    public byte[] NewValue { get; set; }
    public DiffKind Kind { get; set; }
    public string OldDisplay { get; set; }
    public string NewDisplay { get; set; }
}

public class BinaryObjectService
{
    public BinaryObjectFile Original { get; private set; }
    public BinaryObjectFile Working { get; private set; }
    public bool IsModified { get; set; }

    public void Open(byte[] data)
    {
        using var ms = new MemoryStream(data);
        var f = new BinaryObjectFile();
        f.Deserialize(ms);
        Original = f.Clone();
        Working = f;
        IsModified = false;
    }

    public byte[] Save()
    {
        using var ms = new MemoryStream();
        Working.Serialize(ms);
        var result = ms.ToArray();
        IsModified = !BytesEqual(Original, Working);
        return result;
    }

    public void MarkModified()
    {
        IsModified = true;
    }

    public void ResetToOriginal()
    {
        Working = Original.Clone();
        IsModified = false;
    }

    public List<FieldDiff> ComputeDiff()
    {
        var diffs = new List<FieldDiff>();
        if (Original == null || Working == null) return diffs;
        DiffNodes(Original.Root, Working.Root, diffs);
        return diffs;
    }

    private void DiffNodes(Node a, Node b, List<FieldDiff> diffs)
    {
        if (a == null || b == null) return;

        var allKeys = new HashSet<uint>(a.Fields.Keys);
        foreach (var k in b.Fields.Keys) allKeys.Add(k);

        foreach (var key in allKeys)
        {
            a.Fields.TryGetValue(key, out var av);
            b.Fields.TryGetValue(key, out var bv);

            if (av == null && bv == null) continue;

            var typeHint = "";
            var oldDisplay = "";
            var newDisplay = "";
            DiffKind kind;

            if (av == null) { kind = DiffKind.Added; bv ??= Array.Empty<byte>(); }
            else if (bv == null) { kind = DiffKind.Removed; av ??= Array.Empty<byte>(); }
            else
            {
                typeHint = BinaryObjectTypeHelper.DetectType(av);
                oldDisplay = BinaryObjectTypeHelper.FormatValue(av, typeHint);
                newDisplay = BinaryObjectTypeHelper.FormatValue(bv, typeHint);
                kind = av.AsSpan().SequenceEqual(bv) ? DiffKind.Unchanged : DiffKind.Changed;
            }

            if (kind != DiffKind.Unchanged)
            {
                diffs.Add(new FieldDiff
                {
                    NameHash = key,
                    ResolvedName = "", // caller resolves
                    TypeHint = typeHint,
                    OldValue = av,
                    NewValue = bv,
                    Kind = kind,
                    OldDisplay = oldDisplay,
                    NewDisplay = newDisplay,
                });
            }
        }

        int max = Math.Max(a.Children.Count, b.Children.Count);
        for (int i = 0; i < max; i++)
        {
            var ca = i < a.Children.Count ? a.Children[i] : null;
            var cb = i < b.Children.Count ? b.Children[i] : null;
            if (ca != null && cb != null)
                DiffNodes(ca, cb, diffs);
        }
    }

    private static bool BytesEqual(BinaryObjectFile a, BinaryObjectFile b)
    {
        using var ma = new MemoryStream();
        using var mb = new MemoryStream();
        a.Serialize(ma);
        b.Serialize(mb);
        return ma.ToArray().AsSpan().SequenceEqual(mb.ToArray());
    }
}
