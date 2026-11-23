namespace HospitalSystem.Core.Entities;

public sealed class DiagnosticoPaciente
{
    public int Id { get; init; }
    public int AtencionId { get; set; }
    public int PacienteId { get; set; }
    public int DiagnosticoId { get; set; }
    public bool Confirmado { get; set; }
    public DateTime FechaAsignacion { get; init; } = DateTime.Now;
}
