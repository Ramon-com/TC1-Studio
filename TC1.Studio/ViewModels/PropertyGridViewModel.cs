using System;
using System.Collections.ObjectModel;
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

    public event Action FieldEdited;
    public event Action HashRenamed;

    [ObservableProperty]
    private string _nodeInfo;

    public ObservableCollection<FieldViewModel> Fields { get; } = new();

    public PropertyGridViewModel(BinaryObjectService boService, CommandStack undo, HashService hashes)
    {
        _boService = boService;
        _undo = undo;
        _hashes = hashes;
    }

    public void ShowNode(ObjectNodeViewModel node)
    {
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

        var newValue = ParseValue(field.EditValue, field.TypeHint);
        if (newValue == null) return;

        if (newValue.AsSpan().SequenceEqual(field.RawValue)) return;

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
        try
        {
            return type switch
            {
                "float" when float.TryParse(text, out var f) => BitConverter.GetBytes(f),
                "float64" when double.TryParse(text, out var d) => BitConverter.GetBytes(d),
                "byte" when byte.TryParse(text, out var b) => new[] { b },
                "int16" when short.TryParse(text, out var s) => BitConverter.GetBytes(s),
                "vec3" when TryParseVec3(text, out var v) => v,
                "vec4" when TryParseVec4(text, out var v) => v,
                _ => null,
            };
        }
        catch
        {
            return null;
        }
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
