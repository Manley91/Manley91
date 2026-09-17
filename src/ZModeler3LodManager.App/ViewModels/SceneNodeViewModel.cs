using System.Collections.ObjectModel;
using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.ViewModels;

/// <summary>Wraps a <see cref="SceneNode"/> for display and checkbox-selection in the TreeView.</summary>
public class SceneNodeViewModel : ObservableObject
{
    public SceneNode Node { get; }

    public ObservableCollection<SceneNodeViewModel> Children { get; } = new();

    public string DisplayText => Node.TriangleCount.HasValue
        ? $"{Node.Name} ({Node.TriangleCount} tris)"
        : Node.Name;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public SceneNodeViewModel(SceneNode node)
    {
        Node = node;
    }
}
