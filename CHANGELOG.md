# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- `DataSize` subtraction operator (`-`), checked for overflow and rejecting results below
  zero, completing the arithmetic alongside `+` and `*`.

## [0.1.0]

### Added
- `DataSize` value type: parse/format human byte sizes in binary (KiB) and decimal (KB) units.
- Arithmetic and comparison operators, checked arithmetic; NuGet-packable.
