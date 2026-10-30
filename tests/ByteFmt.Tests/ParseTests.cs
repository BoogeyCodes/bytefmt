using ByteFmt;

namespace ByteFmt.Tests;

public sealed class ParseTests
{
    [Theory]
    [InlineData("2048", 2048)]
    [InlineData("512 B", 512)]
    [InlineData("1 KiB", 1024)]
    [InlineData("1.5 KiB", 1536)]
    [InlineData("1 MiB", 1048576)]
    [InlineData("2 GiB", 2147483648)]
    public void Parses_binary_suffixes(string text, long expectedBytes)
    {
        Assert.Equal(expectedBytes, DataSize.Parse(text).Bytes);
    }

    [Theory]
    [InlineData("1 KB", 1000)]
    [InlineData("1.5 KB", 1500)]
    [InlineData("900 MB", 900_000_000)]
    [InlineData("2.5 GB", 2_500_000_000)]
    public void Parses_decimal_suffixes(string text, long expectedBytes)
    {
        Assert.Equal(expectedBytes, DataSize.Parse(text).Bytes);
    }

    [Theory]
    [InlineData("1K", 1024)]   // a bare letter is binary
    [InlineData("1M", 1048576)]
    [InlineData("1 gib", 1073741824)] // case-insensitive
    [InlineData("1,024 KB", 1_024_000)] // thousands separators and spaces allowed
    public void Handles_bare_letters_case_and_separators(string text, long expectedBytes)
    {
        Assert.Equal(expectedBytes, DataSize.Parse(text).Bytes);
    }

    [Fact]
    public void Round_trips_through_humanize_and_parse()
    {
        var original = DataSize.FromGibibytes(3.25);
        var restored = DataSize.Parse(original.Humanize(UnitStandard.Binary, decimals: 3));
        Assert.Equal(original.Bytes, restored.Bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("-5 KB")]
    [InlineData("1 ZB")]
    [InlineData("1 KiBB")]
    public void Rejects_invalid_input(string text)
    {
        Assert.False(DataSize.TryParse(text, out _));
        Assert.Throws<FormatException>(() => DataSize.Parse(text));
    }
}
