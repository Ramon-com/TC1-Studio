using System;
using System.Collections.Generic;
using TC1.Core.BinaryObject;

namespace TC1.Studio.Services;

public enum ValidationSeverity { Info, Warning, Error }

public class ValidationResult
{
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; }
    public uint? FieldHash { get; set; }
    public string NodePath { get; set; }
}

public class ValidationService
{
    public List<ValidationResult> Validate(BinaryObjectFile file)
    {
        var results = new List<ValidationResult>();
        if (file?.Root == null) return results;
        ValidateNode(file.Root, "root", results, new HashSet<object>());
        return results;
    }

    private void ValidateNode(Node node, string path, List<ValidationResult> results, HashSet<object> visited)
    {
        if (node == null) return;
        if (!visited.Add(node)) { results.Add(new ValidationResult { Severity = ValidationSeverity.Error, Message = $"Circular reference at {path}", NodePath = path }); return; }

        if (node.Children.Count > 500)
            results.Add(new ValidationResult { Severity = ValidationSeverity.Warning, Message = $"Node has {node.Children.Count} children (possible corruption)", NodePath = path });

        foreach (var kv in node.Fields)
        {
            var fieldPath = $"{path}.0x{kv.Key:X8}";
            ValidateField(kv.Key, kv.Value, fieldPath, results);
        }

        for (int i = 0; i < node.Children.Count; i++)
            ValidateNode(node.Children[i], $"{path}[{i}]", results, visited);
    }

    private void ValidateField(uint hash, byte[] data, string fieldPath, List<ValidationResult> results)
    {
        if (data == null)
        {
            results.Add(new ValidationResult { Severity = ValidationSeverity.Error, Message = $"Null field data at {fieldPath}", FieldHash = hash });
            return;
        }

        if (data.Length == 0) return;

        // Alignment checks
        int align = data.Length switch
        {
            4 => 4,
            8 => 4,
            12 => 4,
            16 => 4,
            _ => 1
        };

        // Range warnings for floats
        if (data.Length == 4)
        {
            var val = BitConverter.ToSingle(data);
            if (float.IsNaN(val))
                results.Add(new ValidationResult { Severity = ValidationSeverity.Warning, Message = $"NaN float at {fieldPath}", FieldHash = hash });
            else if (float.IsInfinity(val))
                results.Add(new ValidationResult { Severity = ValidationSeverity.Warning, Message = $"Infinity float at {fieldPath}", FieldHash = hash });
        }

        if (data.Length == 8)
        {
            var val = BitConverter.ToDouble(data);
            if (double.IsNaN(val))
                results.Add(new ValidationResult { Severity = ValidationSeverity.Warning, Message = $"NaN float64 at {fieldPath}", FieldHash = hash });
        }
    }
}
