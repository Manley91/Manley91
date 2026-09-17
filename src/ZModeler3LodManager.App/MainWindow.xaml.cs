using System.Windows;
using ZModeler3LodManager.Core.IO.Z3d;
using ZModeler3LodManager.ViewModels;

namespace ZModeler3LodManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AboutZ3dMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(Z3dNotSupportedMessage.Text, "About .z3d support", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
