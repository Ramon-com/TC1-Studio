# hash database

crc32 field names and crc64 file names are stored as json hash → name mappings. the database uses layers so you can add custom names without messing with the shipped files.

## layer precedence

lookup order: **user > community > builtin**

```
user/fields.user.json         ← %APPDATA%/TC1-Studio/hashes/User/ (highest priority — your custom names)
community/*.json              ← %APPDATA%/TC1-Studio/hashes/Community/ (shared packs from other people)
builtin/crc32_fields.json     ← embedded in the exe (read-only, shipped with the app)
```

## json format

each file is just a flat object mapping hex hash keys to string names:

```json
{
  "0x2002CFD9": "RainPrecipitationAmount",
  "0x46C636DA": "WetMaterialFactor",
  "0xDF6951A5": "WetPuddleFactor"
}
```

- crc32 keys: `0x` + 8 hex digits (10 chars total)
- crc64 keys: `0x` + 16 hex digits (18 chars total)

## adding names (in-app)

1. right-click any unknown field (shown as `0x????????`) in the object tree or property grid
2. select **assign friendly name…**
3. type a valid identifier (letters, digits, underscores; must start with a letter or underscore)
4. the name saves to `User/fields.user.json` immediately
5. all views refresh — the new name shows up right away

## community packs

to share a set of hash names:

1. drop a `.json` file in `%APPDATA%/TC1-Studio/hashes/Community/`
2. restart the app (or it'll pick it up on next startup)
3. community entries show up with a "(Community)" tag

community packs are lower priority than user names but higher than built-in, so you can override community entries individually.

## implementation

- `TC1.Studio.Services.HashService` — manages layer dictionaries, loaded at startup
- `HashEntry` — tracks both the resolved name and its origin layer (who added it)
- `SaveUserHash()` — saves a new name to the user layer and writes to disk
