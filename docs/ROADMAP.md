# roadmap

## v0.1.0-alpha — done ✓

- bigfile archive open/browse/repack
- binaryobject inspect and edit (float, vec3, vec4, float64, int16, byte)
- undo/redo (100-level command stack)
- validation (nan, infinity, circular refs, structural checks)
- diff viewer (original vs modified)
- hash database with layered lookup (builtin / user / community)
- in-app hash naming (right-click → assign friendly name)
- plugin api (`IEditorPlugin`)
- 66 unit tests

## v0.2 — weather plugin (next)

- domain-specific weather editor with sliders, toggles, presets
- grow the hash database from weather reverse-engineering
- real-world validation: edit → repack → actually play

## v0.3 — search & hash editor

- search across all entries in an archive
- dedicated hash database management panel
- import/export community hash packs
- batch rename from reverse-engineering sessions

## v0.4 — texture preview & hex inspector

- dds texture preview for relevant entries
- hex viewer/editor for raw bytes
- entry-level metadata display

## v0.5 — vehicle editor

- vehicle parameter editing (handling, speed, cosmetics)
- follow the same pattern as the weather plugin

## v1.0 — stable plugin ecosystem

- dynamic plugin discovery (load from `Plugins/` directory)
- plugin sdk docs and examples
- community plugin repo
- polished ux (dark theme refinements, keyboard shortcuts, accessibility)
- crash recovery and autosave

## what this won't do

- distribute game assets (obviously)
- online multiplayer modding
- support non-tc1 games (the format knowledge might help with other ubiart titles but no plans)
