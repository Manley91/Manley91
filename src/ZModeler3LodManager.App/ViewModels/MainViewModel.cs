using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using ZModeler3LodManager.Core.IO;
using ZModeler3LodManager.Core.Models;
using ZModeler3LodManager.Core.Naming;
using ZModeler3LodManager.Core.Processing;

namespace ZModeler3LodManager.ViewModels;

public class MainViewModel : ObservableObject
{
    private Scene? _scene;
    private LodPlan? _pendingPlan;

    public ObservableCollection<SceneNodeViewModel> Roots { get; } = new();

    public ObservableCollection<ChangeOperationViewModel> PreviewOperations { get; } = new();

    public ObservableCollection<string> Notes { get; } = new();

    private string _statusMessage = "Open a model to get started (.obj, .json, or .z3d).";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private bool _isOrganizeExistingMode = true;

    public bool IsOrganizeExistingMode
    {
        get => _isOrganizeExistingMode;
        set
        {
            if (SetField(ref _isOrganizeExistingMode, value) && value)
            {
                IsCreateCopiesMode = false;
            }
        }
    }

    private bool _isCreateCopiesMode;

    public bool IsCreateCopiesMode
    {
        get => _isCreateCopiesMode;
        set
        {
            if (SetField(ref _isCreateCopiesMode, value) && value)
            {
                IsOrganizeExistingMode = false;
            }
        }
    }

    private int _levelCount = 3;

    public int LevelCount
    {
        get => _levelCount;
        set => SetField(ref _levelCount, Math.Max(1, value));
    }

    private string _suffixFormat = "_L{n}";

    public string SuffixFormat
    {
        get => _suffixFormat;
        set => SetField(ref _suffixFormat, value);
    }

    private bool _canApply;

    public bool CanApply
    {
        get => _canApply;
        private set
        {
            if (SetField(ref _canApply, value))
            {
                ApplyCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand OpenCommand { get; }

    public RelayCommand SaveAsCommand { get; }

    public RelayCommand GeneratePreviewCommand { get; }

    public RelayCommand ApplyCommand { get; }

    public MainViewModel()
    {
        OpenCommand = new RelayCommand(Open);
        SaveAsCommand = new RelayCommand(SaveAs, () => _scene is not null);
        GeneratePreviewCommand = new RelayCommand(GeneratePreview, () => _scene is not null);
        ApplyCommand = new RelayCommand(ApplyPlan, () => CanApply);
    }

    private void Open()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All supported files (*.obj;*.json;*.z3d)|*.obj;*.json;*.z3d|" +
                     "Wavefront OBJ (*.obj)|*.obj|" +
                     "JSON scene (*.json)|*.json|" +
                     "ZModeler3 (*.z3d)|*.z3d|" +
                     "All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _scene = SceneIO.Import(dialog.FileName);
            _pendingPlan = null;
            CanApply = false;
            PreviewOperations.Clear();
            Notes.Clear();
            RebuildTree(null);

            var nodeCount = _scene.AllNodes().Count();
            StatusMessage = $"Loaded {nodeCount} node(s) from {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Couldn't open file", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SaveAsCommand.RaiseCanExecuteChanged();
            GeneratePreviewCommand.RaiseCanExecuteChanged();
        }
    }

    private void SaveAs()
    {
        if (_scene is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Wavefront OBJ (*.obj)|*.obj|JSON scene (*.json)|*.json",
            FileName = System.IO.Path.GetFileNameWithoutExtension(_scene.SourcePath),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SceneIO.Export(_scene, dialog.FileName);
            StatusMessage = $"Saved to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Couldn't save file", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GeneratePreview()
    {
        var selected = GetSelectedNodes();
        if (selected.Count == 0)
        {
            MessageBox.Show(
                "Check one or more hierarchies in the tree first.",
                "Nothing selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var options = new LodNamingOptions { LevelCount = LevelCount, SuffixFormat = SuffixFormat };
        _pendingPlan = IsOrganizeExistingMode
            ? LodPlanner.PlanOrganizeExisting(selected, options)
            : LodPlanner.PlanCreateCopies(selected, options);

        PreviewOperations.Clear();
        foreach (var rename in _pendingPlan.Renames)
        {
            PreviewOperations.Add(new ChangeOperationViewModel("Rename", rename.OldPath, rename.OldName, rename.NewName));
        }

        foreach (var duplication in _pendingPlan.Duplications)
        {
            PreviewOperations.Add(new ChangeOperationViewModel(
                "Duplicate", duplication.SourcePath, duplication.Source.Name, duplication.NewName));
        }

        Notes.Clear();
        foreach (var note in _pendingPlan.Notes)
        {
            Notes.Add(note);
        }

        CanApply = !_pendingPlan.IsEmpty;
        StatusMessage = _pendingPlan.IsEmpty
            ? "Nothing to do: no changes were planned for the current selection."
            : $"Preview ready: {_pendingPlan.Renames.Count} rename(s), {_pendingPlan.Duplications.Count} " +
              "duplication(s) planned. Review below, then click Apply.";
    }

    private void ApplyPlan()
    {
        if (_pendingPlan is null || _scene is null)
        {
            return;
        }

        var selectedBefore = GetSelectedNodes();
        LodPlanApplier.Apply(_pendingPlan);
        _pendingPlan = null;
        CanApply = false;
        PreviewOperations.Clear();
        Notes.Clear();
        RebuildTree(selectedBefore);
        StatusMessage = "Changes applied. Use Save As to write the result to a file.";
    }

    private List<SceneNode> GetSelectedNodes()
    {
        var result = new List<SceneNode>();

        void Walk(SceneNodeViewModel vm)
        {
            if (vm.IsSelected)
            {
                result.Add(vm.Node);
            }

            foreach (var child in vm.Children)
            {
                Walk(child);
            }
        }

        foreach (var root in Roots)
        {
            Walk(root);
        }

        return result;
    }

    private void RebuildTree(IEnumerable<SceneNode>? previouslySelected)
    {
        var selectedSet = previouslySelected is null ? null : new HashSet<SceneNode>(previouslySelected);
        Roots.Clear();

        if (_scene is null)
        {
            return;
        }

        foreach (var root in _scene.Roots)
        {
            Roots.Add(BuildViewModel(root, selectedSet));
        }
    }

    private static SceneNodeViewModel BuildViewModel(SceneNode node, HashSet<SceneNode>? selectedSet)
    {
        var vm = new SceneNodeViewModel(node) { IsSelected = selectedSet?.Contains(node) ?? false };
        foreach (var child in node.Children)
        {
            vm.Children.Add(BuildViewModel(child, selectedSet));
        }

        return vm;
    }
}
