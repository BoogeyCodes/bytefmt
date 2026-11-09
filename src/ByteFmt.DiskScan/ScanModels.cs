namespace ByteFmt.DiskScan;

/// <summary>One file found during a scan.</summary>
public sealed record FileEntry(string Path, string Name, DataSize Size);

/// <summary>An immediate subdirectory of the scanned root, with its recursive size and file count.</summary>
public sealed record FolderStat(string Path, string Name, DataSize Size, int FileCount);

/// <summary>Every file of a given extension, aggregated.</summary>
public sealed record ExtensionStat(string Extension, DataSize Size, int FileCount);

/// <summary>Options controlling a scan.</summary>
public sealed record ScanOptions
{
    /// <summary>How many entries to keep in each "top" list (folders, extensions, largest files).</summary>
    public int Top { get; init; } = 10;

    /// <summary>Follow symbolic links / reparse points. Off by default to avoid cycles.</summary>
    public bool FollowSymlinks { get; init; } = false;
}

/// <summary>
/// The result of scanning a directory tree: the total size, counts, and the biggest folders,
/// extensions and files. All sizes are <see cref="DataSize"/> values, so they format themselves.
/// </summary>
public sealed record ScanResult(
    string Root,
    DataSize TotalSize,
    int FileCount,
    int DirectoryCount,
    int SkippedCount,
    IReadOnlyList<FolderStat> TopFolders,
    IReadOnlyList<ExtensionStat> TopExtensions,
    IReadOnlyList<FileEntry> LargestFiles);
