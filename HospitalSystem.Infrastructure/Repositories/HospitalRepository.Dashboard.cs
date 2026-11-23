using System.Data;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Extensions;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

/// <summary>
/// Módulo Dashboard, Salas, Camas y Convenios de HospitalRepository.
/// </summary>
public sealed partial class HospitalRepository
{
    public async Task<DashboardHospitalDto> ObtenerIndicadoresAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand("SELECT * FROM vw_indicadores_hospital;", conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (await reader.ReadAsync(ct))
            {
                return new DashboardHospitalDto
                {
                    CamasDisponibles = reader.GetInt32("camas_disponibles"),
                    CamasTotales = reader.GetInt32("camas_totales"),
                    CantidadSalas = reader.GetInt32("cantidad_salas"),
                    MedicosDelDia = reader.GetInt32("medicos_del_dia"),
                    PacientesActivos = reader.GetInt32("pacientes_activos"),
                    SalidasLaboratorioActivas = reader.GetInt32("salidas_laboratorio_activas"),
                    UsandoMySql = true,
                    OrigenDatos = $"MySQL ({conn.Database} @ {conn.DataSource})"
                };
            }
        }

        lock (dataStore.SyncRoot)
        {
            return new DashboardHospitalDto
            {
                CamasDisponibles = dataStore.Camas.Count(c => !c.Ocupada),
                CamasTotales = dataStore.Camas.Count,
                CantidadSalas = dataStore.Salas.Count(s => s.Activa),
                MedicosDelDia = dataStore.Medicos.Count(m => m.Activo && m.EnGuardiaHoy),
                PacientesActivos = dataStore.Atenciones.Count(a => a.Estado != EstadoAtencion.AltaMedica),
                SalidasLaboratorioActivas = dataStore.SalidasLaboratorio.Count(s => s.Estado != EstadoSalidaLaboratorio.ResultadoRecibido),
                UsandoMySql = false,
                OrigenDatos = "Modo Respaldo en Memoria (Datos Ficticios Demo)"
            };
        }
    }

    public async Task<IReadOnlyList<SalaResumenDto>> ObtenerSalasResumenAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var resultado = new List<SalaResumenDto>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT
                    s.id,
                    s.codigo,
                    s.nombre,
                    s.piso,
                    COUNT(c.id) AS camas_totales,
                    COALESCE(SUM(CASE WHEN c.ocupada = 0 THEN 1 ELSE 0 END), 0) AS camas_disponibles,
                    fn_ocupacion_sala_pct(s.id) AS ocupacion_pct
                FROM salas s
                LEFT JOIN camas c ON c.sala_id = s.id
                WHERE s.activa = 1
                GROUP BY s.id, s.codigo, s.nombre, s.piso
                ORDER BY s.piso, s.id;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                resultado.Add(new SalaResumenDto
                {
                    SalaId = reader.GetInt32("id"),
                    Codigo = reader.GetString("codigo"),
                    Nombre = reader.GetString("nombre"),
                    Piso = reader.GetInt32("piso"),
                    CamasTotales = reader.GetInt32("camas_totales"),
                    CamasDisponibles = reader.GetInt32("camas_disponibles"),
                    PorcentajeOcupacion = reader.GetDecimal("ocupacion_pct")
                });
            }

            return resultado;
        }

        lock (dataStore.SyncRoot)
        {
            return [.. dataStore.Salas
                .Where(s => s.Activa)
                .Select(s =>
                {
                    s.Camas = [.. dataStore.Camas.Where(c => c.SalaId == s.Id)];
                    return new SalaResumenDto
                    {
                        SalaId = s.Id,
                        Codigo = s.Codigo,
                        Nombre = s.Nombre,
                        Piso = s.Piso,
                        CamasTotales = s.TotalCamas,
                        CamasDisponibles = s.CamasDisponibles,
                        PorcentajeOcupacion = s.PorcentajeOcupacion
                    };
                })];
        }
    }

    public async Task<IReadOnlyList<Cama>> ObtenerCamasDisponiblesAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var camas = new List<Cama>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT c.id, c.sala_id, s.nombre AS sala_nombre, c.codigo, c.ocupada, c.observacion
                FROM camas c
                INNER JOIN salas s ON s.id = c.sala_id
                WHERE c.ocupada = 0 AND s.activa = 1
                ORDER BY s.piso, c.codigo;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                camas.Add(new Cama
                {
                    Id = reader.GetInt32("id"),
                    SalaId = reader.GetInt32("sala_id"),
                    SalaNombre = reader.GetString("sala_nombre"),
                    Codigo = reader.GetString("codigo"),
                    Ocupada = reader.GetBoolean("ocupada"),
                    Observacion = reader.IsDBNull("observacion") ? null : reader.GetString("observacion")
                });
            }

            return camas;
        }

        lock (dataStore.SyncRoot)
        {
            return [.. dataStore.Camas.Where(c => !c.Ocupada).OrderBy(c => c.Codigo)];
        }
    }

    public async Task<IReadOnlyList<Convenio>> ObtenerConveniosAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var convenios = new List<Convenio>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand(
                "SELECT id, codigo, nombre, porcentaje_cobertura, activo FROM convenios WHERE activo = 1 ORDER BY id;",
                conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                convenios.Add(new Convenio
                {
                    Id = reader.GetInt32("id"),
                    Codigo = reader.GetString("codigo"),
                    Nombre = reader.GetString("nombre"),
                    PorcentajeCobertura = reader.GetDecimal("porcentaje_cobertura"),
                    Activo = reader.GetBoolean("activo")
                });
            }

            return convenios;
        }

        lock (dataStore.SyncRoot)
        {
            return [.. dataStore.Convenios.Where(c => c.Activo)];
        }
    }
}
