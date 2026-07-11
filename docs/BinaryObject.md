# BinaryObject format

BinaryObject files (`.bin` inside bigfile archives) hold structured game data — like entity configs, weather settings, vehicle stats, ui layouts. basically where the game keeps all its tunables.

## header

| offset | size | field |
|---|---|---|
| 0x00 | 4 | signature (`0x4643626E` = `FCbn`) |
| 0x04 | 4 | version (2 in tc1) |
| 0x08 | 4 | flags |
| 0x0C | 4 | total object (node) count |
| 0x10 | 4 | total value (field) count |
| 0x14 | | root node |

## node structure

a node (the format calls it an "object") is recursive:

| field | type | description |
|---|---|---|
| name hash | uint32 | crc32 of the node name |
| child count | uint32 | number of child nodes |
| children | node[count] | recursive child nodes |
| field count | uint32 | number of named fields |
| fields | field[count] | named field entries |

## field structure

| field | type | description |
|---|---|---|
| name hash | uint32 | crc32 of the field name |
| value | byte[] | raw binary value |

### value encoding

values use a variable-length encoding for their length prefix (kinda like LEB128 but not exactly):

- if length < 0xFE: encoded as a single byte
- if length = 0xFE: followed by a uint32 with the actual length
- if length >= 0xFE (special): treated as uint32 raw

this is in `BinaryObjectFile.ReadCount()` / `WriteCount()`.

## field type detection

since BinaryObject fields don't have type metadata, the editor guesses types from byte length and content:

| length | detection |
|---|---|
| 1 | `byte` |
| 2 | `int16` |
| 4 | `float` |
| 8 | `float64` if all finite values, else `bytes[8]` |
| 12 | `vec3` if 3 finite floats |
| 16 | `vec4` if 4 finite floats |
| 3 | `vec3_byte` |
| other | `bytes[N]` |

## round-trip guarantee

the parser preserves:
- unknown field hashes and raw bytes
- field ordering within a node
- empty fields (zero-length byte arrays)
- all node hierarchy

serializing a freshly-deserialized file produces byte-identical output. that was important to me so editing one field doesn't corrupt the rest.

## implementation

- `TC1.Core.BinaryObject.BinaryObjectFile` — top-level container (serialize/deserialize)
- `TC1.Core.BinaryObject.Node` — recursive tree node with children + fields
- `TC1.Core.BinaryObjectTypeHelper` — type detection and value formatting
