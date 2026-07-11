using System.Collections.Generic;

namespace TC1.Core.BinaryObject;

public class Node
{
    public uint NameHash { get; set; }
    public List<Node> Children { get; } = new();
    public Dictionary<uint, byte[]> Fields { get; } = new();

    public Node Clone()
    {
        var c = new Node { NameHash = NameHash };
        foreach (var kv in Fields)
        {
            c.Fields[kv.Key] = kv.Value != null ? (byte[])kv.Value.Clone() : null;
        }
        foreach (var child in Children)
            c.Children.Add(child.Clone());
        return c;
    }
}
