namespace ZModeler3LodManager.Core.IO.Z3d;

public static class Z3dNotSupportedMessage
{
    public const string Text =
        "Direct .z3d support isn't implemented yet. ZModeler3's native .z3d file format is a " +
        "proprietary, undocumented binary format, and no real .z3d sample file was available " +
        "while building this tool, so its layout was never inspected. Guessing at the binary " +
        "structure would risk corrupting real models - the exact concern raised when this tool " +
        "was scoped out.\n\n" +
        "Workaround for now: in ZModeler3, use File > Export to save the model as .obj, open " +
        "that .obj here, run Organize Existing LODs or Create LOD Copies, save the result, and " +
        "re-import that .obj back into ZModeler3.\n\n" +
        "To add real .z3d support: get one .z3d file without LODs and one with existing L0/L1/L2 " +
        "LODs, inspect their byte layout (a hex editor plus diffing two near-identical exports is " +
        "usually the fastest way in), and implement the reading/writing logic in " +
        "Z3dSceneImporter/Z3dSceneExporter. See IMPLEMENTING_Z3D.md at the repository root for a " +
        "more detailed starting point.";
}
