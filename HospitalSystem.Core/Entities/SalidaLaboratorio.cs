using HospitalSystem.Core.Enums;

namespace HospitalSystem.Core.Entities;

public sealed class SalidaLaboratorio
{
    public int Id { get; init; }
    public int? AtencionId { get; set; }
    public int PacienteId { get; set; }
    public int MedicoId { get; set; }
    public int LaboratorioId { get; set; }
    public string ExamenSolicitado { get; set; } = string.Empty;
    public string MotivoClinico { get; set; } = string.Empty;
    public PrioridadLaboratorio Prioridad { get; set; } = PrioridadLaboratorio.Rutina;
    public EstadoSalidaLaboratorio Estado { get; set; } = EstadoSalidaLaboratorio.PendienteSalida;
    public string? ResultadoResumen { get; set; }
    public DateTime FechaSalida { get; init; } = DateTime.Now;
    public DateTime? FechaRetorno { get; set; }
}
