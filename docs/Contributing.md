# contributing

## setup

1. install .net 8 sdk
2. clone the repo
3. `dotnet build` — should build with no errors
4. `dotnet test` — all 66 tests should pass

## project structure

```
TC1.Core/              # format libraries (no gui code at all)
  BigFile/             # fat/dat read/write
  BinaryObject/        # node tree serialization
  Compression/         # lzo1x p/invoke wrapper
  Hashing/             # crc32, crc64 jones
  BinaryObjectTypeHelper.cs

TC1.Studio/            # avalonia gui app
  Hashes/              # hash name databases
    BuiltIn/           # shipped crc mappings (embedded in exe)
    User/              # user-defined names (%APPDATA%)
    Community/         # community name packs (%APPDATA%)
  Plugins/             # IEditorPlugin interface
  Services/            # business logic layer
  ViewModels/          # mvvm state
  Views/               # avalonia xaml ui

TC1.Core.Tests/        # core library unit tests
TC1.Studio.Tests/      # service layer + undo/redo tests
```

## code conventions

- no comments in production code (intent should be clear from method/class names)
- file-scoped namespaces
- mvvm via communitytoolkit.mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`)
- avalonia 11.2 with fluent theme
- services are stateless where possible — state lives in viewmodels
- tests use FluentAssertions for readability

## pr checklist

- [ ] build succeeds
- [ ] all existing tests pass
- [ ] new tests added for any new functionality
- [ ] no game assets included in the repo
- [ ] if adding a hash name, explain how you figured it out

## building a release

```bash
dotnet publish TC1.Studio -c Release -r win-x64 --self-contained true -o dist
```

output is a single `TC1.Studio.exe` (~44 MB) with no runtime dependencies. just runs.
