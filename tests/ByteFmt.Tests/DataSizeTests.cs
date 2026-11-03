using ByteFmt;

namespace ByteFmt.Tests;

public sealed class DataSizeTests
{
    [Fact]
    public void Factory_helpers_convert_binary_units()
    {
        Assert.Equal(1024, DataSize.FromKibibytes(1).Bytes);
        Assert.Equal(1024 * 1024, DataSize.FromMebibytes(1).Bytes);
        Assert.Equal(1536, DataSize.FromKibibytes(1.5).Bytes);
    }

    [Fact]
    public void Exposes_fractional_unit_values()
    {
        var size = DataSize.FromMebibytes(2);
        Assert.Equal(2, size.Mebibytes, 10);
        Assert.Equal(2048, size.Kibibytes, 10);
    }

    [Fact]
    public void Supports_addition_and_scaling()
    {
        var total = DataSize.FromKibibytes(1) + DataSize.FromKibibytes(3);
        Assert.Equal(4096, total.Bytes);
        Assert.Equal(3072, (DataSize.FromKibibytes(1) * 3).Bytes);
    }

    [Fact]
    public void Compares_and_sorts_by_byte_count()
    {
        var sizes = new[] { DataSize.FromMebibytes(1), DataSize.FromKibibytes(1), new DataSize(500) };
        Array.Sort(sizes);
        Assert.Equal([500, 1024, 1024 * 1024], sizes.Select(s => s.Bytes));

        Assert.True(DataSize.FromKibibytes(1) < DataSize.FromMebibytes(1));
        Assert.True(DataSize.FromKibibytes(1) == new DataSize(1024));
    }

    [Fact]
    public void Equality_and_hashing_follow_byte_count()
    {
        var a = DataSize.FromKibibytes(1);
        var b = new DataSize(1024);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Rejects_negative_sizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataSize(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DataSize.FromKibibytes(-1));
    }

    [Fact]
    public void Subtraction_and_zero()
    {
        var remaining = DataSize.FromKibibytes(5) - DataSize.FromKibibytes(2);
        Assert.Equal(new DataSize(3 * 1024), remaining);
        Assert.Equal(0, DataSize.Zero.Bytes);
        Assert.Equal(DataSize.FromKibibytes(2), DataSize.FromKibibytes(2) - DataSize.Zero);
    }

    [Fact]
    public void Subtraction_below_zero_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DataSize.FromKibibytes(1) - DataSize.FromKibibytes(2));
    }
}
