using HospitalSystem.Core.Enums;

namespace HospitalSystem.Core.Entities;

public sealed class Atencion
{
    public int Id { get; init; }
    public int PacienteId { get; set; }
    public int MedicoId { get; set; }
    public int? CamaId { get; set; }
    public string Sintomas { get; set; } = string.Empty;
    public string? NotasMedicas { get; set; }
    public bool RequiereLaboratorio { get; set; }
    public EstadoAtencion Estado { get; set; } = EstadoAtencion.EnAtencion;
    public DateTime FechaIngreso { get; init; } = DateTime.Now;
    public DateTime? FechaEgreso { get; set; }
}
