namespace ZModeler3LodManager.ViewModels;

/// <summary>One row in the preview grid: a planned rename or duplication.</summary>
public class ChangeOperationViewModel
{
    public string Kind { get; }

    public string Path { get; }

    public string OldName { get; }

    public string NewName { get; }

    public ChangeOperationViewModel(string kind, string path, string oldName, string newName)
    {
        Kind = kind;
        Path = path;
        OldName = oldName;
        NewName = newName;
    }
}
