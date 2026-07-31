# Changelog

## 0.2.0 (2026-07-31)

feature pass. the archive no longer breaks your save if you only opened a couple entries, and raw byte fields are editable now.

### new stuff

- extract all button in the archive explorer, pulls everything out in one pass instead of reopening the dat per entry.
- hex editing for raw byte fields. type in something like "FF 00 80 3F" (spaces optional) and it just works, so bytes, vec3_byte and the not so pretty blobs are editable too.
- add and remove fields on any node, both undoable. right click a node for add field, right click a field for remove.
- rename archive entries now. file name hashes are crc64 and they get their own user hash entries, same as fields.
- validation panel under the main view. lists every warning and error with a path instead of just a count in the status bar.
- entry sizes shown in the archive list so you can spot the chunky ones.
- save no longer throws when you edit one entry out of a hundred. entries you never opened get pulled straight from the source dat.
- cli repack keeps lzo compression when the dll is present.
- validation also flags infinity float64 values, not just nan.

## 0.1.1 (2026-07-31)

first round of fixes from the initial code review. mostly edge cases that bite in weird spots. the file format is unchanged, no save migration needed.

### fixed

- lzo work buffer is per thread now. the shared buffer could corrupt data if two threads hit it at once, which can happen during batch extraction.
- fixed a seek bug in the binaryobject reader. offset referenced fields near the start of a node would throw or read garbage. offsets are validated before seeking now.
- repacking no longer dies on compression schemes we dont handle. those entries get written back uncompressed so the archive still loads. lzo entries stay lzo when the dll is around.
- undo trimming no longer rebuilds the whole stack at 100 commands. it just drops the oldest one, so no more array shuffling.
- saving a document no longer serializes the whole tree twice just to check for changes. it compares against the source bytes, which is a lot faster on big files.
- renaming a hash only writes that one entry to the user file instead of rewriting every user hash.
- type guessing for 8, 12 and 16 byte fields is stricter so random byte blobs are less likely to show up as doubles and vectors.
- the diff button updates on its own now instead of waiting for a manual nudge.
- the property grid shows a red error line when it cant parse your input instead of silently ignoring you.
- removed a dead alignment check in validation that computed a value and never used it.
- the bit packing variables in the bigfile code got real names. they were a, c and d, good luck figuring that out later.
- the 0xfe and 0xff markers in the stream helpers are named constants now.

still kicking, not fixed this round: the tree render lag on deep objects, and extracting multiple entries reopens the dat file each time.
