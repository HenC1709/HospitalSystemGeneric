using HospitalSystem.Core.Enums;

namespace HospitalSystem.Core.Entities;

public sealed class Laboratorio
{
    public int Id { get; init; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoLaboratorio Tipo { get; set; } = TipoLaboratorio.Interno;
    public string Ubicacion { get; set; } = string.Empty;
    public string EspecialidadAnalisis { get; set; } = string.Empty;
    public int? ConvenioId { get; set; }
    public bool Activo { get; set; } = true;

    public string EtiquetaSelector => Tipo == TipoLaboratorio.ExternoConvenio
        ? $"{Nombre} [Externo]"
        : $"{Nombre} [Interno]";
}
