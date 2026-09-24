using Avalonia.Controls;
using OpenScanSim.UI.ViewModels;

namespace OpenScanSim.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
