using TC1.Core.Hashing;

var testResults = new List<string>();

// CRC32 tests
uint crc32a = CRC32.Hash("hello");
uint crc32b = CRC32.Hash("RainPrecipitationAmount");
uint crc32c = CRC32.Hash("WetMaterialFactor");
uint crc32d = CRC32.Hash("WetPuddleFactor");

testResults.Add($"CRC32('hello') = 0x{crc32a:X8} (expected 0x3610A686)");
testResults.Add($"CRC32('RainPrecipitationAmount') = 0x{crc32b:X8} (expected 0x2002CFD9)");
testResults.Add($"CRC32('WetMaterialFactor') = 0x{crc32c:X8}");
testResults.Add($"CRC32('WetPuddleFactor') = 0x{crc32d:X8}");

// CRC64 Jones tests
ulong crc64a = CRC64.Hash("", true);
ulong crc64b = CRC64.Hash("global_db_patch_1", true);
ulong crc64c = CRC64.Hash("archetypes.entities.bin", true);

testResults.Add($"CRC64 Jones('') = 0x{crc64a:X16} (expected 0x0000000000000000)");
testResults.Add($"CRC64 Jones('global_db_patch_1') = 0x{crc64b:X16}");
testResults.Add($"CRC64 Jones('archetypes.entities.bin') = 0x{crc64c:X16}");

foreach (var r in testResults)
    Console.WriteLine(r);
