namespace HospitalSystem.Core.Entities;

public sealed class Hospital
{
    public int Id { get; init; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoHabilitacion { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}
