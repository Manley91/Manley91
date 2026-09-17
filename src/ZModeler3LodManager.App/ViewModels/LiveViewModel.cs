using System.Windows;
using System.Windows.Automation;
using ZModeler3LodManager.Automation;

namespace ZModeler3LodManager.ViewModels;

/// <summary>
/// Step 1 of live (no-file) LOD renaming: attach to the running ZModeler3 window and dump its
/// UI Automation tree, so we can see - from the outside, without ZModeler3's source - whether
/// and where it exposes its scene hierarchy. Rename/duplicate actions come once that's known.
/// </summary>
public class LiveViewModel : ObservableObject
{
    private AutomationElement? _mainWindow;

    private string _statusText = "Not attached. Click \"Attach to ZModeler3\" while ZModeler3 is running with a model open.";

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    private string _treeDump = string.Empty;

    public string TreeDump
    {
        get => _treeDump;
        set => SetField(ref _treeDump, value);
    }

    public RelayCommand AttachCommand { get; }

    public RelayCommand InspectCommand { get; }

    public RelayCommand TryRenameProbeCommand { get; }

    public LiveViewModel()
    {
        AttachCommand = new RelayCommand(Attach);
        InspectCommand = new RelayCommand(Inspect, () => _mainWindow is not null);
        TryRenameProbeCommand = new RelayCommand(TryRenameProbe, () => _mainWindow is not null);
    }

    private void Attach()
    {
        try
        {
            _mainWindow = ZModeler3Connection.FindMainWindow();
            StatusText = $"Attached to \"{_mainWindow.Current.Name}\". Click \"Inspect window\" next.";
        }
        catch (Exception ex)
        {
            _mainWindow = null;
            StatusText = ex.Message;
            MessageBox.Show(ex.Message, "Couldn't attach", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            InspectCommand.RaiseCanExecuteChanged();
            TryRenameProbeCommand.RaiseCanExecuteChanged();
        }
    }

    private void TryRenameProbe()
    {
        if (_mainWindow is null)
        {
            return;
        }

        try
        {
            StatusText = RenameProbe.TryAltClickOnFirstRow(_mainWindow);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Probe failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Inspect()
    {
        if (_mainWindow is null)
        {
            return;
        }

        try
        {
            TreeDump = AutomationTreeDump.Dump(_mainWindow);
            StatusText = "Inspected the window - copy the text below (Ctrl+A, Ctrl+C) and send it over.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Couldn't inspect window", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
