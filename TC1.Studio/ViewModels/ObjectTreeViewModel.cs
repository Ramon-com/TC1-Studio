using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TC1.Core;
using TC1.Core.BinaryObject;
using TC1.Studio.Services;

namespace TC1.Studio.ViewModels;

public enum FieldConfidence { Known, Inferred, Unknown }

public partial class ObjectNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private uint _nameHash;

    [ObservableProperty]
    private string _resolvedName;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private FieldConfidence _confidence;

    [ObservableProperty]
    private string _originTag;

    [ObservableProperty]
    private bool _canRename;

    public ObservableCollection<ObjectNodeViewModel> Children { get; } = new();
    public ObservableCollection<FieldViewModel> Fields { get; } = new();
}

public partial class FieldViewModel : ObservableObject
{
    [ObservableProperty]
    private uint _nameHash;

    [ObservableProperty]
    private string _resolvedName;

    [ObservableProperty]
    private byte[] _rawValue;

    [ObservableProperty]
    private string _typeHint;

    [ObservableProperty]
    private string _displayValue;

    [ObservableProperty]
    private string _editValue;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private FieldConfidence _confidence;

    [ObservableProperty]
    private string _editorType = "TextBox";

    [ObservableProperty]
    private string _originTag;

    [ObservableProperty]
    private bool _canRename;

    public Node ParentNode { get; set; }
    public uint ParentNodeHash { get; set; }
}

public partial class ObjectTreeViewModel : ObservableObject
{
    private readonly BinaryObjectService _boService;
    private readonly HashService _hashes;

    public ObservableCollection<ObjectNodeViewModel> RootNodes { get; } = new();

    [ObservableProperty]
    private ObjectNodeViewModel _selectedNode;

    public event Action<ObjectNodeViewModel> NodeSelected;
    public event Action HashRenamed;

    public ObjectTreeViewModel(BinaryObjectService boService, HashService hashes)
    {
        _boService = boService;
        _hashes = hashes;
    }

    public void LoadFromCurrent()
    {
        if (_boService.Working == null) return;
        RootNodes.Clear();
        RootNodes.Add(BuildNode(_boService.Working.Root));
        SelectedNode = null;
    }

    private ObjectNodeViewModel BuildNode(TC1.Core.BinaryObject.Node node)
    {
        var nameKnown = _hashes.TryResolve(node.NameHash, out _);
        var vm = new ObjectNodeViewModel
        {
            NameHash = node.NameHash,
            ResolvedName = _hashes.Resolve(node.NameHash),
            Confidence = nameKnown ? FieldConfidence.Known : FieldConfidence.Inferred,
            CanRename = !nameKnown,
            OriginTag = GetOriginTag(node.NameHash),
        };

        foreach (var child in node.Children)
            vm.Children.Add(BuildNode(child));

        foreach (var kv in node.Fields)
        {
            var typeHint = BinaryObjectTypeHelper.DetectType(kv.Value);
            var display = BinaryObjectTypeHelper.FormatValue(kv.Value, typeHint);
            var fieldKnown = _hashes.TryResolve(kv.Key, out _);
            vm.Fields.Add(new FieldViewModel
            {
                NameHash = kv.Key,
                ResolvedName = _hashes.Resolve(kv.Key),
                RawValue = kv.Value,
                TypeHint = typeHint,
                DisplayValue = display,
                EditValue = display,
                Confidence = fieldKnown ? FieldConfidence.Known : (typeHint != "bytes" ? FieldConfidence.Inferred : FieldConfidence.Unknown),
                EditorType = GetEditorType(typeHint, kv.Value),
                ParentNode = node,
                ParentNodeHash = node.NameHash,
                CanRename = !fieldKnown,
                OriginTag = GetOriginTag(kv.Key),
            });
        }

        return vm;
    }

    private string GetOriginTag(uint hash)
    {
        if (_hashes.TryGetEntry(hash, out var entry))
            return entry.Origin switch
            {
                HashOrigin.BuiltIn => "Built-in",
                HashOrigin.User => "User",
                HashOrigin.Community => "Community",
                _ => null,
            };
        return null;
    }

    private string GetOriginTag(ulong hash)
    {
        if (_hashes.TryGetEntry(hash, out var entry))
            return entry.Origin switch
            {
                HashOrigin.BuiltIn => "Built-in",
                HashOrigin.User => "User",
                HashOrigin.Community => "Community",
                _ => null,
            };
        return null;
    }

    public bool RenameHash(string hexHash, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return false;
        if (!IsValidIdentifier(newName)) return false;

        if (hexHash.StartsWith("0x") || hexHash.StartsWith("0X"))
        {
            var hex = hexHash.Substring(2);
            if (hex.Length == 8 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var u32))
            {
                // Check it's not already known as a different name
                if (_hashes.IsKnown(u32) && !_hashes.IsUserDefined(u32))
                    return false;

                _hashes.SaveUserHash(u32, newName);
                LoadFromCurrent();
                HashRenamed?.Invoke();
                return true;
            }
        }
        return false;
    }

    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        for (int i = 1; i < name.Length; i++)
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        return true;
    }

    [RelayCommand]
    public void SelectNode(ObjectNodeViewModel node)
    {
        SelectedNode = node;
        NodeSelected?.Invoke(node);
    }

    private static string GetEditorType(string typeHint, byte[] data)
    {
        return typeHint switch
        {
            "float" => "FloatBox",
            "float64" => "FloatBox",
            "byte" => "IntBox",
            "int16" => "IntBox",
            "vec3" => "VecBox",
            "vec4" => "VecBox",
            "empty" => "None",
            _ => "HexBox",
        };
    }
}
