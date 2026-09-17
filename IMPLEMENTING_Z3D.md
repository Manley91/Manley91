# Adding real .z3d support

`Z3dSceneImporter`/`Z3dSceneExporter` (in `src/ZModeler3LodManager.Core/IO/Z3d/`) currently throw
`NotSupportedException`. This is intentional, not a placeholder that was forgotten: ZModeler3's
`.z3d` format is a proprietary, undocumented binary format, and no real `.z3d` file was available
while building this tool, so nobody has looked at its actual byte layout. Writing a binary
importer/exporter without ever having seen a real file would mean guessing at the structure -
exactly the risk that was flagged (and the reason this tool works against `.obj` as an
interchange format instead) when this project was scoped out.

## What you need before touching this

1. One real `.z3d` file with **no** LODs (a plain part hierarchy).
2. One real `.z3d` file that **already has** L0/L1/L2 LODs, ideally exported from the same base
   model so the only difference is the LOD structure.
3. Ideally, a third file that's a small, deliberate variation of #1 (rename one part, add one
   child) so you can diff the bytes and see exactly what changed.

Without at least #1 and #2, don't start writing a binary parser - you'll be guessing, and a wrong
guess here means silently corrupted models, which is the one thing this tool must never do.

## Suggested approach

1. **Diff, don't guess.** Open the files in a hex editor (e.g. HxD, ImHex, or `xxd`/`od` on the
   command line) side by side. Look for:
   - A header/magic number and version field at the start of the file.
   - String tables - part names are human-readable, so `grep`-ing the raw bytes for ASCII/UTF-8
     text (`strings model.z3d`) is a fast way to find where names live and how they're framed
     (length-prefixed? null-terminated?).
   - A block that repeats once per part/node - that's the hierarchy. Look for parent/child index
     references (small integers near each name) to figure out how nesting is encoded.
   - Whatever changed between the no-LOD and with-LOD files - that's the LOD-specific structure
     you actually need for this tool.
2. **Confirm each guess before relying on it.** If you think you've found "number of children",
   change a value experimentally in a copy of a real file's bytes, see if ZModeler3 still opens
   it and reflects the change you expected. Never build the writer path before the reader path
   works reliably on real files.
3. **Reuse the existing model.** Once the layout is understood, implement:
   - `Z3dSceneImporter.Import(string filePath) -> Scene` - populate `SceneNode` names, hierarchy,
     and `TriangleCount`/`RawGeometryLines`-equivalent geometry data (add whatever field(s)
     `SceneNode` needs to hold a reference to the raw mesh block, following the same pattern
     `ObjSceneImporter`/`ObjSceneExporter` already use: read the mesh bytes verbatim, keep them on
     the node, and never regenerate or alter them - only reorder/rename/duplicate at the
     hierarchy level).
   - `Z3dSceneExporter.Export(Scene scene, string filePath)` - write the file back out, changing
     only what `LodPlanApplier.Apply` changed (names, and duplicated nodes), keeping every byte of
     actual mesh geometry identical to the source for anything that wasn't duplicated.
4. **Test round-trips before testing LOD logic.** Add a test that imports a real `.z3d` fixture,
   exports it unchanged, and asserts the output is byte-identical (or semantically identical, if
   ZModeler3 doesn't roundtrip byte-for-byte on its own). Only once that passes should you trust
   the importer/exporter enough to run `LodPlanner`/`LodPlanApplier` against real files.
5. Wire the new code into `SceneIO` (it already dispatches `.z3d` to these classes) and remove the
   `NotSupportedException` calls.

## In the meantime

Export from ZModeler3 as `.obj` (File > Export), run Organize Existing LODs / Create LOD Copies in
this tool, and re-import the result back into ZModeler3. The OBJ path is fully implemented, tested,
and non-destructive (see `src/ZModeler3LodManager.Core/IO/Obj/`) - it just requires the extra
export/import step around ZModeler3 itself until `.z3d` is supported directly.
