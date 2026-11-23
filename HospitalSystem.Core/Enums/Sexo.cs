namespace HospitalSystem.Core.Enums;

public enum Sexo
{
    Masculino,
    Femenino,
    Otro
}

public enum TurnoMedico
{
    Manana,
    Tarde,
    Noche,
    Guardia24h
}

public enum EstadoAtencion
{
    EnAtencion,
    EnLaboratorio,
    AltaMedica
}

public enum TipoLaboratorio
{
    Interno,
    ExternoConvenio
}

public enum PrioridadLaboratorio
{
    Rutina,
    Urgente,
    Critica
}

public enum EstadoSalidaLaboratorio
{
    PendienteSalida,
    EnLaboratorio,
    ResultadoRecibido
}
