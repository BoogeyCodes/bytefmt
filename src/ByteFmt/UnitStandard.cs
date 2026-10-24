namespace ByteFmt;

public enum UnitStandard
{
    /// <summary>Powers of 1024 with IEC suffixes: KiB, MiB, GiB, TiB, PiB.</summary>
    Binary,

    /// <summary>Powers of 1000 with SI suffixes: KB, MB, GB, TB, PB.</summary>
    Decimal,
}

internal static class Units
{
    private static readonly string[] BinarySuffixes = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
    private static readonly string[] DecimalSuffixes = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static (long BytesPerUnit, string[] Suffixes) For(UnitStandard standard) =>
        standard == UnitStandard.Decimal ? (1000, DecimalSuffixes) : (1024, BinarySuffixes);

    /// <summary>
    /// Resolve a suffix string to a byte multiplier. Binary (KiB) and decimal (KB) are distinguished;
    /// a bare letter (K, M, G) is treated as binary, which is what people usually mean by "1K".
    /// </summary>
    public static bool TryResolveSuffix(string suffix, out long multiplier)
    {
        multiplier = 1;
        var normalised = suffix.Trim().TrimEnd('s', 'S');
        if (normalised.Length == 0 || string.Equals(normalised, "B", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Explicit IEC binary suffix, e.g. "KiB".
        var binaryIndex = IndexOfSuffix(BinarySuffixes, normalised);
        if (binaryIndex > 0)
        {
            multiplier = Pow(1024, binaryIndex);
            return true;
        }

        // Decimal "KB" or bare "K": decimal when a 'B' is present, binary otherwise.
        var letter = char.ToUpperInvariant(normalised[0]);
        var order = "KMGTP".IndexOf(letter) + 1;
        if (order == 0)
        {
            return false;
        }

        var rest = normalised[1..];
        var isDecimal = string.Equals(rest, "B", StringComparison.OrdinalIgnoreCase);
        var isBareLetter = rest.Length == 0;
        if (!isDecimal && !isBareLetter)
        {
            return false;
        }

        multiplier = Pow(isDecimal ? 1000 : 1024, order);
        return true;
    }

    private static int IndexOfSuffix(string[] suffixes, string value)
    {
        for (var i = 0; i < suffixes.Length; i++)
        {
            if (string.Equals(suffixes[i], value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static long Pow(long baseValue, int exponent)
    {
        long result = 1;
        for (var i = 0; i < exponent; i++)
        {
            result = checked(result * baseValue);
        }

        return result;
    }
}
