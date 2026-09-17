namespace ZModeler3LodManager.ViewModels;

/// <summary>Root DataContext: the existing file-based workflow, plus the new live/attach one.</summary>
public class ShellViewModel
{
    public MainViewModel File { get; } = new();

    public LiveViewModel Live { get; } = new();
}
