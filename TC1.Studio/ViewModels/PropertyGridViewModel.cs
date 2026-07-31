using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TC1.Core;
using TC1.Core.BinaryObject;
using TC1.Studio.Services;

namespace TC1.Studio.ViewModels;

public partial class PropertyGridViewModel : ObservableObject
{
    private readonly BinaryObjectService _boService;
    private readonly HashService _hashes;
    private readonly CommandStack _undo;
    private ObjectNodeViewModel _currentNode;

    public event Action FieldEdited;
    public event Action HashRenamed;

    [ObservableProperty]
    private string _nodeInfo;

    [ObservableProperty]
    private string _errorMessage;

    public ObservableCollection<FieldViewModel> Fields { get; } = new();

    public PropertyGridViewModel(BinaryObjectService boService, CommandStack undo, HashService hashes)
    {
        _boService = boService;
        _undo = undo;
        _hashes = hashes;
    }

    public void ShowNode(ObjectNodeViewModel node)
    {
        ErrorMessage = null;
        _currentNode = node;
        if (node == null)
        {
            NodeInfo = null;
            Fields.Clear();
            return;
        }

        NodeInfo = $"{node.ResolvedName} (0x{node.NameHash:X8}) — {node.Children.Count} children, {node.Fields.Count} fields";
        Fields.Clear();
        foreach (var f in node.Fields)
            Fields.Add(f);
    }

    [RelayCommand]
    public void ApplyFieldEdit(FieldViewModel field)
    {
        if (field == null) return;

        byte[] newValue;
        try
        {
            newValue = ParseValue(field.EditValue, field.TypeHint);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        if (newValue.AsSpan().SequenceEqual(field.RawValue)) return;

        ErrorMessage = null;
        var oldValue = field.RawValue.ToArray();

        var cmd = new SetFieldCommand(
            _boService.Working,
            field.ParentNode,
            field.NameHash,
            oldValue,
            newValue,
            field.ResolvedName);

        _undo.Execute(cmd);
        field.RawValue = newValue;
        field.DisplayValue = BinaryObjectTypeHelper.FormatValue(newValue, field.TypeHint);
        field.IsModified = true;
        _boService.MarkModified();
        FieldEdited?.Invoke();
    }

    public void RenameFieldHash(FieldViewModel field, string newName)
    {
        if (!ObjectTreeViewModel.IsValidIdentifier(newName)) return;
        if (_hashes.IsKnown(field.NameHash) && !_hashes.IsUserDefined(field.NameHash)) return;

        _hashes.SaveUserHash(field.NameHash, newName);
        field.ResolvedName = newName;
        field.CanRename = false;
        field.OriginTag = "User";
        HashRenamed?.Invoke();
    }

    private static byte[] ParseValue(string text, string type)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Value cannot be empty");

        switch (type)
        {
            case "float":
                if (float.TryParse(text, out var f)) return BitConverter.GetBytes(f);
                throw new ArgumentException($"'{text}' is not a valid float");
            case "float64":
                if (double.TryParse(text, out var d)) return BitConverter.GetBytes(d);
                throw new ArgumentException($"'{text}' is not a valid float64");
            case "byte":
                if (byte.TryParse(text, out var b)) return new[] { b };
                throw new ArgumentException($"'{text}' is not a valid byte");
            case "int16":
                if (short.TryParse(text, out var s)) return BitConverter.GetBytes(s);
                throw new ArgumentException($"'{text}' is not a valid int16");
            case "vec3":
                if (TryParseVec3(text, out var v3)) return v3;
                throw new ArgumentException($"'{text}' is not a valid vec3 (x, y, z)");
            case "vec4":
                if (TryParseVec4(text, out var v4)) return v4;
                throw new ArgumentException($"'{text}' is not a valid vec4 (x, y, z, w)");
            default:
                return ParseHex(text);
        }
    }

    public static byte[] ParseHex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Value cannot be empty");

        var cleaned = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (cleaned.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even number of digits");
        if (cleaned.Length == 0)
            throw new ArgumentException("Value cannot be empty");

        var result = new byte[cleaned.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            var pair = cleaned.Substring(i * 2, 2);
            if (!byte.TryParse(pair, System.Globalization.NumberStyles.HexNumber, null, out var b))
                throw new ArgumentException($"'{pair}' is not a valid hex byte");
            result[i] = b;
        }
        return result;
    }

    public void AddFieldToNode(ObjectNodeViewModel node, uint hash, byte[] value)
    {
        if (node?.SourceNode == null) return;

        var cmd = new AddFieldCommand(node.SourceNode, hash, value, $"0x{hash:X8}");
        _undo.Execute(cmd);

        var fieldVm = ObjectTreeViewModel.CreateFieldViewModel(node.SourceNode, hash, value, _hashes);
        node.Fields.Add(fieldVm);
        if (_currentNode == node)
            Fields.Add(fieldVm);

        _boService.MarkModified();
        FieldEdited?.Invoke();
    }

    public void RemoveField(FieldViewModel field)
    {
        if (field?.ParentNode == null) return;

        var oldValue = field.RawValue != null ? field.RawValue.ToArray() : null;
        var cmd = new RemoveFieldCommand(field.ParentNode, field.NameHash, oldValue, field.ResolvedName);
        _undo.Execute(cmd);

        if (_currentNode != null)
            _currentNode.Fields.Remove(field);
        Fields.Remove(field);

        _boService.MarkModified();
        FieldEdited?.Invoke();
    }

    private static bool TryParseVec3(string text, out byte[] result)
    {
        result = null;
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        if (!float.TryParse(parts[0], out var x)) return false;
        if (!float.TryParse(parts[1], out var y)) return false;
        if (!float.TryParse(parts[2], out var z)) return false;
        result = new byte[12];
        BitConverter.GetBytes(x).CopyTo(result, 0);
        BitConverter.GetBytes(y).CopyTo(result, 4);
        BitConverter.GetBytes(z).CopyTo(result, 8);
        return true;
    }

    private static bool TryParseVec4(string text, out byte[] result)
    {
        result = null;
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        if (!float.TryParse(parts[0], out var x)) return false;
        if (!float.TryParse(parts[1], out var y)) return false;
        if (!float.TryParse(parts[2], out var z)) return false;
        if (!float.TryParse(parts[3], out var w)) return false;
        result = new byte[16];
        BitConverter.GetBytes(x).CopyTo(result, 0);
        BitConverter.GetBytes(y).CopyTo(result, 4);
        BitConverter.GetBytes(z).CopyTo(result, 8);
        BitConverter.GetBytes(w).CopyTo(result, 12);
        return true;
    }
}
