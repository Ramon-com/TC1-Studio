using System.IO;

namespace TC1.Studio.Services;

public class FileService
{
    public (string fat, string dat) FindArchivePair(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".fat")
            return (path, Path.ChangeExtension(path, ".dat"));
        if (ext == ".dat")
            return (Path.ChangeExtension(path, ".fat"), path);
        return (null, null);
    }

    public void WriteExtractedFile(string dir, int index, ulong hash, byte[] data)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{index:D6}_{hash:X16}.bin");
        File.WriteAllBytes(path, data);
    }

    public byte[] ReadExtractedFile(string dir, int index, ulong hash)
    {
        var path = Path.Combine(dir, $"{index:D6}_{hash:X16}.bin");
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
