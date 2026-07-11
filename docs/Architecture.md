# architecture

## overview

tc1 studio is a gui modding tool for the crew 1. it's split into layers: core (the format stuff) → services (the logic) → viewmodels (state) → views (the actual window).

```mermaid
graph TD
    subgraph "TC1.Core"
        BigFile[BigFile Reader/Writer]
        BinaryObject[BinaryObject Serializer]
        CRC[CRC32 / CRC64 Jones]
        LZO[LZO1x Compression]
        StreamHelpers[Stream Helpers]
    end

    subgraph "TC1.Studio Services"
        ArchiveService[ArchiveService]
        BinaryObjectService[BinaryObjectService]
        HashService[HashService]
        ValidationService[ValidationService]
        CommandStack[CommandStack]
        FileService[FileService]
    end

    subgraph "TC1.Studio ViewModels"
        MainVM[MainWindowViewModel]
        ArchiveExplorerVM[ArchiveExplorerViewModel]
        ObjectTreeVM[ObjectTreeViewModel]
        PropertyGridVM[PropertyGridViewModel]
        DiffVM[DiffViewModel]
    end

    subgraph "TC1.Studio Views"
        MainWindow[MainWindow]
        ArchiveExplorerView[ArchiveExplorerView]
        ObjectTreeView[ObjectTreeView]
        PropertyGridView[PropertyGridView]
        DiffView[DiffView]
    end

    subgraph "Plugins"
        IEditorPlugin[IEditorPlugin]
    end

    BigFile --> ArchiveService
    BinaryObject --> BinaryObjectService
    CRC --> HashService
    LZO --> ArchiveService

    ArchiveService --> ArchiveExplorerVM
    BinaryObjectService --> ObjectTreeVM
    BinaryObjectService --> PropertyGridVM
    BinaryObjectService --> DiffVM
    HashService --> ObjectTreeVM
    HashService --> PropertyGridVM
    CommandStack --> PropertyGridVM
    ValidationService --> MainVM

    MainVM --> MainWindow
    ArchiveExplorerVM --> ArchiveExplorerView
    ObjectTreeVM --> ObjectTreeView
    PropertyGridVM --> PropertyGridView
    DiffVM --> DiffView

    IEditorPlugin --> PropertyGridVM
```

## read pipeline (opening a file)

```mermaid
sequenceDiagram
    participant User
    participant MainWindow
    participant ArchiveService
    participant BigFileReader
    participant BinaryObjectService
    participant ObjectTreeVM

    User->>MainWindow: Open .fat
    MainWindow->>ArchiveService: LoadArchive(fat, dat)
    ArchiveService->>BigFileReader: Deserialize(fat stream)
    BigFileReader-->>ArchiveService: entries list
    ArchiveService-->>MainWindow: entries
    User->>MainWindow: Select entry → Open BinaryObject
    MainWindow->>BinaryObjectService: Open(raw bytes)
    BinaryObjectService->>BinaryObjectService: Deserialize → Original + Working
    BinaryObjectService-->>MainWindow: ready
    MainWindow->>ObjectTreeVM: LoadFromCurrent()
    ObjectTreeVM-->>MainWindow: tree model
```

## write pipeline (saving)

```mermaid
sequenceDiagram
    participant User
    participant MainWindow
    participant BinaryObjectService
    participant ArchiveService
    participant BigFileWriter

    User->>MainWindow: Edit field + Apply
    MainWindow->>BinaryObjectService: MarkModified()
    User->>MainWindow: File → Save
    MainWindow->>BinaryObjectService: Save()
    BinaryObjectService->>BinaryObjectService: Serialize Working → bytes
    BinaryObjectService-->>MainWindow: bytes
    MainWindow->>ArchiveService: Repack(new fat, new dat)
    ArchiveService->>BigFileWriter: Repack(entries with modified data)
    BigFileWriter-->>ArchiveService: new FAT + DAT streams
    ArchiveService-->>MainWindow: done
```

## design decisions (why it works this way)

- **lossless round-trip** — the parser preserves unknown field hashes, raw bytes, and field ordering. edit one field without corrupting anything else.
- **original + working model** — keeps an immutable copy of the file as it was when opened. diff and reset-to-original compare against this snapshot.
- **layered hash database** — builtin (shipped with the app) → user (your appdata folder) → community (shared packs). your custom names never touch the shipped files.
- **command pattern** — every edit gets pushed to a command stack. undo/redo just reverses field assignments. simple.
- **plugin isolation** — plugins return an avalonia Control. the main app provides the frame, plugins provide the actual editor controls.
