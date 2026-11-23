namespace HospitalSystem.Core.Entities;

public sealed class Convenio
{
    public int Id { get; init; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeCobertura { get; set; }
    public bool Activo { get; set; } = true;

    public override string ToString() => $"{Nombre} ({PorcentajeCobertura:0}% cob.)";
}
