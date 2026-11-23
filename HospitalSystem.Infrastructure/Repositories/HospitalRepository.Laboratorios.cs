using System.Data;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

/// <summary>
/// Módulo Laboratorios y Trazabilidad de Salidas/Retornos de HospitalRepository.
/// </summary>
public sealed partial class HospitalRepository
{
    public async Task<IReadOnlyList<Laboratorio>> ObtenerLaboratoriosAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var labs = new List<Laboratorio>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand(
                "SELECT id, codigo, nombre, tipo, ubicacion, especialidad_analisis, convenio_id, activo FROM laboratorios WHERE activo = 1 ORDER BY id;",
                conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                labs.Add(new Laboratorio
                {
                    Id = reader.GetInt32("id"),
                    Codigo = reader.GetString("codigo"),
                    Nombre = reader.GetString("nombre"),
                    Tipo = Enum.Parse<TipoLaboratorio>(reader.GetString("tipo"), ignoreCase: true),
                    Ubicacion = reader.GetString("ubicacion"),
                    EspecialidadAnalisis = reader.GetString("especialidad_analisis"),
                    ConvenioId = reader.IsDBNull("convenio_id") ? null : reader.GetInt32("convenio_id"),
                    Activo = reader.GetBoolean("activo")
                });
            }

            return labs;
        }

        lock (dataStore.SyncRoot)
        {
            return [.. dataStore.Laboratorios.Where(l => l.Activo)];
        }
    }

    public async Task<IReadOnlyList<SalidaLaboratorioDto>> ObtenerSalidasLaboratorioAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var salidas = new List<SalidaLaboratorioDto>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT
                    sl.id,
                    sl.atencion_id,
                    sl.paciente_id,
                    p.cedula AS paciente_cedula,
                    p.nombres AS paciente_nombres,
                    m.nombres AS medico_nombre,
                    l.nombre AS laboratorio_nombre,
                    l.tipo AS laboratorio_tipo,
                    sl.examen_solicitado,
                    sl.motivo_clinico,
                    sl.prioridad,
                    sl.estado,
                    sl.resultado_resumen,
                    sl.fecha_salida,
                    sl.fecha_retorno
                FROM salidas_laboratorio sl
                INNER JOIN pacientes p ON p.id = sl.paciente_id
                INNER JOIN medicos m ON m.id = sl.medico_id
                INNER JOIN laboratorios l ON l.id = sl.laboratorio_id
                ORDER BY sl.fecha_salida DESC;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                salidas.Add(new SalidaLaboratorioDto
                {
                    SalidaId = reader.GetInt32("id"),
                    AtencionId = reader.IsDBNull("atencion_id") ? null : reader.GetInt32("atencion_id"),
                    PacienteId = reader.GetInt32("paciente_id"),
                    PacienteCedula = reader.GetString("paciente_cedula"),
                    PacienteNombres = reader.GetString("paciente_nombres"),
                    MedicoSolicitante = reader.GetString("medico_nombre"),
                    LaboratorioNombre = reader.GetString("laboratorio_nombre"),
                    TipoLaboratorio = Enum.Parse<TipoLaboratorio>(reader.GetString("laboratorio_tipo"), ignoreCase: true),
                    ExamenSolicitado = reader.GetString("examen_solicitado"),
                    MotivoClinico = reader.GetString("motivo_clinico"),
                    Prioridad = Enum.Parse<PrioridadLaboratorio>(reader.GetString("prioridad"), ignoreCase: true),
                    Estado = Enum.Parse<EstadoSalidaLaboratorio>(reader.GetString("estado"), ignoreCase: true),
                    ResultadoResumen = reader.IsDBNull("resultado_resumen") ? null : reader.GetString("resultado_resumen"),
                    FechaSalida = reader.GetDateTime("fecha_salida"),
                    FechaRetorno = reader.IsDBNull("fecha_retorno") ? null : reader.GetDateTime("fecha_retorno")
                });
            }

            return salidas;
        }

        lock (dataStore.SyncRoot)
        {
            return [.. dataStore.SalidasLaboratorio
                .OrderByDescending(s => s.FechaSalida)
                .Select(s =>
                {
                    var p = dataStore.Pacientes.First(x => x.Id == s.PacienteId);
                    var m = dataStore.Medicos.First(x => x.Id == s.MedicoId);
                    var l = dataStore.Laboratorios.First(x => x.Id == s.LaboratorioId);
                    return new SalidaLaboratorioDto
                    {
                        SalidaId = s.Id,
                        AtencionId = s.AtencionId,
                        PacienteId = s.PacienteId,
                        PacienteCedula = p.Cedula,
                        PacienteNombres = p.Nombres,
                        MedicoSolicitante = m.Nombres,
                        LaboratorioNombre = l.Nombre,
                        TipoLaboratorio = l.Tipo,
                        ExamenSolicitado = s.ExamenSolicitado,
                        MotivoClinico = s.MotivoClinico,
                        Prioridad = s.Prioridad,
                        Estado = s.Estado,
                        ResultadoResumen = s.ResultadoResumen,
                        FechaSalida = s.FechaSalida,
                        FechaRetorno = s.FechaRetorno
                    };
                })];
        }
    }

    public async Task<int> RegistrarSalidaLaboratorioAsync(NuevaSalidaLaboratorioRequest request, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            const string sqlInsert = """
                INSERT INTO salidas_laboratorio (
                    atencion_id, paciente_id, medico_id, laboratorio_id,
                    examen_solicitado, motivo_clinico, prioridad, estado
                )
                VALUES (
                    @atencionId, @pacienteId, @medicoId, @labId,
                    @examen, @motivo, @prioridad, 'EnLaboratorio'
                );
                SELECT LAST_INSERT_ID();
                """;

            await using var cmd = new MySqlCommand(sqlInsert, conn, tx);
            cmd.Parameters.AddWithValue("@atencionId", request.AtencionId.HasValue ? request.AtencionId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@pacienteId", request.PacienteId);
            cmd.Parameters.AddWithValue("@medicoId", request.MedicoId);
            cmd.Parameters.AddWithValue("@labId", request.LaboratorioId);
            cmd.Parameters.AddWithValue("@examen", request.ExamenSolicitado.Trim());
            cmd.Parameters.AddWithValue("@motivo", request.MotivoClinico.Trim());
            cmd.Parameters.AddWithValue("@prioridad", request.Prioridad.ToString());

            var nuevoId = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));

            if (request.AtencionId.HasValue)
            {
                await using var cmdAtencion = new MySqlCommand(
                    "UPDATE atenciones SET requiere_laboratorio = 1, estado = 'EnLaboratorio' WHERE id = @id AND estado <> 'AltaMedica';",
                    conn,
                    tx);
                cmdAtencion.Parameters.AddWithValue("@id", request.AtencionId.Value);
                await cmdAtencion.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevoId;
        }

        lock (dataStore.SyncRoot)
        {
            var salida = new SalidaLaboratorio
            {
                Id = dataStore.SiguienteSalidaId(),
                AtencionId = request.AtencionId,
                PacienteId = request.PacienteId,
                MedicoId = request.MedicoId,
                LaboratorioId = request.LaboratorioId,
                ExamenSolicitado = request.ExamenSolicitado.Trim(),
                MotivoClinico = request.MotivoClinico.Trim(),
                Prioridad = request.Prioridad,
                Estado = EstadoSalidaLaboratorio.EnLaboratorio,
                FechaSalida = DateTime.Now
            };
            dataStore.SalidasLaboratorio.Add(salida);

            var atencion = dataStore.Atenciones.FirstOrDefault(a => a.Id == request.AtencionId && a.Estado != EstadoAtencion.AltaMedica);
            atencion?.RequiereLaboratorio = true;
            atencion?.Estado = EstadoAtencion.EnLaboratorio;

            return salida.Id;
        }
    }

    public async Task CompletarSalidaLaboratorioAsync(int salidaId, string resultadoResumen, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using var cmdUpdate = new MySqlCommand("""
                UPDATE salidas_laboratorio
                SET estado = 'ResultadoRecibido',
                    resultado_resumen = @resultado,
                    fecha_retorno = CURRENT_TIMESTAMP
                WHERE id = @id;
                """, conn, tx);
            cmdUpdate.Parameters.AddWithValue("@resultado", resultadoResumen);
            cmdUpdate.Parameters.AddWithValue("@id", salidaId);
            await cmdUpdate.ExecuteNonQueryAsync(ct);

            await using var cmdRestore = new MySqlCommand("""
                UPDATE atenciones a
                INNER JOIN salidas_laboratorio sl ON sl.atencion_id = a.id
                SET a.estado = 'EnAtencion'
                WHERE sl.id = @id
                  AND a.estado = 'EnLaboratorio'
                  AND fn_paciente_tiene_salida_activa(a.paciente_id) = 0;
                """, conn, tx);
            cmdRestore.Parameters.AddWithValue("@id", salidaId);
            await cmdRestore.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
            return;
        }

        lock (dataStore.SyncRoot)
        {
            var salida = dataStore.SalidasLaboratorio.FirstOrDefault(s => s.Id == salidaId);
            if (salida is not null)
            {
                salida.Estado = EstadoSalidaLaboratorio.ResultadoRecibido;
                salida.ResultadoResumen = resultadoResumen;
                salida.FechaRetorno = DateTime.Now;

                var tieneOtrasActivas = dataStore.SalidasLaboratorio.Any(s =>
                    s.PacienteId == salida.PacienteId && s.Estado != EstadoSalidaLaboratorio.ResultadoRecibido);

                if (!tieneOtrasActivas)
                {
                    var atencion = dataStore.Atenciones.FirstOrDefault(a => a.Id == salida.AtencionId && a.Estado == EstadoAtencion.EnLaboratorio);
                    atencion?.Estado = EstadoAtencion.EnAtencion;
                }
            }
        }
    }
}
