namespace ByteFmt.Tests;

public sealed class FractionOfTests
{
    [Fact]
    public void Returns_the_fraction_of_the_whole()
    {
        Assert.Equal(0.25, DataSize.FromBytes(250).FractionOf(DataSize.FromBytes(1000)));
        Assert.Equal(1.0, DataSize.FromBytes(1000).FractionOf(DataSize.FromBytes(1000)));
    }

    [Fact]
    public void Is_zero_when_the_whole_is_zero()
    {
        Assert.Equal(0.0, DataSize.FromBytes(500).FractionOf(DataSize.Zero));
        Assert.Equal(0.0, DataSize.Zero.FractionOf(DataSize.Zero));
    }

    [Fact]
    public void Can_exceed_one_when_larger_than_the_whole()
    {
        Assert.Equal(2.0, DataSize.FromBytes(2000).FractionOf(DataSize.FromBytes(1000)));
    }
}
