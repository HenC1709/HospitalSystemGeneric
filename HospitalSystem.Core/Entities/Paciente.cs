using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;

namespace HospitalSystem.Core.Entities;

public sealed class Paciente
{
    public int Id { get; init; }

    // C# 14: propiedad respaldada por 'field' con validación de dominio directa
    public string Cedula
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new HospitalValidationException("La cédula del paciente es obligatoria.")
            : value.Trim().ToUpperInvariant();
    } = "SIN-CEDULA";

    public string Nombres
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new HospitalValidationException("Los nombres del paciente son obligatorios.")
            : value.Trim();
    } = "Sin Nombre";

    public Sexo Sexo { get; set; } = Sexo.Otro;

    public string SintomasActuales
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new HospitalValidationException("Debe especificar los síntomas del paciente.")
            : value.Trim();
    } = "Por evaluar";

    public int? ConvenioId { get; set; }
    public string? ConvenioNombre { get; set; }
    public DateTime FechaRegistro { get; init; } = DateTime.Now;
}
