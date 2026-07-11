using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace TC1.Studio.Services;

public enum HashOrigin { BuiltIn, User, Community }

public class HashEntry
{
    public string Name { get; set; }
    public HashOrigin Origin { get; set; }
}

public class HashService
{
    private readonly Dictionary<uint, HashEntry> _crc32 = new();
    private readonly Dictionary<ulong, HashEntry> _crc64 = new();

    private string _userPath;
    private string _communityDir;

    public HashService(string dataDir = null)
    {
        dataDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TC1-Studio", "hashes");
        _userPath = Path.Combine(dataDir, "User", "fields.user.json");
        _communityDir = Path.Combine(dataDir, "Community");
        LoadBuiltIn();
        LoadCommunity();
        LoadUser();
    }

    public void SetPaths(string userPath, string communityDir)
    {
        _userPath = userPath;
        _communityDir = communityDir;
    }

    private void LoadBuiltIn()
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.EndsWith(".json")) continue;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) continue;
            LoadFromStream(stream, HashOrigin.BuiltIn, overwrite: false);
        }
    }

    public void LoadUser()
    {
        if (!string.IsNullOrEmpty(_userPath) && File.Exists(_userPath))
            LoadJsonFile(_userPath, HashOrigin.User, overwrite: true);
    }

    public void LoadCommunity()
    {
        if (string.IsNullOrEmpty(_communityDir) || !Directory.Exists(_communityDir)) return;
        foreach (var file in Directory.GetFiles(_communityDir, "*.json"))
            LoadJsonFile(file, HashOrigin.Community, overwrite: false);
    }

    private void LoadJsonFile(string path, HashOrigin origin, bool overwrite)
    {
        using var stream = File.OpenRead(path);
        LoadFromStream(stream, origin, overwrite);
    }

    private void LoadFromStream(Stream stream, HashOrigin origin, bool overwrite)
    {
        using var doc = JsonDocument.Parse(stream);
        foreach (var entry in doc.RootElement.EnumerateObject())
        {
            if (!entry.Name.StartsWith("0x")) continue;

            var name = entry.Value.GetString();
            if (string.IsNullOrEmpty(name)) continue;

            if (entry.Name.Length == 18) // CRC64
            {
                var hash = System.Convert.ToUInt64(entry.Name.Substring(2), 16);
                if (overwrite || !_crc64.ContainsKey(hash))
                    _crc64[hash] = new HashEntry { Name = name, Origin = origin };
            }
            else // CRC32
            {
                var hash = System.Convert.ToUInt32(entry.Name.Substring(2), 16);
                if (overwrite || !_crc32.ContainsKey(hash))
                    _crc32[hash] = new HashEntry { Name = name, Origin = origin };
            }
        }
    }

    public bool SaveUserHash(uint hash, string name)
    {
        _crc32[hash] = new HashEntry { Name = name, Origin = HashOrigin.User };

        var dir = Path.GetDirectoryName(_userPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var dict = new Dictionary<string, string>();
        foreach (var kv in _crc32)
        {
            if (kv.Value.Origin == HashOrigin.User)
                dict[$"0x{kv.Key:X8}"] = kv.Value.Name;
        }
        foreach (var kv in _crc64)
        {
            if (kv.Value.Origin == HashOrigin.User)
                dict[$"0x{kv.Key:X16}"] = kv.Value.Name;
        }

        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_userPath, json);
        return true;
    }

    public string Resolve(uint hash)
    {
        return _crc32.TryGetValue(hash, out var entry) ? entry.Name : $"0x{hash:X8}";
    }

    public string Resolve(ulong hash)
    {
        return _crc64.TryGetValue(hash, out var entry) ? entry.Name : $"0x{hash:X16}";
    }

    public bool TryResolve(uint hash, out string name)
    {
        if (_crc32.TryGetValue(hash, out var entry))
        {
            name = entry.Name;
            return true;
        }
        name = null;
        return false;
    }

    public bool TryResolve(ulong hash, out string name)
    {
        if (_crc64.TryGetValue(hash, out var entry))
        {
            name = entry.Name;
            return true;
        }
        name = null;
        return false;
    }

    public bool TryGetEntry(uint hash, out HashEntry entry)
    {
        return _crc32.TryGetValue(hash, out entry);
    }

    public bool TryGetEntry(ulong hash, out HashEntry entry)
    {
        return _crc64.TryGetValue(hash, out entry);
    }

    public bool IsKnown(uint hash) => _crc32.ContainsKey(hash);
    public bool IsKnown(ulong hash) => _crc64.ContainsKey(hash);

    public bool IsUserDefined(uint hash)
    {
        return _crc32.TryGetValue(hash, out var e) && e.Origin == HashOrigin.User;
    }
}
