using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.Services;
using HospitalSystem.Core.Enums;

namespace HospitalSystem.Desktop.ViewModels;

public enum SeccionMenu
{
    Resumen,
    Pacientes,
    Medicos,
    Laboratorios
}

public enum SeveridadFeedback
{
    Informacion,
    Exito,
    Advertencia,
    Error
}

/// <summary>
/// Núcleo de MainViewModel: navegación del Navbar, sistema de notificaciones clínicas
/// y utilidades comunes (los módulos específicos residen en sus archivos parciales).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly HospitalServices _services;

    public event Action? VistaCambiada;
    public event Action? FeedbackActualizado;

    public MainViewModel(HospitalServices services)
    {
        _services = services;
        CadenaConexion = services.CadenaConexionActual;
    }

    public SeccionMenu SeccionActual
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(EsVistaResumen));
                OnPropertyChanged(nameof(EsVistaPacientes));
                OnPropertyChanged(nameof(EsVistaMedicos));
                OnPropertyChanged(nameof(EsVistaLaboratorios));
                VistaCambiada?.Invoke();
            }
        }
    } = SeccionMenu.Resumen;

    public bool EsVistaResumen
    {
        get => SeccionActual == SeccionMenu.Resumen;
        set { if (value) SeccionActual = SeccionMenu.Resumen; }
    }

    public bool EsVistaPacientes
    {
        get => SeccionActual == SeccionMenu.Pacientes;
        set { if (value) SeccionActual = SeccionMenu.Pacientes; }
    }

    public bool EsVistaMedicos
    {
        get => SeccionActual == SeccionMenu.Medicos;
        set { if (value) SeccionActual = SeccionMenu.Medicos; }
    }

    public bool EsVistaLaboratorios
    {
        get => SeccionActual == SeccionMenu.Laboratorios;
        set { if (value) SeccionActual = SeccionMenu.Laboratorios; }
    }

    public IReadOnlyList<Sexo> OpcionesSexo { get; } = Enum.GetValues<Sexo>();
    public IReadOnlyList<TurnoMedico> OpcionesTurno { get; } = Enum.GetValues<TurnoMedico>();
    public IReadOnlyList<PrioridadLaboratorio> OpcionesPrioridad { get; } = Enum.GetValues<PrioridadLaboratorio>();

    // Banner Clínico Animado de Feedback
    public bool FeedbackVisible { get; set => SetProperty(ref field, value); } = true;
    public string FeedbackTitulo { get; set => SetProperty(ref field, value); } = "Entorno de Demostración Inicializado";
    public string FeedbackDetalle { get; set => SetProperty(ref field, value); } = "Utiliza exclusivamente datos ficticios. Puedes probar tu conexión MySQL desde Resumen General.";
    public string FeedbackHora { get; set => SetProperty(ref field, value); } = DateTime.Now.ToString("HH:mm:ss");
    public Brush FeedbackFondoBrush { get; set => SetProperty(ref field, value); } = new SolidColorBrush(Color.FromRgb(224, 242, 254));
    public Brush FeedbackBordeBrush { get; set => SetProperty(ref field, value); } = new SolidColorBrush(Color.FromRgb(2, 132, 199));
    public Brush FeedbackTextoBrush { get; set => SetProperty(ref field, value); } = new SolidColorBrush(Color.FromRgb(12, 74, 110));

    private void Notificar(SeveridadFeedback severidad, string titulo, string detalle)
    {
        var (fondo, borde, texto) = severidad switch
        {
            SeveridadFeedback.Exito => (
                Color.FromRgb(209, 250, 229),
                Color.FromRgb(5, 150, 105),
                Color.FromRgb(6, 95, 70)),
            SeveridadFeedback.Advertencia => (
                Color.FromRgb(254, 243, 199),
                Color.FromRgb(217, 119, 6),
                Color.FromRgb(146, 64, 14)),
            SeveridadFeedback.Error => (
                Color.FromRgb(254, 226, 226),
                Color.FromRgb(220, 38, 38),
                Color.FromRgb(153, 27, 27)),
            _ => (
                Color.FromRgb(224, 242, 254),
                Color.FromRgb(2, 132, 199),
                Color.FromRgb(12, 74, 110))
        };

        FeedbackFondoBrush = new SolidColorBrush(fondo);
        FeedbackBordeBrush = new SolidColorBrush(borde);
        FeedbackTextoBrush = new SolidColorBrush(texto);
        FeedbackTitulo = titulo;
        FeedbackDetalle = detalle;
        FeedbackHora = DateTime.Now.ToString("HH:mm:ss");
        FeedbackVisible = true;
        MensajeEstado = $"{titulo}: {detalle}";
        FeedbackActualizado?.Invoke();
    }

    [RelayCommand]
    private void Navegar(SeccionMenu destino) => SeccionActual = destino;

    [RelayCommand]
    private void CerrarFeedback() => FeedbackVisible = false;

    private static void RecargarColeccion<T>(ObservableCollection<T> coleccion, IEnumerable<T> nuevos)
    {
        coleccion.Clear();
        foreach (var item in nuevos)
            coleccion.Add(item);
    }

    private static (string Schema, string Seed) ResolverRutasSql()
    {
        var candidatos = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Database"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database")),
            Path.Combine(Directory.GetCurrentDirectory(), "Database")
        };

        foreach (var dir in candidatos)
        {
            var schema = Path.Combine(dir, "schema.sql");
            var seed = Path.Combine(dir, "seed.sql");
            if (File.Exists(schema) && File.Exists(seed))
                return (schema, seed);
        }

        return (Path.Combine(AppContext.BaseDirectory, "Database", "schema.sql"),
                Path.Combine(AppContext.BaseDirectory, "Database", "seed.sql"));
    }

    public static string LeerCadenaConexionConfig()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return string.Empty;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("HospitalMySql", out var mysql))
            {
                return mysql.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Usa valor por defecto si appsettings.json tiene formato inválido
        }

        return string.Empty;
    }
}
