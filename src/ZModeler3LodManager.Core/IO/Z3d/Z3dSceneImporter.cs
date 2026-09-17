using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.IO.Z3d;

/// <summary>
/// Extension point for reading ZModeler3's native .z3d format directly. Not implemented - see
/// <see cref="Z3dNotSupportedMessage.Text"/> for why, and IMPLEMENTING_Z3D.md for how to fill
/// this in once a real sample file is available.
/// </summary>
public static class Z3dSceneImporter
{
    public static Scene Import(string filePath) => throw new NotSupportedException(Z3dNotSupportedMessage.Text);
}
