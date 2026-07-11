# testing

uses xUnit + FluentAssertions. tests are split across two projects cause the core library has no GUI deps.

## TC1.Core.Tests (35 tests)

| category | file | what it tests |
|---|---|---|
| crc32 | `Crc32Tests.cs` | known vectors (`RainPrecipitationAmount` = `0x2002CFD9`), empty input, the standard `123456789` test |
| crc64 jones | `Crc64Tests.cs` | determinism, empty, standard vs jones difference |
| binaryobject | `BinaryObjectRoundTripTests.cs` | byte-for-byte round-trip, empty root, many children, various field sizes, unknown field preservation, deep nesting, clone independence |
| bigfile | `BigFileRoundTripTests.cs` | synthetic FAT read/write, bad signature, version 2, zero entries, large offsets, 1000 entries |
| type detection | `BinaryObjectTypeHelperTests.cs` | float, float64, vec3, vec4, byte, int16, bytes[N], format output |

## TC1.Studio.Tests (31 tests)

| category | file | what it tests |
|---|---|---|
| binaryobjectservice | `BinaryObjectServiceTests.cs` | open creates original+working, modify detection, reset-to-original, save, computediff (change, add, remove, child field, multiple) |
| validation | `ValidationServiceTests.cs` | null root, empty file, nan float, infinity float, normal float, nan float64, null field, many children, circular reference, valid file |
| undo/redo | `UndoRedoTests.cs` | initial state, execute/undo/redo, clear, 100 ops, 100 different fields, empty stack safety, changed event |

## running tests

```bash
# all tests
dotnet test

# individual project
dotnet test TC1.Core.Tests
dotnet test TC1.Studio.Tests

# with filter
dotnet test --filter "FullyQualifiedName~Crc32"
```

## writing tests

tests follow arrange-act-assert with FluentAssertions:

```csharp
[Fact]
public void Round_trip_byte_for_byte()
{
    var bo = MakeSimpleFile();
    using var ms1 = new MemoryStream();
    bo.Serialize(ms1);
    var original = ms1.ToArray();

    var bo2 = new BinaryObjectFile();
    using var ms2 = new MemoryStream(original);
    bo2.Deserialize(ms2);

    using var ms3 = new MemoryStream();
    bo2.Serialize(ms3);

    ms3.ToArray().Should().BeEquivalentTo(original);
}
```

## test fixtures

synthetic test data is generated in-memory. no game assets are in the repo. the `TestAssets/` dir can hold regression fixtures for specific bugs — just only use files you're allowed to redistribute.
