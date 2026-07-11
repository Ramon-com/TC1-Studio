using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TC1.Studio.Services;

namespace TC1.Studio.ViewModels;

public partial class ArchiveEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ulong _nameHash;

    [ObservableProperty]
    private long _offset;

    [ObservableProperty]
    private uint _compressedSize;

    [ObservableProperty]
    private uint _uncompressedSize;

    [ObservableProperty]
    private string _compressionScheme;

    [ObservableProperty]
    private bool _isOpen;
}

public partial class ArchiveExplorerViewModel : ObservableObject
{
    private readonly ArchiveService _archive;
    private readonly FileService _files;
    private readonly BinaryObjectService _boService;
    private readonly HashService _hashes;

    [ObservableProperty]
    private string _archiveName;

    [ObservableProperty]
    private ArchiveEntryViewModel _selectedEntry;

    public ObservableCollection<ArchiveEntryViewModel> Entries { get; } = new();

    public ArchiveExplorerViewModel(ArchiveService archive, FileService files, BinaryObjectService boService, HashService hashes)
    {
        _archive = archive;
        _files = files;
        _boService = boService;
        _hashes = hashes;
    }

    public void LoadArchive(string fatPath, string datPath)
    {
        _archive.Open(fatPath, datPath);
        ArchiveName = Path.GetFileName(fatPath);

        Entries.Clear();
        for (int i = 0; i < _archive.CurrentReader.Entries.Count; i++)
        {
            var e = _archive.CurrentReader.Entries[i];
            Entries.Add(new ArchiveEntryViewModel
            {
                Index = i,
                Name = _hashes.Resolve(e.NameHash),
                NameHash = e.NameHash,
                Offset = e.Offset,
                CompressedSize = e.CompressedSize,
                UncompressedSize = e.UncompressedSize,
                CompressionScheme = e.CompressionScheme.ToString(),
            });
        }
    }

    [RelayCommand]
    public void OpenSelectedEntry()
    {
        var entry = SelectedEntry;
        if (entry == null) return;

        try
        {
            var data = _archive.ExtractEntry(entry.Index);
            _files.WriteExtractedFile(_archive.DataDir, entry.Index, entry.NameHash, data);
            _boService.Open(data);
            entry.IsOpen = true;
            StatusMessage?.Invoke($"Opened entry {entry.Index}: {entry.Name}");
            ObjectTreeOpened?.Invoke(entry.NameHash);
        }
        catch (Exception ex)
        {
            StatusMessage?.Invoke($"Error opening entry: {ex.Message}");
        }
    }

    public event Action<ulong> ObjectTreeOpened;
    public event Action<string> StatusMessage;
}
