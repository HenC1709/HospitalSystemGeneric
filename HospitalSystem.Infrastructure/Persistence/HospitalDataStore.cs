using System.Text;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Persistence;

/// <summary>
/// Gestiona la factoría de conexiones MySQL, el ejecutor de scripts SQL con soporte DELIMITER
/// y un almacén en memoria con control de concurrencia y datos ficticios de demostración.
/// </summary>
public sealed class HospitalDataStore
{
    private readonly Lock _syncRoot = new();
    private int _nextPacienteId = 4;
    private int _nextAtencionId = 4;
    private int _nextMedicoId = 9;
    private int _nextSalidaId = 4;

    public Lock SyncRoot => _syncRoot;

    public string ConnectionString
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? "Server=localhost;Port=3306;Database=hospital_system_db;Uid=root;Pwd=;AllowUserVariables=True;"
            : EnsureAllowUserVariables(value);
    }

    public List<Convenio> Convenios { get; } = [];
    public List<Sala> Salas { get; } = [];
    public List<Cama> Camas { get; } = [];
    public List<Medico> Medicos { get; } = [];
    public List<Laboratorio> Laboratorios { get; } = [];
    public List<Paciente> Pacientes { get; } = [];
    public List<Atencion> Atenciones { get; } = [];
    public List<SalidaLaboratorio> SalidasLaboratorio { get; } = [];

    public HospitalDataStore(string? connectionString = null)
    {
        ConnectionString = connectionString ?? string.Empty;
        CargarDatosSemillaMemoria();
    }

    public MySqlConnection CrearConexion() => new(ConnectionString);

    public MySqlConnection CrearConexionServidor()
    {
        var builder = new MySqlConnectionStringBuilder(ConnectionString)
        {
            Database = string.Empty,
            AllowUserVariables = true
        };
        return new MySqlConnection(builder.ConnectionString);
    }

    private static string EnsureAllowUserVariables(string cs)
    {
        var builder = new MySqlConnectionStringBuilder(cs)
        {
            AllowUserVariables = true
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Ejecuta archivos .sql de MySQL soportando cambios de DELIMITER (funciones y procedimientos).
    /// </summary>
    public static async Task EjecutarScriptSqlAsync(MySqlConnection connection, string sqlScript, CancellationToken ct = default)
    {
        var delimiter = ";";
        var buffer = new StringBuilder();
        var lines = sqlScript.Replace("\r\n", "\n").Split('\n');

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                continue;

            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed["DELIMITER ".Length..].Trim();
                continue;
            }

            if (rawLine.TrimEnd().EndsWith(delimiter, StringComparison.Ordinal))
            {
                var lineWithoutDelimiter = rawLine.TrimEnd()[..^delimiter.Length];
                buffer.AppendLine(lineWithoutDelimiter);

                var statement = buffer.ToString().Trim();
                buffer.Clear();

                if (!string.IsNullOrWhiteSpace(statement))
                {
                    await using var cmd = new MySqlCommand(statement, connection);
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }
            else
            {
                buffer.AppendLine(rawLine);
            }
        }

        var remaining = buffer.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            await using var cmd = new MySqlCommand(remaining, connection);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public int SiguientePacienteId() => _nextPacienteId++;
    public int SiguienteAtencionId() => _nextAtencionId++;
    public int SiguienteMedicoId() => _nextMedicoId++;
    public int SiguienteSalidaId() => _nextSalidaId++;

    /// <summary>
    /// Libera la cama únicamente si ninguna otra atención activa la mantiene asignada.
    /// </summary>
    public void LiberarCamaSiNoReasignada(int? camaId, int atencionDadaDeAltaId)
    {
        if (camaId is null)
            return;

        var ocupadaPorOtraAtencionActiva = Atenciones.Any(a =>
            a.CamaId == camaId &&
            a.Id != atencionDadaDeAltaId &&
            a.Estado != EstadoAtencion.AltaMedica);

        if (ocupadaPorOtraAtencionActiva)
            return;

        var cama = Camas.FirstOrDefault(c => c.Id == camaId);
        // C# 14: asignación condicional nula (?.=)
        cama?.Ocupada = false;
    }

    /// <summary>
    /// Ocupa una cama validando que exista y esté libre; lanza excepción si ya está ocupada.
    /// </summary>
    public void OcuparCamaDisponible(int camaId)
    {
        var cama = Camas.FirstOrDefault(c => c.Id == camaId)
            ?? throw new HospitalValidationException("La cama seleccionada no existe.");

        if (cama.Ocupada)
            throw new HospitalValidationException("La cama seleccionada ya está ocupada o no existe.");

        cama.Ocupada = true;
    }

    private void CargarDatosSemillaMemoria()
    {
        Convenios.AddRange([
            new Convenio { Id = 1, Codigo = "SEG-NAC", Nombre = "Seguro Nacional de Salud", PorcentajeCobertura = 80m },
            new Convenio { Id = 2, Codigo = "CRUZ-AZUL", Nombre = "Previsión Médica Cruz Azul", PorcentajeCobertura = 90m },
            new Convenio { Id = 3, Codigo = "PARTICULAR", Nombre = "Atención Particular Directa", PorcentajeCobertura = 0m },
            new Convenio { Id = 4, Codigo = "CONV-BIOMED", Nombre = "Alianza Diagnóstica Biomédica", PorcentajeCobertura = 75m }
        ]);

        Salas.AddRange([
            new Sala { Id = 1, Codigo = "SAL-URG", Nombre = "Urgencias y Triaje", Piso = 1, Activa = true },
            new Sala { Id = 2, Codigo = "SAL-OBS", Nombre = "Observación Clínica", Piso = 1, Activa = true },
            new Sala { Id = 3, Codigo = "SAL-MIN", Nombre = "Medicina Interna", Piso = 2, Activa = true },
            new Sala { Id = 4, Codigo = "SAL-PED", Nombre = "Pediatría General", Piso = 2, Activa = true },
            new Sala { Id = 5, Codigo = "SAL-UCI", Nombre = "Unidad de Cuidados Intensivos (UCI)", Piso = 3, Activa = true }
        ]);

        Camas.AddRange([
            new Cama { Id = 1,  SalaId = 1, SalaNombre = "Urgencias y Triaje", Codigo = "URG-01", Ocupada = true,  Observacion = "Monitor multiparámetro" },
            new Cama { Id = 2,  SalaId = 1, SalaNombre = "Urgencias y Triaje", Codigo = "URG-02", Ocupada = false, Observacion = "Toma de oxígeno central" },
            new Cama { Id = 3,  SalaId = 1, SalaNombre = "Urgencias y Triaje", Codigo = "URG-03", Ocupada = false, Observacion = "Box de atención rápida" },
            new Cama { Id = 4,  SalaId = 1, SalaNombre = "Urgencias y Triaje", Codigo = "URG-04", Ocupada = false, Observacion = "Box de reanimación" },
            new Cama { Id = 5,  SalaId = 2, SalaNombre = "Observación Clínica", Codigo = "OBS-01", Ocupada = true,  Observacion = "Hidratación endovenosa" },
            new Cama { Id = 6,  SalaId = 2, SalaNombre = "Observación Clínica", Codigo = "OBS-02", Ocupada = false, Observacion = "Observación corta estancia" },
            new Cama { Id = 7,  SalaId = 2, SalaNombre = "Observación Clínica", Codigo = "OBS-03", Ocupada = false, Observacion = "Observación aislamiento ligero" },
            new Cama { Id = 8,  SalaId = 3, SalaNombre = "Medicina Interna", Codigo = "MIN-01", Ocupada = true,  Observacion = "Habitación 201-A" },
            new Cama { Id = 9,  SalaId = 3, SalaNombre = "Medicina Interna", Codigo = "MIN-02", Ocupada = false, Observacion = "Habitación 201-B" },
            new Cama { Id = 10, SalaId = 3, SalaNombre = "Medicina Interna", Codigo = "MIN-03", Ocupada = false, Observacion = "Habitación 202-A" },
            new Cama { Id = 11, SalaId = 3, SalaNombre = "Medicina Interna", Codigo = "MIN-04", Ocupada = false, Observacion = "Habitación 202-B" },
            new Cama { Id = 12, SalaId = 4, SalaNombre = "Pediatría General", Codigo = "PED-01", Ocupada = false, Observacion = "Cuna clínica pediátrica" },
            new Cama { Id = 13, SalaId = 4, SalaNombre = "Pediatría General", Codigo = "PED-02", Ocupada = false, Observacion = "Cama juvenil con acompañante" },
            new Cama { Id = 14, SalaId = 4, SalaNombre = "Pediatría General", Codigo = "PED-03", Ocupada = false, Observacion = "Cama pediátrica monitorizada" },
            new Cama { Id = 15, SalaId = 5, SalaNombre = "Unidad de Cuidados Intensivos (UCI)", Codigo = "UCI-01", Ocupada = false, Observacion = "Ventilación mecánica avanzada" },
            new Cama { Id = 16, SalaId = 5, SalaNombre = "Unidad de Cuidados Intensivos (UCI)", Codigo = "UCI-02", Ocupada = false, Observacion = "Soporte hemodinámico completo" }
        ]);

        foreach (var sala in Salas)
        {
            sala.Camas = [.. Camas.Where(c => c.SalaId == sala.Id)];
        }

        Medicos.AddRange([
            new Medico { Id = 1, CedulaProfesional = "MP-10452", Nombres = "Dra. Valentina Rojas Méndez", Especialidad = "Medicina de Urgencias", Turno = TurnoMedico.Guardia24h, Consultorio = "Urgencias Box 1", EnGuardiaHoy = true },
            new Medico { Id = 2, CedulaProfesional = "MP-18930", Nombres = "Dr. Alejandro Silva Paredes", Especialidad = "Medicina Interna", Turno = TurnoMedico.Manana, Consultorio = "Consultorio 204", EnGuardiaHoy = true },
            new Medico { Id = 3, CedulaProfesional = "MP-22415", Nombres = "Dra. Camila Herrera Soto", Especialidad = "Cardiología Clínica", Turno = TurnoMedico.Manana, Consultorio = "Consultorio 301", EnGuardiaHoy = true },
            new Medico { Id = 4, CedulaProfesional = "MP-30118", Nombres = "Dr. Sebastián Morales Vega", Especialidad = "Infectología y Diagnóstico", Turno = TurnoMedico.Tarde, Consultorio = "Consultorio 208", EnGuardiaHoy = true },
            new Medico { Id = 5, CedulaProfesional = "MP-33890", Nombres = "Dra. Lucía Fernández Castro", Especialidad = "Pediatría", Turno = TurnoMedico.Tarde, Consultorio = "Pabellón Pediátrico 2", EnGuardiaHoy = true },
            new Medico { Id = 6, CedulaProfesional = "MP-41207", Nombres = "Dr. Mateo Navarro Guzmán", Especialidad = "Medicina Intensiva (UCI)", Turno = TurnoMedico.Noche, Consultorio = "UCI Central Piso 3", EnGuardiaHoy = true },
            new Medico { Id = 7, CedulaProfesional = "MP-45901", Nombres = "Dr. Ricardo Mendoza Peña", Especialidad = "Traumatología", Turno = TurnoMedico.Manana, Consultorio = "Consultorio 105", EnGuardiaHoy = false },
            new Medico { Id = 8, CedulaProfesional = "MP-50234", Nombres = "Dra. Gabriela Ortiz Luna", Especialidad = "Neurología", Turno = TurnoMedico.Tarde, Consultorio = "Consultorio 310", EnGuardiaHoy = false }
        ]);

        Laboratorios.AddRange([
            new Laboratorio { Id = 1, Codigo = "LAB-HEM", Nombre = "Laboratorio Central de Hematología y Bioquímica", Tipo = TipoLaboratorio.Interno, Ubicacion = "Planta Baja - Ala Este", EspecialidadAnalisis = "Hemograma, Perfil Renal, Electrolitos y Troponinas" },
            new Laboratorio { Id = 2, Codigo = "LAB-IMG", Nombre = "Unidad de Imagenología y Tomografía", Tipo = TipoLaboratorio.Interno, Ubicacion = "Sótano 1 - Bloque Diagnóstico", EspecialidadAnalisis = "Rayos X Digital, Ecografía y TAC Multicorte" },
            new Laboratorio { Id = 3, Codigo = "LAB-MIC", Nombre = "Laboratorio de Microbiología y Serología", Tipo = TipoLaboratorio.Interno, Ubicacion = "Piso 1 - Ala Norte", EspecialidadAnalisis = "Hemocultivos, PCR Viral, Antígeno Dengue" },
            new Laboratorio { Id = 4, Codigo = "LAB-EXT", Nombre = "Centro Biomédico Especializado (Externo)", Tipo = TipoLaboratorio.ExternoConvenio, Ubicacion = "Av. Ciencias 220 (Ambulancia traslado)", EspecialidadAnalisis = "Resonancia Magnética y Panel Inmunológico Avanzado", ConvenioId = 4 }
        ]);

        Pacientes.AddRange([
            new Paciente { Id = 1, Cedula = "V-19845120", Nombres = "Carlos Eduardo Ramírez Tovar", Sexo = Sexo.Masculino, SintomasActuales = "Fiebre alta 39.2°C de 3 días, mialgias intensas y dolor retroocular", ConvenioId = 1, FechaRegistro = DateTime.Now.AddHours(-5) },
            new Paciente { Id = 2, Cedula = "V-24510988", Nombres = "Mariana Isabel Gómez Pérez", Sexo = Sexo.Femenino, SintomasActuales = "Dolor opresivo en el pecho al esfuerzo, disnea leve y mareos", ConvenioId = 2, FechaRegistro = DateTime.Now.AddHours(-3) },
            new Paciente { Id = 3, Cedula = "V-14203771", Nombres = "Jorge Luis Delgado Rivas", Sexo = Sexo.Masculino, SintomasActuales = "Tos productiva persistente, escalofríos y saturación 93%", ConvenioId = 1, FechaRegistro = DateTime.Now.AddHours(-2) }
        ]);

        Atenciones.AddRange([
            new Atencion { Id = 1, PacienteId = 1, MedicoId = 4, CamaId = 5, Sintomas = Pacientes[0].SintomasActuales, RequiereLaboratorio = true, Estado = EstadoAtencion.EnLaboratorio, FechaIngreso = DateTime.Now.AddHours(-5) },
            new Atencion { Id = 2, PacienteId = 2, MedicoId = 3, CamaId = 1, Sintomas = Pacientes[1].SintomasActuales, RequiereLaboratorio = true, Estado = EstadoAtencion.EnLaboratorio, FechaIngreso = DateTime.Now.AddHours(-3) },
            new Atencion { Id = 3, PacienteId = 3, MedicoId = 2, CamaId = 8, Sintomas = Pacientes[2].SintomasActuales, RequiereLaboratorio = false, Estado = EstadoAtencion.EnAtencion, FechaIngreso = DateTime.Now.AddHours(-2) }
        ]);

        SalidasLaboratorio.AddRange([
            new SalidaLaboratorio { Id = 1, AtencionId = 1, PacienteId = 1, MedicoId = 4, LaboratorioId = 3, ExamenSolicitado = "Hemograma con recuento plaquetario + Antígeno NS1 Dengue", MotivoClinico = "Fiebre aguda y sospecha de trombocitopenia", Prioridad = PrioridadLaboratorio.Urgente, Estado = EstadoSalidaLaboratorio.EnLaboratorio, FechaSalida = DateTime.Now.AddHours(-4) },
            new SalidaLaboratorio { Id = 2, AtencionId = 2, PacienteId = 2, MedicoId = 3, LaboratorioId = 1, ExamenSolicitado = "Troponina ultrasensible seriada + Perfil lipídico", MotivoClinico = "Descartar síndrome coronario agudo", Prioridad = PrioridadLaboratorio.Critica, Estado = EstadoSalidaLaboratorio.PendienteSalida, FechaSalida = DateTime.Now.AddHours(-2) },
            new SalidaLaboratorio { Id = 3, AtencionId = 3, PacienteId = 3, MedicoId = 2, LaboratorioId = 2, ExamenSolicitado = "Radiografía de Tórax PA y Lateral", MotivoClinico = "Evaluar infiltrado basal derecho", Prioridad = PrioridadLaboratorio.Urgente, Estado = EstadoSalidaLaboratorio.ResultadoRecibido, ResultadoResumen = "Infiltrado alveolar basal derecho compatible con neumonía comunitaria.", FechaSalida = DateTime.Now.AddMinutes(-90), FechaRetorno = DateTime.Now.AddMinutes(-30) }
        ]);
    }
}
