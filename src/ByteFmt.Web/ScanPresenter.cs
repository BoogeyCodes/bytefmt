using ByteFmt.DiskScan;

namespace ByteFmt.Web;

public sealed record ScanResponse(
    string Root,
    long TotalBytes,
    string TotalHuman,
    int Files,
    int Directories,
    int Skipped,
    string Standard,
    IReadOnlyList<FolderDto> TopFolders,
    IReadOnlyList<ExtensionDto> TopExtensions,
    IReadOnlyList<FileDto> LargestFiles);

public sealed record FolderDto(string Name, string Path, long Bytes, string Human, int Files, double Percent);

public sealed record ExtensionDto(string Extension, long Bytes, string Human, int Files, double Percent);

public sealed record FileDto(string Name, string Path, long Bytes, string Human);

/// <summary>Turns a <see cref="ScanResult"/> into a response with humanised sizes and percentages.</summary>
public static class ScanPresenter
{
    public static ScanResponse ToResponse(ScanResult result, UnitStandard standard = UnitStandard.Binary)
    {
        long total = result.TotalSize.Bytes;
        var whole = result.TotalSize;

        double Percent(DataSize size) => Math.Round(size.FractionOf(whole) * 100.0, 1);
        string Human(DataSize size) => size.Humanize(standard);

        return new ScanResponse(
            result.Root,
            total,
            Human(result.TotalSize),
            result.FileCount,
            result.DirectoryCount,
            result.SkippedCount,
            standard.ToString(),
            result.TopFolders
                .Select(f => new FolderDto(f.Name, f.Path, f.Size.Bytes, Human(f.Size), f.FileCount, Percent(f.Size)))
                .ToList(),
            result.TopExtensions
                .Select(e => new ExtensionDto(e.Extension, e.Size.Bytes, Human(e.Size), e.FileCount, Percent(e.Size)))
                .ToList(),
            result.LargestFiles
                .Select(f => new FileDto(f.Name, f.Path, f.Size.Bytes, Human(f.Size)))
                .ToList());
    }
}
