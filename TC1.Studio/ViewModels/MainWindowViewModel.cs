using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TC1.Studio.Services;

namespace TC1.Studio.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ArchiveExplorerViewModel ArchiveExplorer { get; }
    public ObjectTreeViewModel ObjectTree { get; }
    public PropertyGridViewModel PropertyGrid { get; }
    public DiffViewModel DiffView { get; }
    public CommandStack Undo { get; } = new();

    private readonly ArchiveService _archive;
    private readonly BinaryObjectService _boService;
    private readonly ValidationService _validation;
    private readonly FileService _files;
    private readonly HashService _hashes;

    [ObservableProperty]
    private string _statusText = "No archive loaded";

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _title = "TC1 Studio";

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private bool _hasDocument;

    [ObservableProperty]
    private bool _hasValidationWarnings;

    public ObservableCollection<ValidationResult> ValidationResults { get; } = new();

    public Func<Task<string>> RequestOpenFile { get; set; }
    public Func<Task<string>> RequestSaveFile { get; set; }
    public Func<Task<string>> RequestImportHashPack { get; set; }
    public Func<Task<string>> RequestExportHashPack { get; set; }

    public MainWindowViewModel()
    {
        _hashes = new HashService();
        var files = new FileService();
        _archive = new ArchiveService();
        _boService = new BinaryObjectService();
        _validation = new ValidationService();
        _files = files;

        ArchiveExplorer = new ArchiveExplorerViewModel(_archive, files, _boService, _hashes);
        ObjectTree = new ObjectTreeViewModel(_boService, _hashes);
        PropertyGrid = new PropertyGridViewModel(_boService, Undo, _hashes);
        DiffView = new DiffViewModel(_boService, _hashes);

        ArchiveExplorer.ObjectTreeOpened += _ =>
        {
            ObjectTree.LoadFromCurrent();
            Undo.Clear();
            UpdateState();
            StatusText = $"Opened document";
            DiffView.Clear();
            RunValidation();
        };

        ArchiveExplorer.StatusMessage += msg =>
        {
            StatusText = msg;
            UpdateState();
        };

        ObjectTree.NodeSelected += node =>
        {
            PropertyGrid.ShowNode(node);
        };

        Undo.Changed += () =>
        {
            UpdateState();
        };

        PropertyGrid.FieldEdited += () =>
        {
            DiffView.Refresh();
            RunValidation();
        };

        PropertyGrid.HashRenamed += () =>
        {
            ObjectTree.LoadFromCurrent();
            PropertyGrid.Fields.Clear();
        };

        ObjectTree.HashRenamed += () =>
        {
            PropertyGrid.Fields.Clear();
        };
    }

    private void UpdateState()
    {
        IsModified = _boService.IsModified || _archive.IsModified;
        Title = IsModified ? "TC1 Studio *" : "TC1 Studio";
        CanUndo = Undo.CanUndo;
        CanRedo = Undo.CanRedo;
        HasDocument = _boService.Working != null;
    }

    private void RunValidation()
    {
        ValidationResults.Clear();
        if (_boService.Working != null)
        {
            var results = _validation.Validate(_boService.Working);
            foreach (var r in results)
                ValidationResults.Add(r);
            HasValidationWarnings = results.Count > 0;
            if (results.Count > 0)
                StatusText = $"{results.Count} validation { (results.Count == 1 ? "warning" : "warnings") }";
        }
        else
        {
            HasValidationWarnings = false;
        }
    }

    [RelayCommand]
    public async Task OpenArchive()
    {
        if (RequestOpenFile == null) return;

        var path = await RequestOpenFile();
        if (string.IsNullOrEmpty(path)) return;

        var (fat, dat) = _files.FindArchivePair(path);
        if (fat == null)
        {
            StatusText = "Invalid archive file";
            return;
        }

        try
        {
            ArchiveExplorer.LoadArchive(fat, dat);
            Undo.Clear();
            UpdateState();
            StatusText = $"Loaded {Path.GetFileName(fat)} ({ArchiveExplorer.Entries.Count} entries)";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveArchive()
    {
        if (RequestSaveFile == null) return;

        try
        {
            // 1. Save current BinaryObject back to extract dir
            if (_boService.Working != null)
            {
                var bytes = _boService.Save();
                var selectedEntry = ArchiveExplorer.SelectedEntry;
                if (selectedEntry != null)
                {
                    var dir = _archive.DataDir;
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{selectedEntry.Index:D6}_{selectedEntry.NameHash:X16}.bin");
                    File.WriteAllBytes(path, bytes);
                    StatusText = $"Saved {selectedEntry.Name}";
                }
            }

            // 2. Repack entire archive
            var savePath = await RequestSaveFile();
            if (string.IsNullOrEmpty(savePath)) return;

            var datPath = Path.ChangeExtension(savePath, ".dat");
            _archive.Repack(savePath, datPath);

            _boService.IsModified = false;
            _archive.IsModified = false;
            Undo.Clear();
            UpdateState();
            StatusText = $"Saved to {Path.GetFileName(savePath)}";
            DiffView.Clear();
            ValidationResults.Clear();
            HasValidationWarnings = false;
        }
        catch (Exception ex)
        {
            StatusText = $"Save error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void UndoCommand() => Undo.Undo();

    [RelayCommand]
    public void RedoCommand() => Undo.Redo();

    [RelayCommand]
    public void ShowDiff()
    {
        DiffView.Refresh();
        DiffView.IsVisible = !DiffView.IsVisible;
    }

    [RelayCommand]
    public async Task ImportHashPack()
    {
        if (RequestImportHashPack == null) return;
        var path = await RequestImportHashPack();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var count = _hashes.ImportPack(path);
            if (count == 0)
            {
                StatusText = "No new names in that pack";
                return;
            }
            ObjectTree.LoadFromCurrent();
            PropertyGrid.Fields.Clear();
            StatusText = $"Imported {count} names from {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ExportHashPack()
    {
        if (RequestExportHashPack == null) return;
        var path = await RequestExportHashPack();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var entries = _hashes.GetEntries();
            HashService.WritePack(path, entries);
            StatusText = $"Exported {entries.Count} names to {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ResetToOriginal()
    {
        _boService.ResetToOriginal();
        ObjectTree.LoadFromCurrent();
        PropertyGrid.Fields.Clear();
        Undo.Clear();
        DiffView.Clear();
        ValidationResults.Clear();
        HasValidationWarnings = false;
        UpdateState();
        StatusText = "Reset to original";
    }

    [RelayCommand]
    public void CloseArchive()
    {
        ArchiveExplorer.Entries.Clear();
        ObjectTree.RootNodes.Clear();
        PropertyGrid.Fields.Clear();
        Undo.Clear();
        _boService.IsModified = false;
        _archive.IsModified = false;
        UpdateState();
        StatusText = "No archive loaded";
        DiffView.Clear();
        ValidationResults.Clear();
        HasValidationWarnings = false;
    }
}
