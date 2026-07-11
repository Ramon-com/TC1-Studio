# BigFile format

the crew 1 uses bigfile containers for its archives. basically like a zip file but custom. archives come as a pair: `.fat` (the table of contents) and `.dat` (the actual data).

## spec

| field | description |
|---|---|
| **signature** | `0x46415432` (`FAT2`) |
| **version** | 5 (tc1), can be 2–9 |
| **platform** | 0 = PC (little-endian), 1 = PS3, 2 = Xbox 360, 3 = Wii U |
| **endianness** | little-endian (PC platform) |

## FAT header

```
offset  size  field
0x00    4     signature ('FAT2')
0x04    4     version
0x08    4     platform (version >= 3)
0x0C    4     entry count
0x10         entries (32 bytes each, version 5)
```

for version 7+, there's extra stuff after the entry list (another count + 16-byte records).

## entry structure (version 5)

each entry is 32 bytes:

| offset | size | field |
|---|---|---|
| 0x00 | 4 | `a` = packed flags + uncompressed size |
| 0x04 | 4 | author (always 0 in tc1) |
| 0x08 | 8 | name hash (crc64 jones) |
| 0x10 | 4 | `c` = packed compressed size + offset bits |
| 0x14 | 4 | `d` = offset high bits |

### field packing (this is the annoying part)

**`a` (uint32):**
- bits 0–1: compression scheme (0 = none, 1 = lzo1x, 2 = zlib, 3 = oodle)
- bits 2–31: uncompressed size >> 2

**`c` (uint32):**
- bits 0–29: compressed size
- bits 30–31: offset bits 0–1

**`d` (uint32):**
- bits 0–31: offset >> 2

**final offset:** `(d << 2) | ((c >> 30) & 3)` — a 34-bit value. yeah, 34 bit.

## compression

| scheme | value | notes |
|---|---|---|
| none | 0 | raw data, uncompressed size = 0 |
| lzo1x | 1 | via `lzo_64.dll` (comes with the game) |
| zlib | 2 | not supported for repack |
| oodle | 3 | not supported for repack |

## implementation

- `TC1.Core.BigFile.BigFileReader` — reads fat stream, gives you entries
- `TC1.Core.BigFile.BigFileWriter` — writes entries back to fat stream, handles repack
- `TC1.Core.BigFile.DataReader` — pulls entry data from dat with optional decompression
- `TC1.Core.Compression.LZO` — p/invoke wrapper for `lzo_64.dll`
