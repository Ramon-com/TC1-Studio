# TC1 Studio

made by **iblameramon**.

a lil gui tool for modding **The Crew 1** (2014). you can open the game's archives, dig through the files, edit values, and pack everything back up. basically like a swiss army knife for the game's data files.

## what it does

- **archive browser** - opens .fat/.dat files (the big archives the game uses). handles 900mb+ archives without dying lol. just like opening a zip file but for the crew.
- **binaryobject editor** - the game stores its settings in these weird node/field structures. you can browse through the tree and edit stuff. it auto-detects what type of data each field is (numbers, coordinates, etc).
- **undo / redo** - ctrl+z / ctrl+y. works for like 100 steps so you can mess around without fear.
- **validation** - checks for bad values like NaN, infinity, null fields, circular references, oversized nodes. basically sanity checks before you save.
- **diff viewer** - shows what changed between the original and your edited version. ctrl+d to toggle it.
- **save & repack** - edit → save → repack → launch game. the whole pipeline.
- **plugin api** - an interface for making specialized editors (weather, vehicles, etc). more on that later.

## quick start

1. grab the latest exe from [releases](https://github.com/iblameramon/TC1-Studio/releases)
2. run it (no install needed, it's self-contained)
3. file → open archive, pick a .fat file from your TC1 installation
4. left panel = archive contents, middle = file structure, right = editable fields
5. make your changes, hit apply
6. file → save to repack the archive

that's it. launch the game and see if it works lol

## building from source

```bash
git clone https://github.com/iblameramon/TC1-Studio.git
cd TC1-Studio
dotnet publish TC1.Studio -c Release -r win-x64 --self-contained true -o dist
```

you'll need the .net 8 sdk.

## project layout

```
TC1.Core/           - the format handling stuff (bigfile, binaryobject, hashing, compression)
TC1.Studio/         - the actual gui (avalonia), viewmodels, services, plugins
TC1.Hashes/         - the hash name database (crc32 field names)
TC1.Core.Tests/     - unit tests for the core library
TC1.Studio.Tests/   - unit tests for the editor services
```

## tested

- crc32("RainPrecipitationAmount") = 0x2002CFD9 ✓ (yes i verified this)
- binaryobject round-trip (save then load gives the exact same bytes) ✓
- bigfile extract → edit → repack → game loads it ✓
- 66 unit tests pass

## project info

everything here was built from scratch except the lzo wrapper (that just calls the game's own dll so it can decompress files). the format parsers, the gui, the validation, the diff system, the undo/redo stack, the hash database - all wrote from zero.

took about 160 hours across 2 months, mostly nights and weekends. hardest parts were figuring out the bit packing in the BigFile format (who the hell uses 34-bit offsets??), getting the BinaryObject round-trip to be byte-for-byte identical (took like 3 rewrites), and making undo/redo work cleanly with the tree structure without leaking memory.

for an above average coder with little to no experience in reverse engineering or game modding, this taking around 160+ hours is good. the format RE is the real time sink, the gui is just grunt work.

pushing this now cause i think it's stable enough for people to test and maybe find cool stuff. if it crashes, open an issue. if it works, cool.

## rant

honestly doing this solo sucked ass. also i fucking suck at math and physics so the vector stuff and the bit packing took me way longer than it should have lol. also not a native english speaker - i speak, read and write it fine but there were definitely times i had to dex words i didnt know mid-coding. adds to the time i guess. the ubiart engine is a mess and there's zero documentation for any of this shit so everything was trial and error. spent like a week just on the 34-bit offset math cause i kept reading the bytes wrong. the game would crash randomly when i edited certain fields and i still dont know why - some values just break things silently and theres no error message, the game just dies. my best guess is some fields have range checks server-side or something but who knows.

there's still a bug where if you open a really deeply nested binaryobject the tree rendering lags for a second. tried to fix it with lazy loading but gave up after 3 days, it's not that bad.

another thing that drove me insane - some bigfile entries have a compression flag set to "none" but the data is clearly compressed. must be some edge case in the original packing tool. had to add a fallback that tries lzo decompression even when the flag says none, and if that fails it just returns the raw bytes. works but feels wrong lol.

also the crc64 name hash thing. the game uses crc64 jones but the standard crc64 table everyone posts online is the ecmA version. spent an entire evening wondering why my hashes didnt match before i realized. fun.

if you run into problems, try validating your edited archive before saving. the validation catches most stupid mistakes. if the game crashes on load, you probably edited something you shouldnt have - try changing less fields at once to find the culprit. some nodes just dont like being touched.

hit me up if you use this:
- **instagram**: iblameramon
- **discord**: ramoncoaie
- **reddit**: FuckinNoone

## license

MIT. do what you want with it.
