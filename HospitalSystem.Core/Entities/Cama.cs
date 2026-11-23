namespace HospitalSystem.Core.Entities;

public sealed class Cama
{
    public int Id { get; init; }
    public int SalaId { get; set; }
    public string SalaNombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool Ocupada { get; set; }
    public string? Observacion { get; set; }

    public string EtiquetaSelector => string.IsNullOrWhiteSpace(SalaNombre)
        ? Codigo
        : $"{Codigo} — {SalaNombre}";
}
