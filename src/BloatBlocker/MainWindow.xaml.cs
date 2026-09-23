using System.Windows;
using BloatBlocker.ViewModels;

namespace BloatBlocker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
