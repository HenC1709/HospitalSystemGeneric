using System.Windows;
using HospitalSystem.Application.Services;
using HospitalSystem.Desktop.Themes;
using HospitalSystem.Desktop.ViewModels;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Repositories;

namespace HospitalSystem.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HospitalThemeManager.AplicarTema(modoOscuro: false);

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
