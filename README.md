# ZModeler3 LOD Manager

A standalone Windows desktop tool for organizing and creating LOD (Level of Detail) hierarchies
for ZModeler3 vehicle models - built from the ChatGPT conversation this repo started from
(`samtale.docx`): a separate program that runs alongside ZModeler3 (not a plugin inside it), with
two modes, selectable LOD level count, a configurable naming convention, and a preview before
anything is changed.

## What it does

- **Organize Existing LODs** - for parts that already have LOD meshes with different polygon
  counts (however they're currently named), finds the variants of each part and renames them
  consistently to `_L0`, `_L1`, `_L2`, ... ordered from highest to lowest triangle count.
- **Create LOD Copies** - for parts that don't have LODs yet, duplicates each one into the
  configured number of LOD levels (`_L0`, `_L1`, `_L2`, ...) so you don't have to manually copy
  and rename every child by hand. Parts that already have variants are skipped (use Organize
  Existing LODs for those instead).
- Select one or more hierarchies (check any node in the tree) and both modes work through every
  part underneath the checked node(s).
- Number of LOD levels and the naming pattern (`_L{n}` by default, e.g. `_LOD{n}`) are both
  configurable.
- Nothing is changed until you click **Apply** - **Generate Preview** shows every planned rename
  and duplication first.

## About the .z3d limitation - please read this before opening a real model

ZModeler3's native `.z3d` file format is a proprietary, undocumented binary format. While
scoping this out in the original conversation, the plan was to inspect a real `.z3d` file before
writing any binary reader/writer, specifically to avoid guessing at the format and risking
corrupted models. That sample file was never provided, so **direct `.z3d` reading/writing is not
implemented** - opening a `.z3d` file shows a clear explanation instead of silently failing or
(worse) writing something that could damage a real model.

**Working today:** export your model from ZModeler3 as `.obj` (File > Export), open that file
here, run Organize Existing LODs or Create LOD Copies, save the result, and re-import that `.obj`
back into ZModeler3. The OBJ importer/exporter is fully implemented and non-destructive - it never
touches vertex data, only object names and which faces belong to which named part, so duplicating
a part for a new LOD level is guaranteed not to corrupt the mesh.

**To add real `.z3d` support later:** see `IMPLEMENTING_Z3D.md` for a starting point once a real
sample file (ideally one with, and one without, existing LODs) is available.

## Project layout

```
ZModeler3LodManager.sln
src/
  ZModeler3LodManager.Core/    - hierarchy model, LOD naming/grouping/planning logic, file I/O
                                 (no WPF dependency - this is what's actually tested)
  ZModeler3LodManager.App/     - the WPF (.NET 8) desktop application
  ZModeler3LodManager.Tests/   - xUnit tests for everything in Core
samples/                       - sample .obj/.json files to try the tool against
```

## Building and running (Windows)

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet build
dotnet run --project src/ZModeler3LodManager.App
```

> This was developed and unit-tested in a Linux sandbox, where WPF applications can't actually be
> compiled or run (the Windows Desktop build tools only exist on Windows). The core logic
> (`ZModeler3LodManager.Core`) was fully built and its test suite passes there; the WPF project
> (`ZModeler3LodManager.App`) was written carefully by hand but has only been verified by
> `dotnet build`/`dotnet run` on Windows in principle, not literally run yet. If you hit a build
> error in the App project on your machine, it's likely a small XAML/binding typo - share the
> error and it's a quick fix.

To run the tests:

```
dotnet test
```

## Trying it without a real ZModeler3 export

`samples/police_car_no_lods.obj` mirrors the `PoliceCar` example from the original conversation
(Body, Interior, Wheels, Lights, Grille, Bumper, none of them with LODs yet) - open it, check the
root node, switch to **Create LOD Copies**, and generate a preview.

`samples/truck_mixed_lods.obj` has parts with pre-existing, inconsistently-named LOD variants
(`Cab_high`/`Cab_low`, `Chassis.001`/`Chassis.002`, `Bumper_hp`/`Bumper_mp`) plus one part
(`Grille`) with only a single variant - open it and try **Organize Existing LODs** to see them
grouped and renamed by triangle count, while `Grille` is correctly left untouched.

`samples/fire_truck_nested_lods.json` and `samples/police_car.json` are lightweight JSON versions
of the same idea, useful for quickly experimenting with the hierarchy logic without needing real
mesh data.

## Naming and grouping assumptions - tune these once you test against a real export

Since the real ZModeler3 export conventions were never confirmed against a sample file, the
grouping logic uses a configurable, best-guess set of "variant marker" patterns to recognize
existing LOD variants (`_L0`/`_LOD1`, `_high`/`_low`/`_hp`/`_mp`/`_lp`, `.001`-style copy suffixes,
`(1)`-style copy suffixes, trailing `_0`/`_1`/...). If your actual ZModeler3/OBJ export uses a
different convention, edit `LodNamingOptions.DefaultVariantMarkers` in
`src/ZModeler3LodManager.Core/Naming/LodNamingOptions.cs` (or adjust it per-run once that's
exposed in the UI) to match.
