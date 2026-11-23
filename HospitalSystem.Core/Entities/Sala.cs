namespace HospitalSystem.Core.Entities;

public sealed class Sala
{
    public int Id { get; init; }
    public int HospitalId { get; set; } = 1;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Piso { get; set; } = 1;
    public bool Activa { get; set; } = true;
    public List<Cama> Camas { get; set; } = [];
}
