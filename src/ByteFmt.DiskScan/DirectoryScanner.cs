namespace ByteFmt.DiskScan;

/// <summary>
/// Walks a directory tree and reports where the space goes: the total size, the biggest immediate
/// subfolders, the breakdown by file extension, and the largest individual files. Inaccessible files
/// and folders are skipped and counted rather than aborting the scan.
/// </summary>
public sealed class DirectoryScanner
{
    public ScanResult Scan(string rootPath, ScanOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("A path is required.", nameof(rootPath));
        }

        options ??= new ScanOptions();
        if (options.Top < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Top must not be negative.");
        }

        var root = new DirectoryInfo(Path.GetFullPath(rootPath));
        if (!root.Exists)
        {
            throw new DirectoryNotFoundException($"Directory not found: {root.FullName}");
        }

        var acc = new Accumulator();

        // The root's own files, then each immediate subdirectory (recursively) as a FolderStat.
        long rootFilesSize = AddFiles(root, acc, options);
        var folders = new List<FolderStat>();
        foreach (var sub in SafeDirectories(root, acc))
        {
            if (!options.FollowSymlinks && IsReparsePoint(sub))
            {
                continue;
            }

            int filesBefore = acc.FileCount;
            long size = Walk(sub, acc, options);
            folders.Add(new FolderStat(sub.FullName, sub.Name, new DataSize(size), acc.FileCount - filesBefore));
        }

        long total = rootFilesSize;
        foreach (var f in folders)
        {
            total += f.Size.Bytes;
        }

        var topFolders = folders
            .OrderByDescending(f => f.Size.Bytes).ThenBy(f => f.Name, StringComparer.Ordinal)
            .Take(options.Top).ToList();

        var topExtensions = acc.Extensions
            .Select(kv => new ExtensionStat(kv.Key, new DataSize(kv.Value.Bytes), kv.Value.Count))
            .OrderByDescending(e => e.Size.Bytes).ThenBy(e => e.Extension, StringComparer.Ordinal)
            .Take(options.Top).ToList();

        var largestFiles = acc.Files
            .OrderByDescending(f => f.Size.Bytes).ThenBy(f => f.Name, StringComparer.Ordinal)
            .Take(options.Top).ToList();

        return new ScanResult(
            root.FullName, new DataSize(total), acc.FileCount, acc.DirectoryCount, acc.SkippedCount,
            topFolders, topExtensions, largestFiles);
    }

    private long Walk(DirectoryInfo dir, Accumulator acc, ScanOptions options)
    {
        acc.DirectoryCount++;
        long size = AddFiles(dir, acc, options);
        foreach (var sub in SafeDirectories(dir, acc))
        {
            if (!options.FollowSymlinks && IsReparsePoint(sub))
            {
                continue;
            }

            size += Walk(sub, acc, options);
        }

        return size;
    }

    private static long AddFiles(DirectoryInfo dir, Accumulator acc, ScanOptions options)
    {
        FileInfo[] files;
        try
        {
            files = dir.GetFiles();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            acc.SkippedCount++;
            return 0;
        }

        long size = 0;
        foreach (var file in files)
        {
            if (!options.FollowSymlinks && IsReparsePoint(file))
            {
                continue;
            }

            long length;
            try
            {
                length = file.Length;
            }
            catch (Exception e) when (e is FileNotFoundException or IOException)
            {
                acc.SkippedCount++;
                continue;
            }

            size += length;
            acc.FileCount++;

            var extension = file.Extension.ToLowerInvariant();
            if (extension.Length == 0)
            {
                extension = "(none)";
            }

            acc.AddExtension(extension, length);
            acc.AddFile(new FileEntry(file.FullName, file.Name, new DataSize(length)), options.Top);
        }

        return size;
    }

    private static IEnumerable<DirectoryInfo> SafeDirectories(DirectoryInfo dir, Accumulator acc)
    {
        try
        {
            return dir.GetDirectories();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            acc.SkippedCount++;
            return [];
        }
    }

    private static bool IsReparsePoint(FileSystemInfo info)
    {
        try
        {
            return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Mutable scan state. The largest-files list is kept bounded so a scan of a tree with millions of
    /// files does not hold them all in memory: it keeps a little more than <c>Top</c> and trims.
    /// </summary>
    private sealed class Accumulator
    {
        private const int TrimThresholdMultiplier = 8;

        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public int SkippedCount { get; set; }

        public Dictionary<string, (long Bytes, int Count)> Extensions { get; } = new();
        public List<FileEntry> Files { get; } = [];

        public void AddExtension(string extension, long bytes)
        {
            var current = Extensions.GetValueOrDefault(extension);
            Extensions[extension] = (current.Bytes + bytes, current.Count + 1);
        }

        public void AddFile(FileEntry entry, int top)
        {
            Files.Add(entry);
            var cap = Math.Max(top, 1) * TrimThresholdMultiplier;
            if (Files.Count > cap)
            {
                Files.Sort((a, b) => b.Size.Bytes.CompareTo(a.Size.Bytes));
                Files.RemoveRange(Math.Max(top, 1), Files.Count - Math.Max(top, 1));
            }
        }
    }
}
