using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;

namespace HospitalSystem.Core.Entities;

public sealed class Medico
{
    public int Id { get; init; }

    // C# 14: uso de 'field' para normalizar y validar sin campos privados redundantes
    public string CedulaProfesional
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new HospitalValidationException("La cédula profesional del médico es obligatoria.")
            : value.Trim().ToUpperInvariant();
    } = "MP-0000";

    public string Nombres
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new HospitalValidationException("El nombre del médico es obligatorio.")
            : value.Trim();
    } = "Médico General";

    public string Especialidad
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? "Medicina General" : value.Trim();
    } = "Medicina General";

    public TurnoMedico Turno { get; set; } = TurnoMedico.Manana;
    public string Consultorio { get; set; } = "Consultorio General";
    public bool EnGuardiaHoy { get; set; } = true;
    public bool Activo { get; set; } = true;

    public string ResumenSelector => $"{Nombres} ({Especialidad})";
}
