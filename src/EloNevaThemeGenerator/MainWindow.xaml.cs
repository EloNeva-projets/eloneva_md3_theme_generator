using System.ComponentModel;
using System.Windows;
using EloNevaThemeGenerator.ViewModels;

namespace EloNevaThemeGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnClearFilters(object sender, RoutedEventArgs e) => EmployeesGrid.ClearFilters();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && !vm.ConfirmDiscard())
            e.Cancel = true;
    }
}
