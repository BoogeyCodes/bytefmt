# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [0.2.0]

Grows the repo from a single value type into a product: **DiskScope**, a disk-usage explorer that
scans a directory and shows where the space goes — biggest folders, file types and files — with every
size rendered by `DataSize`. The library stays a dependency-free NuGet package; the analyzer is a
separate scan engine plus an ASP.NET Core web app.

### Added
- `ByteFmt.DiskScan` — a `DirectoryScanner` that walks a tree and returns a `ScanResult`: total size,
  file/directory counts, the biggest immediate folders (sized recursively), a by-extension breakdown,
  and the largest files. Inaccessible entries are skipped and counted rather than aborting the scan;
  the largest-files list is kept bounded so huge trees don't exhaust memory.
- `ByteFmt.Web` — an ASP.NET Core 8 minimal-API app: a disk-usage dashboard, a `GET /api/scan` JSON
  endpoint (binary or decimal units), and `/healthz`. Integration-tested with `WebApplicationFactory`.
- `DataSize.FractionOf(whole)` — this size as a fraction of another (used by the analyzer's share bars).
- A `Dockerfile` that serves the dashboard.

### Changed
- Version bumped to 0.2.0.

## [0.1.x]

### Added
- `DataSize` subtraction operator (`-`), checked for overflow and rejecting results below
  zero, completing the arithmetic alongside `+` and `*`.

## [0.1.0]

### Added
- `DataSize` value type: parse/format human byte sizes in binary (KiB) and decimal (KB) units.
- Arithmetic and comparison operators, checked arithmetic; NuGet-packable.
