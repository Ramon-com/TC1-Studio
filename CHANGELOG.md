# Changelog

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
