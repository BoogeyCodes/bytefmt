using System.Globalization;

namespace ByteFmt;

/// <summary>
/// An immutable quantity of digital information, stored as a whole number of bytes.
/// Parses and formats both binary (KiB = 1024) and decimal (KB = 1000) units, and supports
/// comparison and arithmetic.
/// </summary>
public readonly struct DataSize : IEquatable<DataSize>, IComparable<DataSize>
{
    public long Bytes { get; }

    public DataSize(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "A data size cannot be negative.");
        }

        Bytes = bytes;
    }

    public static readonly DataSize Zero = new(0);

    // Factory helpers for the common binary units.
    public static DataSize FromBytes(long bytes) => new(bytes);
    public static DataSize FromKibibytes(double value) => Scale(value, 1L << 10);
    public static DataSize FromMebibytes(double value) => Scale(value, 1L << 20);
    public static DataSize FromGibibytes(double value) => Scale(value, 1L << 30);
    public static DataSize FromTebibytes(double value) => Scale(value, 1L << 40);

    private static DataSize Scale(double value, long unit)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A data size must be a non-negative finite number.");
        }

        return new DataSize(checked((long)Math.Round(value * unit)));
    }

    public double Kibibytes => Bytes / (double)(1L << 10);
    public double Mebibytes => Bytes / (double)(1L << 20);
    public double Gibibytes => Bytes / (double)(1L << 30);
    public double Tebibytes => Bytes / (double)(1L << 40);

    /// <summary>Format using binary units (KiB, MiB, …), e.g. <c>1.50 MiB</c>.</summary>
    public override string ToString() => Humanize(UnitStandard.Binary);

    /// <summary>
    /// Format with the chosen unit standard and number of decimal places. The largest unit that
    /// keeps the value at or above 1 is chosen (bytes are always shown as a whole number).
    /// </summary>
    public string Humanize(UnitStandard standard = UnitStandard.Binary, int decimals = 2)
    {
        if (decimals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals));
        }

        var (bytesPerUnit, suffixes) = Units.For(standard);
        if (Bytes < bytesPerUnit)
        {
            return $"{Bytes} {suffixes[0]}"; // plain bytes, no decimals
        }

        double value = Bytes;
        var index = 0;
        while (value >= bytesPerUnit && index < suffixes.Length - 1)
        {
            value /= bytesPerUnit;
            index++;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{Math.Round(value, decimals)} {suffixes[index]}");
    }

    /// <summary>
    /// Parse a human string such as <c>"1.5 GiB"</c>, <c>"900 MB"</c>, <c>"1,024 KB"</c> or
    /// <c>"2048"</c> (bare bytes). Both binary (KiB) and decimal (KB) suffixes are understood, and
    /// a bare <c>K</c>/<c>M</c>/<c>G</c> is treated as binary.
    /// </summary>
    public static DataSize Parse(string text)
    {
        if (TryParse(text, out var size))
        {
            return size;
        }

        throw new FormatException($"'{text}' is not a valid data size.");
    }

    public static bool TryParse(string? text, out DataSize size)
    {
        size = Zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var splitAt = 0;
        while (splitAt < trimmed.Length && (char.IsDigit(trimmed[splitAt]) || trimmed[splitAt] is '.' or ',' or ' '))
        {
            splitAt++;
        }

        var numberPart = trimmed[..splitAt].Replace(",", "").Replace(" ", "");
        var suffix = trimmed[splitAt..].Trim();

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number < 0)
        {
            return false;
        }

        if (!Units.TryResolveSuffix(suffix, out var multiplier))
        {
            return false;
        }

        try
        {
            size = new DataSize(checked((long)Math.Round(number * multiplier)));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public static DataSize operator +(DataSize a, DataSize b) => new(checked(a.Bytes + b.Bytes));
    public static DataSize operator -(DataSize a, DataSize b) => new(checked(a.Bytes - b.Bytes));
    public static DataSize operator *(DataSize a, int factor) => new(checked(a.Bytes * factor));
    public static bool operator <(DataSize a, DataSize b) => a.Bytes < b.Bytes;
    public static bool operator >(DataSize a, DataSize b) => a.Bytes > b.Bytes;
    public static bool operator <=(DataSize a, DataSize b) => a.Bytes <= b.Bytes;
    public static bool operator >=(DataSize a, DataSize b) => a.Bytes >= b.Bytes;
    public static bool operator ==(DataSize a, DataSize b) => a.Bytes == b.Bytes;
    public static bool operator !=(DataSize a, DataSize b) => a.Bytes != b.Bytes;

    public int CompareTo(DataSize other) => Bytes.CompareTo(other.Bytes);
    public bool Equals(DataSize other) => Bytes == other.Bytes;
    public override bool Equals(object? obj) => obj is DataSize other && Equals(other);
    public override int GetHashCode() => Bytes.GetHashCode();
}
