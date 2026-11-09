using ByteFmt.DiskScan;

namespace ByteFmt.DiskScan.Tests;

public sealed class DirectoryScannerTests : IDisposable
{
    private readonly string _root;
    private readonly DirectoryScanner _scanner = new();

    public DirectoryScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "bytefmt-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        // root/a.txt (100), root/big.bin (5000)
        Write("a.txt", 100);
        Write("big.bin", 5000);
        // root/sub1/b.txt (200), root/sub1/c.log (300)
        Directory.CreateDirectory(Path.Combine(_root, "sub1"));
        Write(Path.Combine("sub1", "b.txt"), 200);
        Write(Path.Combine("sub1", "c.log"), 300);
        // root/sub2/d.txt (50)
        Directory.CreateDirectory(Path.Combine(_root, "sub2"));
        Write(Path.Combine("sub2", "d.txt"), 50);
    }

    private void Write(string relative, int bytes) =>
        File.WriteAllBytes(Path.Combine(_root, relative), new byte[bytes]);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Totals_sum_every_file_and_directory()
    {
        var result = _scanner.Scan(_root);

        Assert.Equal(100 + 5000 + 200 + 300 + 50, result.TotalSize.Bytes);
        Assert.Equal(5, result.FileCount);
        Assert.Equal(2, result.DirectoryCount); // sub1, sub2
        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public void Top_folders_are_immediate_children_sized_recursively_largest_first()
    {
        var result = _scanner.Scan(_root);

        Assert.Equal(2, result.TopFolders.Count);
        var sub1 = result.TopFolders[0];
        Assert.Equal("sub1", sub1.Name);
        Assert.Equal(500, sub1.Size.Bytes); // 200 + 300
        Assert.Equal(2, sub1.FileCount);

        var sub2 = result.TopFolders[1];
        Assert.Equal("sub2", sub2.Name);
        Assert.Equal(50, sub2.Size.Bytes);
    }

    [Fact]
    public void Extensions_are_aggregated_largest_first()
    {
        var result = _scanner.Scan(_root);

        var byExt = result.TopExtensions.ToDictionary(e => e.Extension);
        Assert.Equal(5000, byExt[".bin"].Size.Bytes);
        Assert.Equal(1, byExt[".bin"].FileCount);
        Assert.Equal(350, byExt[".txt"].Size.Bytes); // 100 + 200 + 50
        Assert.Equal(3, byExt[".txt"].FileCount);
        Assert.Equal(300, byExt[".log"].Size.Bytes);

        Assert.Equal(".bin", result.TopExtensions[0].Extension); // biggest first
    }

    [Fact]
    public void Largest_files_lists_the_biggest_file_first()
    {
        var result = _scanner.Scan(_root);

        Assert.Equal("big.bin", result.LargestFiles[0].Name);
        Assert.Equal(5000, result.LargestFiles[0].Size.Bytes);
    }

    [Fact]
    public void Top_limits_the_lists()
    {
        var result = _scanner.Scan(_root, new ScanOptions { Top = 1 });

        Assert.Single(result.TopFolders);
        Assert.Single(result.LargestFiles);
        Assert.Equal("big.bin", result.LargestFiles[0].Name);
        // Totals are unaffected by Top.
        Assert.Equal(5, result.FileCount);
    }

    [Fact]
    public void Files_with_no_extension_are_grouped_as_none()
    {
        File.WriteAllBytes(Path.Combine(_root, "README"), new byte[10]);
        var result = _scanner.Scan(_root);
        Assert.Contains(result.TopExtensions, e => e.Extension == "(none)");
    }

    [Fact]
    public void Empty_directory_scans_to_zero()
    {
        var empty = Path.Combine(_root, "empty");
        Directory.CreateDirectory(empty);
        var result = _scanner.Scan(empty);
        Assert.Equal(0, result.TotalSize.Bytes);
        Assert.Equal(0, result.FileCount);
        Assert.Empty(result.LargestFiles);
    }

    [Fact]
    public void Missing_directory_throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => _scanner.Scan(Path.Combine(_root, "does-not-exist")));
    }

    [Fact]
    public void Blank_path_throws()
    {
        Assert.Throws<ArgumentException>(() => _scanner.Scan("  "));
    }
}
