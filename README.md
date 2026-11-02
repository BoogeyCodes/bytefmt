# ByteFmt

[![CI](https://github.com/BoogeyCodes/bytefmt/actions/workflows/ci.yml/badge.svg)](https://github.com/BoogeyCodes/bytefmt/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A small .NET value type for data sizes. `DataSize` parses and formats human byte strings in both
**binary** (KiB = 1024) and **decimal** (KB = 1000) units, and supports comparison and arithmetic.

```csharp
using ByteFmt;

new DataSize(1_536).ToString();                       // "1.5 KiB"
new DataSize(2_500_000_000).Humanize(UnitStandard.Decimal); // "2.5 GB"

DataSize.Parse("1.5 GiB").Bytes;                      // 1610612736
DataSize.Parse("900 MB").Bytes;                       // 900000000
DataSize.Parse("1,024 KB").Bytes;                     // 1024000

var total = DataSize.FromMebibytes(4) + DataSize.FromKibibytes(512); // 4.5 MiB
```

## Why

"Is it KB or KiB? 1000 or 1024?" is a question every project with file sizes eventually gets wrong.
ByteFmt makes the distinction explicit and handles both, in one small immutable struct that also
compares, sorts and adds. It is the kind of utility you would otherwise reimplement slightly
differently in every codebase; here it is, with the edge cases tested.

## Features

- **Parse** human strings: `"2048"`, `"512 B"`, `"1.5 GiB"`, `"900 MB"`, `"1,024 KB"`, `"1K"`.
  Binary (`KiB`) and decimal (`KB`) suffixes are distinguished; a bare letter (`1K`) is treated as
  binary. Parsing is case-insensitive and tolerates thousands separators and spaces.
- **Format** with `Humanize(standard, decimals)`: the largest unit that keeps the value ≥ 1 is
  chosen, and sub-unit values are shown as whole bytes. `ToString()` uses binary units.
- **A proper value type**: immutable, non-negative, with `==`, `<`, `>`, `CompareTo`, `+`, `* int`,
  equality and hashing all keyed on the byte count, so `DataSize` sorts and compares naturally.
- **Factory helpers**: `FromBytes`, `FromKibibytes`, `FromMebibytes`, `FromGibibytes`,
  `FromTebibytes`, and the `Kibibytes`/`Mebibytes`/… properties for fractional values.

## Install

Packaged as a .NET library (`ByteFmt`). Reference the project, or build the NuGet package with
`dotnet pack`.

## Usage

```csharp
using ByteFmt;

// format
DataSize.FromGibibytes(3.25).Humanize(UnitStandard.Binary, decimals: 2); // "3.25 GiB"
new DataSize(900).Humanize(UnitStandard.Decimal);                        // "900 B"

// parse (throwing or Try pattern)
DataSize size = DataSize.Parse("1.5 GiB");
if (DataSize.TryParse(userInput, out var parsed)) { /* ... */ }

// compare and sort
var sizes = new[] { DataSize.Parse("1 MiB"), DataSize.Parse("1 KiB"), new DataSize(500) };
Array.Sort(sizes); // 500 B, 1 KiB, 1 MiB

// round-trips
var restored = DataSize.Parse(size.Humanize(UnitStandard.Binary, decimals: 3));
```

Invalid input (`""`, `"abc"`, `"-5 KB"`, an unknown suffix) makes `Parse` throw `FormatException` and
`TryParse` return `false`. A negative size throws `ArgumentOutOfRangeException`.

## Design notes

- **Bytes are the single source of truth.** A `DataSize` stores a `long` byte count; every unit value
  is derived, so there is no rounding drift between representations and equality is exact.
- **Binary vs decimal is a choice at the edges.** The stored value is unit-agnostic; the unit
  standard is chosen when formatting, and inferred from the suffix when parsing. A bare `K`/`M`/`G` is
  treated as binary because that is what people usually mean by "1K".
- **Overflow is checked.** Parsing and arithmetic use `checked` arithmetic, so an overflowing value
  fails loudly (or, for `TryParse`, returns `false`) rather than wrapping.

## Testing

```bash
dotnet test
```

Forty-one tests covering binary and decimal formatting at the unit boundaries, precision, parsing of
every suffix form (including bare letters, case and separators), round-tripping, the comparison and
arithmetic operators, equality/hashing, and rejected input. CI runs them on Linux and Windows and
builds the NuGet package.

## Project layout

```
src/ByteFmt
├── DataSize.cs      the value type: parse, format, operators
└── UnitStandard.cs  binary/decimal units and suffix resolution
tests/ByteFmt.Tests  xUnit tests
```

## Ideas for later

- `IFormattable` / custom format strings (`"{0:MiB}"`).
- Subtraction and division (returning a ratio).
- A `System.Text.Json` converter.

## License

MIT
