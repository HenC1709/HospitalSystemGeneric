using System.Windows;
using System.Windows.Media;
using HospitalSystem.Application.Services;
using HospitalSystem.Desktop.ViewModels;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Repositories;
using Wpf.Ui.Appearance;

namespace HospitalSystem.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Aplicar acento azul clínico hospitalario sobre tema luminoso limpio
        ApplicationAccentColorManager.Apply(Color.FromRgb(2, 132, 199), ApplicationTheme.Light);

        var connectionString = MainViewModel.LeerCadenaConexionConfig();
        var dataStore = new HospitalDataStore(connectionString);
        var repository = new HospitalRepository(dataStore);
        var hospitalServices = new HospitalServices(repository);
        var mainViewModel = new MainViewModel(hospitalServices);

        var window = new MainWindow(mainViewModel);
        MainWindow = window;
        window.Show();
    }
}
