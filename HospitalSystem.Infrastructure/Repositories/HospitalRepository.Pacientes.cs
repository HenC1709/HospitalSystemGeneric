using System.Data;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

/// <summary>
/// Módulo Pacientes, Admisiones y Altas Médicas protegidas contra concurrencia.
/// </summary>
public sealed partial class HospitalRepository
{
    public async Task<IReadOnlyList<ResumenPacienteDto>> ObtenerPacientesResumenAsync(bool soloActivos = false, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            var lista = new List<ResumenPacienteDto>();
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            var filtroEstado = soloActivos ? "WHERE a.estado IN ('EnAtencion', 'EnLaboratorio')" : string.Empty;
            var sql = $"""
                SELECT
                    p.id AS paciente_id,
                    a.id AS atencion_id,
                    p.cedula,
                    p.nombres,
                    p.sexo,
                    COALESCE(a.sintomas, p.sintomas_actuales) AS sintomas,
                    COALESCE(cv.nombre, 'Particular') AS convenio_nombre,
                    COALESCE(m.nombres, 'Sin médico') AS medico_nombre,
                    COALESCE(a.medico_id, 0) AS medico_id,
                    CASE
                        WHEN c.id IS NULL THEN 'Ambulatorio'
                        ELSE CONCAT(c.codigo, ' (', s.nombre, ')')
                    END AS cama_desc,
                    COALESCE(a.estado, 'AltaMedica') AS estado,
                    fn_paciente_tiene_salida_activa(p.id) AS salida_lab_activa,
                    COALESCE(a.fecha_ingreso, p.fecha_registro) AS fecha_ingreso
                FROM pacientes p
                LEFT JOIN convenios cv ON cv.id = p.convenio_id
                LEFT JOIN atenciones a ON a.id = (
                    SELECT a2.id FROM atenciones a2
                    WHERE a2.paciente_id = p.id
                    ORDER BY a2.id DESC LIMIT 1
                )
                LEFT JOIN medicos m ON m.id = a.medico_id
                LEFT JOIN camas c ON c.id = a.cama_id
                LEFT JOIN salas s ON s.id = c.sala_id
                {filtroEstado}
                ORDER BY fecha_ingreso DESC;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                lista.Add(new ResumenPacienteDto
                {
                    PacienteId = reader.GetInt32("paciente_id"),
                    AtencionId = reader.IsDBNull("atencion_id") ? null : reader.GetInt32("atencion_id"),
                    Cedula = reader.GetString("cedula"),
                    Nombres = reader.GetString("nombres"),
                    Sexo = Enum.Parse<Sexo>(reader.GetString("sexo"), ignoreCase: true),
                    Sintomas = reader.GetString("sintomas"),
                    ConvenioNombre = reader.GetString("convenio_nombre"),
                    MedicoAsignado = reader.GetString("medico_nombre"),
                    MedicoId = reader.GetInt32("medico_id"),
                    CamaDescripcion = reader.GetString("cama_desc"),
                    Estado = Enum.Parse<EstadoAtencion>(reader.GetString("estado"), ignoreCase: true),
                    TieneSalidaLaboratorioActiva = reader.GetBoolean("salida_lab_activa"),
                    FechaIngreso = reader.GetDateTime("fecha_ingreso")
                });
            }

            return lista;
        }

        lock (dataStore.SyncRoot)
        {
            var resultado = dataStore.Pacientes
                .Select(p =>
                {
                    var ultimaAtencion = dataStore.Atenciones
                        .Where(a => a.PacienteId == p.Id)
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefault();

                    var medico = dataStore.Medicos.FirstOrDefault(m => m.Id == ultimaAtencion?.MedicoId);
                    var cama = dataStore.Camas.FirstOrDefault(c => c.Id == ultimaAtencion?.CamaId);
                    var convenio = dataStore.Convenios.FirstOrDefault(c => c.Id == p.ConvenioId);
                    var salidaActiva = dataStore.SalidasLaboratorio.Any(s =>
                        s.PacienteId == p.Id && s.Estado != EstadoSalidaLaboratorio.ResultadoRecibido);

                    return new ResumenPacienteDto
                    {
                        PacienteId = p.Id,
                        AtencionId = ultimaAtencion?.Id,
                        Cedula = p.Cedula,
                        Nombres = p.Nombres,
                        Sexo = p.Sexo,
                        Sintomas = ultimaAtencion?.Sintomas ?? p.SintomasActuales,
                        ConvenioNombre = convenio?.Nombre ?? "Particular",
                        MedicoAsignado = medico?.Nombres ?? "Sin asignar",
                        MedicoId = medico?.Id ?? 0,
                        CamaDescripcion = cama is null ? "Ambulatorio" : $"{cama.Codigo} ({cama.SalaNombre})",
                        Estado = ultimaAtencion?.Estado ?? EstadoAtencion.AltaMedica,
                        TieneSalidaLaboratorioActiva = salidaActiva,
                        FechaIngreso = ultimaAtencion?.FechaIngreso ?? p.FechaRegistro
                    };
                });

            if (soloActivos)
                resultado = resultado.Where(r => r.Estado != EstadoAtencion.AltaMedica);

            return [.. resultado.OrderByDescending(r => r.FechaIngreso)];
        }
    }

    public async Task<int> RegistrarPacienteAsync(RegistroPacienteRequest request, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            try
            {
                await using var conn = dataStore.CrearConexion();
                await conn.OpenAsync(ct);

                await using var cmd = new MySqlCommand("sp_registrar_ingreso_paciente", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_cedula", request.Cedula.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("p_nombres", request.Nombres.Trim());
                cmd.Parameters.AddWithValue("p_sexo", request.Sexo.ToString());
                cmd.Parameters.AddWithValue("p_sintomas", request.Sintomas.Trim());
                cmd.Parameters.AddWithValue("p_convenio_id", request.ConvenioId is > 0 ? request.ConvenioId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_medico_id", request.MedicoId);
                cmd.Parameters.AddWithValue("p_cama_id", request.CamaId is > 0 ? request.CamaId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_requiere_lab", request.RequiereLaboratorio ? 1 : 0);
                cmd.Parameters.AddWithValue("p_laboratorio_id", request.RequiereLaboratorio && request.LaboratorioId.HasValue ? request.LaboratorioId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_examen_lab", request.ExamenSolicitado?.Trim() ?? string.Empty);
                cmd.Parameters.AddWithValue("p_prioridad_lab", request.PrioridadLaboratorio.ToString());

                var outPaciente = new MySqlParameter("p_paciente_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var outAtencion = new MySqlParameter("p_atencion_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outPaciente);
                cmd.Parameters.Add(outAtencion);

                await cmd.ExecuteNonQueryAsync(ct);
                return Convert.ToInt32(outAtencion.Value);
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                throw new HospitalValidationException(ex.Message);
            }
        }

        lock (dataStore.SyncRoot)
        {
            // Primero validamos y ocupamos la cama atómicamente antes de crear la atención
            if (request.CamaId is > 0)
                dataStore.OcuparCamaDisponible(request.CamaId.Value);

            var cedulaNorm = request.Cedula.Trim().ToUpperInvariant();
            var paciente = dataStore.Pacientes.FirstOrDefault(p => p.Cedula.Equals(cedulaNorm, StringComparison.OrdinalIgnoreCase));
            if (paciente is null)
            {
                paciente = new Paciente
                {
                    Id = dataStore.SiguientePacienteId(),
                    Cedula = cedulaNorm,
                    Nombres = request.Nombres,
                    Sexo = request.Sexo,
                    SintomasActuales = request.Sintomas,
                    ConvenioId = request.ConvenioId,
                    FechaRegistro = DateTime.Now
                };
                dataStore.Pacientes.Add(paciente);
            }
            else
            {
                paciente.Nombres = request.Nombres;
                paciente.Sexo = request.Sexo;
                paciente.SintomasActuales = request.Sintomas;
                paciente.ConvenioId = request.ConvenioId;
            }

            var estadoInicial = request.RequiereLaboratorio && request.LaboratorioId is > 0
                ? EstadoAtencion.EnLaboratorio
                : EstadoAtencion.EnAtencion;

            var atencion = new Atencion
            {
                Id = dataStore.SiguienteAtencionId(),
                PacienteId = paciente.Id,
                MedicoId = request.MedicoId,
                CamaId = request.CamaId is > 0 ? request.CamaId : null,
                Sintomas = request.Sintomas.Trim(),
                RequiereLaboratorio = request.RequiereLaboratorio,
                Estado = estadoInicial,
                FechaIngreso = DateTime.Now
            };
            dataStore.Atenciones.Add(atencion);

            if (request.RequiereLaboratorio && request.LaboratorioId is > 0)
            {
                dataStore.SalidasLaboratorio.Add(new SalidaLaboratorio
                {
                    Id = dataStore.SiguienteSalidaId(),
                    AtencionId = atencion.Id,
                    PacienteId = paciente.Id,
                    MedicoId = request.MedicoId,
                    LaboratorioId = request.LaboratorioId.Value,
                    ExamenSolicitado = string.IsNullOrWhiteSpace(request.ExamenSolicitado)
                        ? "Perfil Clínico General"
                        : request.ExamenSolicitado.Trim(),
                    MotivoClinico = request.Sintomas.Trim(),
                    Prioridad = request.PrioridadLaboratorio,
                    Estado = EstadoSalidaLaboratorio.EnLaboratorio,
                    FechaSalida = DateTime.Now
                });
            }

            return atencion.Id;
        }
    }

    public async Task DarAltaAtencionAsync(int atencionId, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            try
            {
                await using var conn = dataStore.CrearConexion();
                await conn.OpenAsync(ct);

                await using var cmd = new MySqlCommand("sp_dar_alta_atencion", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("p_atencion_id", atencionId);
                await cmd.ExecuteNonQueryAsync(ct);
                return;
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                throw new HospitalValidationException(ex.Message);
            }
        }

        lock (dataStore.SyncRoot)
        {
            var atencion = dataStore.Atenciones.FirstOrDefault(a => a.Id == atencionId)
                ?? throw new HospitalValidationException("La atención indicada no existe.");

            if (atencion.Estado == EstadoAtencion.AltaMedica)
                throw new HospitalValidationException("La atención ya cuenta con alta médica previa.");

            atencion.Estado = EstadoAtencion.AltaMedica;
            atencion.FechaEgreso = DateTime.Now;
            dataStore.LiberarCamaSiNoReasignada(atencion.CamaId, atencion.Id);
        }
    }
}
