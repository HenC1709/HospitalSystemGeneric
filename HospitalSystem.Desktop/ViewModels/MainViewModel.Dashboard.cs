using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;

namespace HospitalSystem.Desktop.ViewModels;

/// <summary>
/// Módulo Dashboard, KPIs y Administración de Conexión/Restablecimiento Demo MySQL.
/// </summary>
public sealed partial class MainViewModel
{
    public int CamasDisponibles { get; set => SetProperty(ref field, value); }
    public int CamasTotales { get; set => SetProperty(ref field, value); }
    public int CantidadSalas { get; set => SetProperty(ref field, value); }
    public int MedicosDelDiaCount { get; set => SetProperty(ref field, value); }
    public int PacientesActivosCount { get; set => SetProperty(ref field, value); }
    public int SalidasLabActivasCount { get; set => SetProperty(ref field, value); }

    public bool ConectadoMySql
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(EtiquetaMySqlNavbar));
                OnPropertyChanged(nameof(ColorPuntoMySql));
                OnPropertyChanged(nameof(FondoBadgeMySql));
            }
        }
    }

    public string EtiquetaMySqlNavbar => ConectadoMySql ? "MySQL Conectado" : "Modo Memoria";
    public Brush ColorPuntoMySql => ConectadoMySql
        ? new SolidColorBrush(Color.FromRgb(5, 150, 105))
        : new SolidColorBrush(Color.FromRgb(217, 119, 6));
    public Brush FondoBadgeMySql => ConectadoMySql
        ? new SolidColorBrush(Color.FromRgb(209, 250, 229))
        : new SolidColorBrush(Color.FromRgb(254, 243, 199));

    public string OrigenDatosTexto { get; set => SetProperty(ref field, value); } = "Verificando conexión...";
    public string MensajeEstado { get; set => SetProperty(ref field, value); } = "Sistema hospitalario listo (solo datos ficticios).";
    public string CadenaConexion { get; set => SetProperty(ref field, value); } = string.Empty;
    public bool EstaCargando { get; set => SetProperty(ref field, value); }

    public ObservableCollection<SalaResumenDto> Salas { get; } = [];
    public ObservableCollection<Cama> CamasDisponiblesLista { get; } = [];
    public ObservableCollection<Convenio> Convenios { get; } = [];

    [RelayCommand]
    public async Task InicializarVistaAsync()
    {
        await ProbarConexionAsync();
    }

    [RelayCommand]
    private async Task ProbarConexionAsync()
    {
        try
        {
            EstaCargando = true;
            _services.ConfigurarCadenaConexion(CadenaConexion);
            var resultado = await _services.ProbarConexionMySqlAsync();
            ConectadoMySql = resultado.Conectado;

            Notificar(
                resultado.Conectado ? SeveridadFeedback.Exito : SeveridadFeedback.Advertencia,
                resultado.Conectado ? "Conexión MySQL Activa" : "Modo Respaldo en Memoria",
                resultado.Mensaje);

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error de Configuración MySQL", ex.Message);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task RestablecerBaseDatosDemoAsync()
    {
        var confirmacion = MessageBox.Show(
            "Esta acción ejecutará DROP TABLE sobre 'hospital_system_db', eliminando cualquier registro previo y recargando los datos ficticios de demostración.\n\n¿Deseas restablecer la base de datos de demostración?",
            "Confirmar restablecimiento destructivo (Demo)",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmacion != MessageBoxResult.Yes)
        {
            Notificar(SeveridadFeedback.Informacion, "Operación cancelada", "No se modificaron las tablas ni los registros existentes.");
            return;
        }

        try
        {
            EstaCargando = true;
            _services.ConfigurarCadenaConexion(CadenaConexion);

            var (rutaSchema, rutaSeed) = ResolverRutasSql();
            await _services.InicializarEsquemaMySqlAsync(rutaSchema, rutaSeed);

            var estado = await _services.ProbarConexionMySqlAsync();
            ConectadoMySql = estado.Conectado;

            Notificar(
                SeveridadFeedback.Exito,
                "Base de Datos de Demostración Restablecida",
                "Se recrearon las tablas, funciones almacenadas y datos ficticios iniciales en MySQL.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "No se pudo restablecer MySQL", ex.Message);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task ActualizarManualAsync()
    {
        await RecargarTodoAsync();
        Notificar(SeveridadFeedback.Informacion, "Indicadores Sincronizados", $"Camas libres: {CamasDisponibles}/{CamasTotales} · Médicos del día: {MedicosDelDiaCount}.");
    }

    public async Task RecargarTodoAsync()
    {
        try
        {
            EstaCargando = true;

            var kpis = await _services.ObtenerDashboardAsync();
            CamasDisponibles = kpis.CamasDisponibles;
            CamasTotales = kpis.CamasTotales;
            CantidadSalas = kpis.CantidadSalas;
            MedicosDelDiaCount = kpis.MedicosDelDia;
            PacientesActivosCount = kpis.PacientesActivos;
            SalidasLabActivasCount = kpis.SalidasLaboratorioActivas;
            ConectadoMySql = kpis.UsandoMySql;
            OrigenDatosTexto = kpis.OrigenDatos;

            RecargarColeccion(Salas, await _services.ObtenerSalasAsync());
            RecargarColeccion(CamasDisponiblesLista, await _services.ObtenerCamasDisponiblesAsync());
            RecargarColeccion(Convenios, await _services.ObtenerConveniosAsync());
            RecargarColeccion(Laboratorios, await _services.ObtenerLaboratoriosAsync());
            RecargarColeccion(MedicosDelDia, await _services.ObtenerMedicosAsync(soloDelDia: true));
            RecargarColeccion(MedicosListado, await _services.ObtenerMedicosAsync(soloDelDia: SoloMedicosDelDiaFiltro));

            var pacientes = await _services.ObtenerPacientesAsync(soloActivos: SoloPacientesActivos);
            RecargarColeccion(Pacientes, pacientes);

            var activos = await _services.ObtenerPacientesAsync(soloActivos: true);
            RecargarColeccion(PacientesActivosSelector, activos);

            RecargarColeccion(SalidasLaboratorio, await _services.ObtenerSalidasLaboratorioAsync());

            ConvenioSeleccionado ??= Convenios.FirstOrDefault();
            MedicoAsignadoSeleccionado ??= MedicosDelDia.FirstOrDefault();
            CamaSeleccionada = CamasDisponiblesLista.FirstOrDefault();
            LaboratorioIngresoSeleccionado ??= Laboratorios.FirstOrDefault();
            LaboratorioSalidaSeleccionado ??= Laboratorios.FirstOrDefault();
            PacienteParaLabSeleccionado ??= PacientesActivosSelector.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al consultar datos", ex.Message);
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
