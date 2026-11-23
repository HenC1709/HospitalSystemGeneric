using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using HospitalSystem.Core.Entities;
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

    // =========================================================================
    // Navegación del Navbar Hospitalario (C# 14 'field' en propiedades MVVM)
    // =========================================================================
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

    // =========================================================================
    // Indicadores Principales y Estado Visual de MySQL
    // =========================================================================
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
    public string MensajeEstado { get; set => SetProperty(ref field, value); } = "Sistema hospitalario listo.";
    public string CadenaConexion { get; set => SetProperty(ref field, value); } = string.Empty;
    public bool EstaCargando { get; set => SetProperty(ref field, value); }

    // =========================================================================
    // Sistema Rico de Feedback Visual (Banner Clínico Animado)
    // =========================================================================
    public bool FeedbackVisible { get; set => SetProperty(ref field, value); } = true;
    public string FeedbackTitulo { get; set => SetProperty(ref field, value); } = "Bienvenido a HospitalSystem";
    public string FeedbackDetalle { get; set => SetProperty(ref field, value); } = "Panel clínico inicializado. Puedes conectar o inicializar tu base de datos MySQL en cualquier momento.";
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
    private void CerrarFeedback() => FeedbackVisible = false;

    // =========================================================================
    // Colecciones Observables para las Vistas
    // =========================================================================
    public ObservableCollection<SalaResumenDto> Salas { get; } = [];
    public ObservableCollection<Cama> CamasDisponiblesLista { get; } = [];
    public ObservableCollection<Convenio> Convenios { get; } = [];
    public ObservableCollection<Laboratorio> Laboratorios { get; } = [];
    public ObservableCollection<Medico> MedicosDelDia { get; } = [];
    public ObservableCollection<Medico> MedicosListado { get; } = [];
    public ObservableCollection<ResumenPacienteDto> Pacientes { get; } = [];
    public ObservableCollection<ResumenPacienteDto> PacientesActivosSelector { get; } = [];
    public ObservableCollection<SalidaLaboratorioDto> SalidasLaboratorio { get; } = [];

    public IReadOnlyList<Sexo> OpcionesSexo { get; } = Enum.GetValues<Sexo>();
    public IReadOnlyList<TurnoMedico> OpcionesTurno { get; } = Enum.GetValues<TurnoMedico>();
    public IReadOnlyList<PrioridadLaboratorio> OpcionesPrioridad { get; } = Enum.GetValues<PrioridadLaboratorio>();

    // =========================================================================
    // Formulario: Menú Pacientes
    // =========================================================================
    public string PacienteCedula { get; set => SetProperty(ref field, value); } = string.Empty;
    public string PacienteNombres { get; set => SetProperty(ref field, value); } = string.Empty;
    public string PacienteSintomas { get; set => SetProperty(ref field, value); } = string.Empty;
    public Sexo PacienteSexo { get; set => SetProperty(ref field, value); } = Sexo.Masculino;
    public Convenio? ConvenioSeleccionado { get; set => SetProperty(ref field, value); }
    public Medico? MedicoAsignadoSeleccionado { get; set => SetProperty(ref field, value); }
    public bool AsignarCamaHospitalaria { get; set => SetProperty(ref field, value); } = true;
    public Cama? CamaSeleccionada { get; set => SetProperty(ref field, value); }
    public bool PacienteRequiereLaboratorio { get; set => SetProperty(ref field, value); }
    public Laboratorio? LaboratorioIngresoSeleccionado { get; set => SetProperty(ref field, value); }
    public string ExamenIngreso { get; set => SetProperty(ref field, value); } = "Hemograma completo y bioquímica";
    public PrioridadLaboratorio PrioridadIngreso { get; set => SetProperty(ref field, value); } = PrioridadLaboratorio.Urgente;

    public bool SoloPacientesActivos
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = RecargarTodoAsync();
        }
    }

    // =========================================================================
    // Formulario: Menú Médicos
    // =========================================================================
    public bool SoloMedicosDelDiaFiltro
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = RecargarMedicosAsync();
        }
    } = true;

    public string NuevoMedicoCedula { get; set => SetProperty(ref field, value); } = string.Empty;
    public string NuevoMedicoNombres { get; set => SetProperty(ref field, value); } = string.Empty;
    public string NuevoMedicoEspecialidad { get; set => SetProperty(ref field, value); } = string.Empty;
    public TurnoMedico NuevoMedicoTurno { get; set => SetProperty(ref field, value); } = TurnoMedico.Manana;
    public string NuevoMedicoConsultorio { get; set => SetProperty(ref field, value); } = string.Empty;
    public bool NuevoMedicoEnGuardia { get; set => SetProperty(ref field, value); } = true;

    // =========================================================================
    // Formulario: Salidas a Laboratorios
    // =========================================================================
    public ResumenPacienteDto? PacienteParaLabSeleccionado
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
                MotivoSalidaLab = value.Sintomas;
        }
    }

    public Laboratorio? LaboratorioSalidaSeleccionado { get; set => SetProperty(ref field, value); }
    public string ExamenSalidaLab { get; set => SetProperty(ref field, value); } = string.Empty;
    public string MotivoSalidaLab { get; set => SetProperty(ref field, value); } = string.Empty;
    public PrioridadLaboratorio PrioridadSalidaLab { get; set => SetProperty(ref field, value); } = PrioridadLaboratorio.Urgente;
    public SalidaLaboratorioDto? SalidaSeleccionada { get; set => SetProperty(ref field, value); }
    public string ResultadoRecibidoTexto { get; set => SetProperty(ref field, value); } = "Valores dentro de parámetros clínicos esperados.";

    // =========================================================================
    // Comandos de Navegación y Operaciones
    // =========================================================================
    [RelayCommand]
    private void Navegar(SeccionMenu destino) => SeccionActual = destino;

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
    private async Task InicializarBaseDatosMySqlAsync()
    {
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
                "Base de Datos MySQL Inicializada",
                "Se ejecutaron schema.sql y seed.sql con sus 11 tablas, 5 funciones y datos iniciales.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "No se pudo inicializar MySQL", ex.Message);
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

    private async Task RecargarMedicosAsync()
    {
        RecargarColeccion(MedicosListado, await _services.ObtenerMedicosAsync(soloDelDia: SoloMedicosDelDiaFiltro));
    }

    [RelayCommand]
    private async Task RegistrarPacienteAsync()
    {
        try
        {
            var camaId = AsignarCamaHospitalaria ? CamaSeleccionada?.Id : null;
            var labId = PacienteRequiereLaboratorio ? LaboratorioIngresoSeleccionado?.Id : null;
            var nombreRegistrado = PacienteNombres.Trim();

            var request = new RegistroPacienteRequest(
                Cedula: PacienteCedula,
                Nombres: PacienteNombres,
                Sintomas: PacienteSintomas,
                Sexo: PacienteSexo,
                ConvenioId: ConvenioSeleccionado?.Id,
                MedicoId: MedicoAsignadoSeleccionado?.Id ?? 0,
                CamaId: camaId,
                RequiereLaboratorio: PacienteRequiereLaboratorio,
                LaboratorioId: labId,
                ExamenSolicitado: ExamenIngreso,
                PrioridadLaboratorio: PrioridadIngreso
            );

            var atencionId = await _services.RegistrarPacienteAsync(request);

            var derivadoLab = PacienteRequiereLaboratorio;
            PacienteCedula = string.Empty;
            PacienteNombres = string.Empty;
            PacienteSintomas = string.Empty;
            PacienteRequiereLaboratorio = false;

            Notificar(
                SeveridadFeedback.Exito,
                "Ingreso de Paciente Registrado",
                derivadoLab
                    ? $"{nombreRegistrado} ingresó en Atención #{atencionId} con salida inmediata a laboratorio."
                    : $"{nombreRegistrado} ingresó correctamente en Atención #{atencionId}.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Revisa los datos del paciente", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DarAltaPacienteAsync(ResumenPacienteDto? paciente)
    {
        if (paciente?.AtencionId is null || paciente.Estado == EstadoAtencion.AltaMedica)
        {
            Notificar(SeveridadFeedback.Informacion, "Paciente ya dado de alta", "Este episodio clínico ya se encuentra finalizado.");
            return;
        }

        try
        {
            await _services.DarAltaAtencionAsync(paciente.AtencionId.Value);
            Notificar(
                SeveridadFeedback.Exito,
                "Alta Médica Otorgada",
                $"Se finalizó la atención de {paciente.Nombres} y se liberó su cama hospitalaria.");
            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al procesar el alta", ex.Message);
        }
    }

    [RelayCommand]
    private void PrepararSalidaLaboratorioDesdePaciente(ResumenPacienteDto? paciente)
    {
        if (paciente is null)
            return;

        PacienteParaLabSeleccionado = PacientesActivosSelector.FirstOrDefault(p => p.PacienteId == paciente.PacienteId) ?? paciente;
        MotivoSalidaLab = paciente.Sintomas;
        ExamenSalidaLab = "Perfil diagnóstico según sintomatología";
        SeccionActual = SeccionMenu.Laboratorios;

        Notificar(
            SeveridadFeedback.Informacion,
            "Derivación a Laboratorio Lista",
            $"Confirma el laboratorio y examen solicitado para {paciente.Nombres}.");
    }

    [RelayCommand]
    private async Task AlternarGuardiaMedicoAsync(Medico? medico)
    {
        if (medico is null)
            return;

        try
        {
            var nuevoEstado = !medico.EnGuardiaHoy;
            await _services.CambiarGuardiaMedicoAsync(medico.Id, nuevoEstado);

            Notificar(
                SeveridadFeedback.Exito,
                nuevoEstado ? "Incorporado a Médicos del Día" : "Guardia Actualizada",
                nuevoEstado
                    ? $"{medico.Nombres} ahora figura activo en la guardia del día."
                    : $"{medico.Nombres} pasó a estado fuera de turno por hoy.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al cambiar guardia", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RegistrarMedicoAsync()
    {
        try
        {
            var nombre = NuevoMedicoNombres.Trim();
            var request = new NuevoMedicoRequest(
                CedulaProfesional: NuevoMedicoCedula,
                Nombres: NuevoMedicoNombres,
                Especialidad: NuevoMedicoEspecialidad,
                Turno: NuevoMedicoTurno,
                Consultorio: NuevoMedicoConsultorio,
                EnGuardiaHoy: NuevoMedicoEnGuardia
            );

            await _services.RegistrarMedicoAsync(request);

            NuevoMedicoCedula = string.Empty;
            NuevoMedicoNombres = string.Empty;
            NuevoMedicoEspecialidad = string.Empty;
            NuevoMedicoConsultorio = string.Empty;

            Notificar(SeveridadFeedback.Exito, "Especialista Incorporado", $"{nombre} se agregó correctamente a la plantilla médica.");
            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Datos incompletos del médico", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RegistrarSalidaLaboratorioAsync()
    {
        if (PacienteParaLabSeleccionado is null)
        {
            Notificar(SeveridadFeedback.Advertencia, "Paciente requerido", "Selecciona un paciente activo para autorizar su salida a laboratorio.");
            return;
        }

        try
        {
            var medicoId = PacienteParaLabSeleccionado.MedicoId > 0
                ? PacienteParaLabSeleccionado.MedicoId
                : (MedicosDelDia.FirstOrDefault()?.Id ?? 1);

            var request = new NuevaSalidaLaboratorioRequest(
                PacienteId: PacienteParaLabSeleccionado.PacienteId,
                AtencionId: PacienteParaLabSeleccionado.AtencionId,
                MedicoId: medicoId,
                LaboratorioId: LaboratorioSalidaSeleccionado?.Id ?? 0,
                ExamenSolicitado: ExamenSalidaLab,
                MotivoClinico: MotivoSalidaLab,
                Prioridad: PrioridadSalidaLab
            );

            await _services.RegistrarSalidaLaboratorioAsync(request);
            ExamenSalidaLab = string.Empty;

            Notificar(
                SeveridadFeedback.Exito,
                "Salida a Laboratorio Autorizada",
                $"{PacienteParaLabSeleccionado.Nombres} fue derivado a {LaboratorioSalidaSeleccionado?.Nombre}.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Revisa la orden de laboratorio", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CompletarSalidaLaboratorioAsync(SalidaLaboratorioDto? salida)
    {
        var objetivo = salida ?? SalidaSeleccionada;
        if (objetivo is null || !objetivo.PuedeRecibirResultado)
            return;

        try
        {
            var resumen = string.IsNullOrWhiteSpace(ResultadoRecibidoTexto)
                ? "Estudio completado sin hallazgos críticos adicionales."
                : ResultadoRecibidoTexto;

            await _services.CompletarSalidaLaboratorioAsync(objetivo.SalidaId, resumen);

            Notificar(
                SeveridadFeedback.Exito,
                "Resultado Recibido de Laboratorio",
                $"Se registró el retorno clínico de {objetivo.PacienteNombres} a sala.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al registrar retorno", ex.Message);
        }
    }

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
