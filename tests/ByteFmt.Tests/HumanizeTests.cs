using ByteFmt;

namespace ByteFmt.Tests;

public sealed class HumanizeTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KiB")]
    [InlineData(1536, "1.5 KiB")]
    [InlineData(1048576, "1 MiB")]
    [InlineData(1073741824, "1 GiB")]
    public void Binary_formatting_picks_the_largest_fitting_unit(long bytes, string expected)
    {
        Assert.Equal(expected, new DataSize(bytes).Humanize(UnitStandard.Binary));
    }

    [Theory]
    [InlineData(1000, "1 KB")]
    [InlineData(1500, "1.5 KB")]
    [InlineData(1000000, "1 MB")]
    [InlineData(2500000000, "2.5 GB")]
    public void Decimal_formatting_uses_powers_of_1000(long bytes, string expected)
    {
        Assert.Equal(expected, new DataSize(bytes).Humanize(UnitStandard.Decimal));
    }

    [Fact]
    public void Respects_the_requested_precision()
    {
        var size = DataSize.FromMebibytes(1.23456);
        Assert.Equal("1.23 MiB", size.Humanize(UnitStandard.Binary, decimals: 2));
        Assert.Equal("1.235 MiB", size.Humanize(UnitStandard.Binary, decimals: 3));
        Assert.Equal("1 MiB", size.Humanize(UnitStandard.Binary, decimals: 0));
    }

    [Fact]
    public void ToString_uses_binary_units()
    {
        Assert.Equal("1.5 MiB", new DataSize(1024 * 1024 + 512 * 1024).ToString());
    }

    [Fact]
    public void Bytes_below_one_unit_are_shown_whole()
    {
        Assert.Equal("900 B", new DataSize(900).Humanize(UnitStandard.Decimal));
    }
}
