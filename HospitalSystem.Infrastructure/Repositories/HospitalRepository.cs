using System.Data;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Interfaces;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;
using HospitalSystem.Core.Extensions;
using HospitalSystem.Infrastructure.Persistence;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

public sealed class HospitalRepository(HospitalDataStore dataStore) : IHospitalRepository
{
    private bool _verificacionInicialRealizada;

    public string CadenaConexionActual => dataStore.ConnectionString;
    public bool ModoMySqlActivo { get; private set; }

    public void ActualizarCadenaConexion(string connectionString)
    {
        dataStore.ConnectionString = connectionString;
        _verificacionInicialRealizada = false;
    }

    private async Task AsegurarModoConexionAsync(CancellationToken ct)
    {
        if (_verificacionInicialRealizada)
            return;

        _verificacionInicialRealizada = true;
        var estado = await ProbarConexionMySqlAsync(ct);
        ModoMySqlActivo = estado.Conectado;
    }

    public async Task<EstadoConexionDto> ProbarConexionMySqlAsync(CancellationToken ct = default)
    {
        var builder = new MySqlConnectionStringBuilder(dataStore.ConnectionString)
        {
            ConnectionTimeout = 3
        };

        try
        {
            await using var conn = new MySqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'pacientes';",
                conn);
            var tablasCreadas = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;

            ModoMySqlActivo = tablasCreadas;
            _verificacionInicialRealizada = true;

            return tablasCreadas
                ? new EstadoConexionDto(
                    true,
                    builder.Server,
                    builder.Database,
                    conn.ServerVersion,
                    $"Conectado a MySQL {conn.ServerVersion} ({builder.Database}).")
                : new EstadoConexionDto(
                    false,
                    builder.Server,
                    builder.Database,
                    conn.ServerVersion,
                    $"Servidor MySQL activo ({conn.ServerVersion}), pero faltan las tablas. Usa 'Inicializar BD (Schema + Seed)'.");
        }
        catch (Exception ex)
        {
            ModoMySqlActivo = false;
            _verificacionInicialRealizada = true;
            return new EstadoConexionDto(
                false,
                builder.Server,
                builder.Database,
                "N/D",
                $"Sin conexión a MySQL ({ex.Message}). Operando con respaldo local en memoria.");
        }
    }

    public async Task InicializarBaseDeDatosMySqlAsync(string rutaSchemaSql, string rutaSeedSql, CancellationToken ct = default)
    {
        if (!File.Exists(rutaSchemaSql))
            throw new HospitalException($"No se encontró el archivo schema.sql en: {rutaSchemaSql}");
        if (!File.Exists(rutaSeedSql))
            throw new HospitalException($"No se encontró el archivo seed.sql en: {rutaSeedSql}");

        try
        {
            var schemaContent = await File.ReadAllTextAsync(rutaSchemaSql, ct);
            var seedContent = await File.ReadAllTextAsync(rutaSeedSql, ct);

            await using var conn = dataStore.CrearConexionServidor();
            await conn.OpenAsync(ct);

            await HospitalDataStore.EjecutarScriptSqlAsync(conn, schemaContent, ct);
            await HospitalDataStore.EjecutarScriptSqlAsync(conn, seedContent, ct);

            ModoMySqlActivo = true;
            _verificacionInicialRealizada = true;
        }
        catch (Exception ex)
        {
            ModoMySqlActivo = false;
            throw new DatabaseConnectionException($"Error al inicializar MySQL: {ex.Message}", ex);
        }
    }

    public async Task<DashboardHospitalDto> ObtenerIndicadoresAsync(CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
        {
            await using var conn = dataStore.CrearConexion();
            await conn.OpenAsync(ct);

            // Consulta la vista vw_indicadores_hospital que invoca fn_camas_disponibles(), fn_total_salas_activas() y fn_medicos_del_dia()
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

        return new DashboardHospitalDto
        {
            CamasDisponibles = dataStore.Camas.Count(c => !c.Ocupada),
            CamasTotales = dataStore.Camas.Count,
            CantidadSalas = dataStore.Salas.Count(s => s.Activa),
            MedicosDelDia = dataStore.Medicos.Count(m => m.Activo && m.EnGuardiaHoy),
            PacientesActivos = dataStore.Atenciones.Count(a => a.Estado != EstadoAtencion.AltaMedica),
            SalidasLaboratorioActivas = dataStore.SalidasLaboratorio.Count(s => s.Estado != EstadoSalidaLaboratorio.ResultadoRecibido),
            UsandoMySql = false,
            OrigenDatos = "Modo Respaldo en Memoria (Conecta MySQL arriba)"
        };
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

        return [.. dataStore.Camas.Where(c => !c.Ocupada).OrderBy(c => c.Codigo)];
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

        return [.. dataStore.Convenios.Where(c => c.Activo)];
    }

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

        return [.. dataStore.Laboratorios.Where(l => l.Activo)];
    }

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

        var query = dataStore.Medicos.Where(m => m.Activo);
        if (soloDelDia)
            query = query.Where(m => m.EnGuardiaHoy);

        return [.. query.OrderByDescending(m => m.EnGuardiaHoy).ThenBy(m => m.Nombres)];
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

        var medico = dataStore.Medicos.FirstOrDefault(m => m.Id == medicoId);
        // C# 14: asignación condicional nula
        medico?.EnGuardiaHoy = enGuardiaHoy;
    }

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

    public async Task<int> RegistrarPacienteAsync(RegistroPacienteRequest request, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
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
            cmd.Parameters.AddWithValue("p_convenio_id", request.ConvenioId.HasValue && request.ConvenioId > 0 ? request.ConvenioId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("p_medico_id", request.MedicoId);
            cmd.Parameters.AddWithValue("p_cama_id", request.CamaId.HasValue && request.CamaId > 0 ? request.CamaId.Value : DBNull.Value);
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

        if (request.CamaId is > 0)
            dataStore.OcuparCama(request.CamaId);

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

    public async Task DarAltaAtencionAsync(int atencionId, CancellationToken ct = default)
    {
        await AsegurarModoConexionAsync(ct);

        if (ModoMySqlActivo)
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

        var atencion = dataStore.Atenciones.FirstOrDefault(a => a.Id == atencionId);
        if (atencion is not null)
        {
            atencion.Estado = EstadoAtencion.AltaMedica;
            atencion.FechaEgreso = DateTime.Now;
            dataStore.LiberarCama(atencion.CamaId);
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
