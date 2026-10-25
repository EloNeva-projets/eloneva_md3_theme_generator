using System.Windows;

namespace EloNeva.Md3.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnAjouterClient(object sender, RoutedEventArgs e) => ViewModel.AjouterClient();

    private void OnChangerStatut(object sender, RoutedEventArgs e) => ViewModel.ChangerUnStatut();

    private void OnEffacerFiltres(object sender, RoutedEventArgs e) => ClientsGrid.ClearFilters();
}
