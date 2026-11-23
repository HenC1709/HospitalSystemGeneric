namespace HospitalSystem.Core.Entities;

public sealed class Diagnostico
{
    public int Id { get; init; }
    public string CodigoCie10 { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
