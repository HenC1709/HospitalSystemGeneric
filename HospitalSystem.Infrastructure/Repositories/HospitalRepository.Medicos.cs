using System.Data;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

/// <summary>
/// Módulo Médicos del Día y Gestión de Guardia de HospitalRepository.
/// </summary>
public sealed partial class HospitalRepository
{
    public async Task<IReadOnlyList<Medico>> ObtenerMedicosAsync(bool soloDelDia = false, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var medicos = new List<Medico>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            var sql = soloDelDia
                ? "SELECT id, cedula_profesional, nombres, especialidad, turno, consultorio, en_guardia_hoy, activo FROM medicos WHERE activo = 1 AND en_guardia_hoy = 1 ORDER BY nombres;"
                : "SELECT id, cedula_profesional, nombres, especialidad, turno, consultorio, en_guardia_hoy, activo FROM medicos WHERE activo = 1 ORDER BY en_guardia_hoy DESC, nombres;";

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                medicos.Add(new Medico
                {
                    Id = reader.GetInt32("id"),
                    CedulaProfesional = reader.GetString("cedula_profesional"),
                    Nombres = reader.GetString("nombres"),
                    Especialidad = reader.GetString("especialidad"),
                    Turno = Enum.Parse<TurnoMedico>(reader.GetString("turno"), ignoreCase: true),
                    Consultorio = reader.GetString("consultorio"),
                    EnGuardiaHoy = reader.GetBoolean("en_guardia_hoy"),
                    Activo = reader.GetBoolean("activo")
                });
            }

            return medicos;
        }

        lock (dataStore.SyncRoot)
        {
            var query = dataStore.Medicos.Where(m => m.Activo);
            if (soloDelDia)
                query = query.Where(m => m.EnGuardiaHoy);

            return [.. query.OrderByDescending(m => m.EnGuardiaHoy).ThenBy(m => m.Nombres)];
        }
    }

    public async Task<int> RegistrarMedicoAsync(NuevoMedicoRequest request, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            const string sql = """
                INSERT INTO medicos (cedula_profesional, nombres, especialidad, turno, consultorio, en_guardia_hoy, activo)
                VALUES (@cedula, @nombres, @especialidad, @turno, @consultorio, @guardia, 1);
                SELECT LAST_INSERT_ID();
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cedula", request.CedulaProfesional.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@nombres", request.Nombres.Trim());
            cmd.Parameters.AddWithValue("@especialidad", request.Especialidad.Trim());
            cmd.Parameters.AddWithValue("@turno", request.Turno.ToString());
            cmd.Parameters.AddWithValue("@consultorio", string.IsNullOrWhiteSpace(request.Consultorio) ? "Consultorio General" : request.Consultorio.Trim());
            cmd.Parameters.AddWithValue("@guardia", request.EnGuardiaHoy);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        lock (dataStore.SyncRoot)
        {
            var nuevo = new Medico
            {
                Id = dataStore.SiguienteMedicoId(),
                CedulaProfesional = request.CedulaProfesional,
                Nombres = request.Nombres,
                Especialidad = request.Especialidad,
                Turno = request.Turno,
                Consultorio = string.IsNullOrWhiteSpace(request.Consultorio) ? "Consultorio General" : request.Consultorio.Trim(),
                EnGuardiaHoy = request.EnGuardiaHoy,
                Activo = true
            };
            dataStore.Medicos.Add(nuevo);
            return nuevo.Id;
        }
    }

    public async Task CambiarGuardiaMedicoAsync(int medicoId, bool enGuardiaHoy, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand(
                "UPDATE medicos SET en_guardia_hoy = @guardia WHERE id = @id;",
                conn);
            cmd.Parameters.AddWithValue("@guardia", enGuardiaHoy);
            cmd.Parameters.AddWithValue("@id", medicoId);
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        lock (dataStore.SyncRoot)
        {
            var medico = dataStore.Medicos.FirstOrDefault(m => m.Id == medicoId);
            medico?.EnGuardiaHoy = enGuardiaHoy;
        }
    }
}
