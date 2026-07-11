using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TC1.Studio.Services;

namespace TC1.Studio.ViewModels;

public class DiffViewModel : ObservableObject
{
    private readonly BinaryObjectService _boService;
    private readonly HashService _hashService;

    public ObservableCollection<DiffItemViewModel> Diffs { get; } = new();

    public bool HasDiffs => Diffs.Count > 0;

    private bool _isVisible;
    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

    public DiffViewModel(BinaryObjectService boService, HashService hashService)
    {
        _boService = boService;
        _hashService = hashService;
    }

    public void Refresh()
    {
        Diffs.Clear();
        var diffs = _boService.ComputeDiff();
        foreach (var d in diffs)
        {
            d.ResolvedName = _hashService.Resolve(d.NameHash);
            Diffs.Add(new DiffItemViewModel(d));
        }
        OnPropertyChanged(nameof(HasDiffs));
        IsVisible = Diffs.Count > 0;
    }

    public void Clear()
    {
        Diffs.Clear();
        OnPropertyChanged(nameof(HasDiffs));
        IsVisible = false;
    }
}

public class DiffItemViewModel
{
    public FieldDiff Diff { get; }
    public DiffItemViewModel(FieldDiff diff) => Diff = diff;

    public string Icon => Diff.Kind switch
    {
        DiffKind.Added => "[+]",
        DiffKind.Removed => "[-]",
        DiffKind.Changed => "[*]",
        _ => "[ ]",
    };

    public string Color => Diff.Kind switch
    {
        DiffKind.Added => "Green",
        DiffKind.Removed => "Red",
        DiffKind.Changed => "Orange",
        _ => "Gray",
    };
}
