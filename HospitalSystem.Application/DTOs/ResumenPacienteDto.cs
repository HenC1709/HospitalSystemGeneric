using HospitalSystem.Core.Enums;

namespace HospitalSystem.Application.DTOs;

public sealed record ResumenPacienteDto
{
    public int PacienteId { get; init; }
    public int? AtencionId { get; init; }
    public required string Cedula { get; init; }
    public required string Nombres { get; init; }
    public Sexo Sexo { get; init; }
    public required string Sintomas { get; init; }
    public string ConvenioNombre { get; init; } = "Particular";
    public string MedicoAsignado { get; init; } = "Sin asignar";
    public int MedicoId { get; init; }
    public string CamaDescripcion { get; init; } = "Ambulatorio";
    public EstadoAtencion Estado { get; init; }
    public bool TieneSalidaLaboratorioActiva { get; init; }
    public DateTime FechaIngreso { get; init; }

    public string EstadoLegible => Estado switch
    {
        EstadoAtencion.EnAtencion => "En atención clínica",
        EstadoAtencion.EnLaboratorio => "Salida a laboratorio",
        EstadoAtencion.AltaMedica => "Alta médica",
        _ => Estado.ToString()
    };

    public string IndicadorLaboratorio => TieneSalidaLaboratorioActiva ? "En Lab" : "En Sala";
}

public sealed record DashboardHospitalDto
{
    public int CamasDisponibles { get; init; }
    public int CamasTotales { get; init; }
    public int CantidadSalas { get; init; }
    public int MedicosDelDia { get; init; }
    public int PacientesActivos { get; init; }
    public int SalidasLaboratorioActivas { get; init; }
    public bool UsandoMySql { get; init; }
    public string OrigenDatos { get; init; } = string.Empty;
}

public sealed record SalaResumenDto
{
    public int SalaId { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public int Piso { get; init; }
    public int CamasTotales { get; init; }
    public int CamasDisponibles { get; init; }
    public decimal PorcentajeOcupacion { get; init; }

    public string ResumenCapacidad => $"{CamasDisponibles} libres de {CamasTotales} camas ({PorcentajeOcupacion:0.#}% ocup.)";
}

public sealed record SalidaLaboratorioDto
{
    public int SalidaId { get; init; }
    public int? AtencionId { get; init; }
    public int PacienteId { get; init; }
    public required string PacienteCedula { get; init; }
    public required string PacienteNombres { get; init; }
    public required string MedicoSolicitante { get; init; }
    public required string LaboratorioNombre { get; init; }
    public TipoLaboratorio TipoLaboratorio { get; init; }
    public required string ExamenSolicitado { get; init; }
    public required string MotivoClinico { get; init; }
    public PrioridadLaboratorio Prioridad { get; init; }
    public EstadoSalidaLaboratorio Estado { get; init; }
    public string? ResultadoResumen { get; init; }
    public DateTime FechaSalida { get; init; }
    public DateTime? FechaRetorno { get; init; }

    public bool PuedeRecibirResultado => Estado != EstadoSalidaLaboratorio.ResultadoRecibido;
}

public sealed record RegistroPacienteRequest(
    string Cedula,
    string Nombres,
    string Sintomas,
    Sexo Sexo,
    int? ConvenioId,
    int MedicoId,
    int? CamaId,
    bool RequiereLaboratorio,
    int? LaboratorioId,
    string? ExamenSolicitado,
    PrioridadLaboratorio PrioridadLaboratorio = PrioridadLaboratorio.Urgente
);

public sealed record NuevoMedicoRequest(
    string CedulaProfesional,
    string Nombres,
    string Especialidad,
    TurnoMedico Turno,
    string Consultorio,
    bool EnGuardiaHoy
);

public sealed record NuevaSalidaLaboratorioRequest(
    int PacienteId,
    int? AtencionId,
    int MedicoId,
    int LaboratorioId,
    string ExamenSolicitado,
    string MotivoClinico,
    PrioridadLaboratorio Prioridad
);

public sealed record EstadoConexionDto(
    bool Conectado,
    string Servidor,
    string BaseDatos,
    string VersionServidor,
    string Mensaje
);
