using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;

namespace HospitalSystem.Core.Extensions;

/// <summary>
/// Bloques de miembros de extensión de C# 14 (extension properties y métodos de dominio).
/// </summary>
public static class HospitalDomainExtensions
{
    extension(Sala sala)
    {
        public int TotalCamas => sala.Camas.Count;
        public int CamasDisponibles => sala.Camas.Count(c => !c.Ocupada);
        public int CamasOcupadas => sala.Camas.Count(c => c.Ocupada);

        public decimal PorcentajeOcupacion => sala.Camas.Count == 0
            ? 0m
            : Math.Round(sala.Camas.Count(c => c.Ocupada) * 100m / sala.Camas.Count, 1);
    }

    extension(Medico medico)
    {
        public string EtiquetaTurno => medico.Turno switch
        {
            TurnoMedico.Manana => "Mañana (07:00 - 14:00)",
            TurnoMedico.Tarde => "Tarde (14:00 - 21:00)",
            TurnoMedico.Noche => "Noche (21:00 - 07:00)",
            TurnoMedico.Guardia24h => "Guardia 24 Horas",
            _ => medico.Turno.ToString()
        };

        public string EstadoGuardiaTexto => medico.EnGuardiaHoy ? "Médico del Día" : "Fuera de turno";
    }

    extension(EstadoSalidaLaboratorio estado)
    {
        public string TextoLegible => estado switch
        {
            EstadoSalidaLaboratorio.PendienteSalida => "Pendiente de traslado",
            EstadoSalidaLaboratorio.EnLaboratorio => "En laboratorio",
            EstadoSalidaLaboratorio.ResultadoRecibido => "Resultado recibido",
            _ => estado.ToString()
        };
    }
}
