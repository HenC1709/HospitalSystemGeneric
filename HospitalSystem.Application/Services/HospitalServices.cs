using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Interfaces;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Exceptions;

namespace HospitalSystem.Application.Services;

public sealed class HospitalServices(IHospitalRepository repository)
{
    public string CadenaConexionActual => repository.CadenaConexionActual;
    public bool ModoMySqlActivo => repository.ModoMySqlActivo;

    public void ConfigurarCadenaConexion(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new HospitalValidationException("La cadena de conexión a MySQL no puede estar vacía.");

        repository.ActualizarCadenaConexion(connectionString.Trim());
    }

    public Task<EstadoConexionDto> ProbarConexionMySqlAsync(CancellationToken ct = default)
        => repository.ProbarConexionMySqlAsync(ct);

    public Task InicializarEsquemaMySqlAsync(string rutaSchemaSql, string rutaSeedSql, CancellationToken ct = default)
        => repository.InicializarBaseDeDatosMySqlAsync(rutaSchemaSql, rutaSeedSql, ct);

    public Task<DashboardHospitalDto> ObtenerDashboardAsync(CancellationToken ct = default)
        => repository.ObtenerIndicadoresAsync(ct);

    public Task<IReadOnlyList<SalaResumenDto>> ObtenerSalasAsync(CancellationToken ct = default)
        => repository.ObtenerSalasResumenAsync(ct);

    public Task<IReadOnlyList<Cama>> ObtenerCamasDisponiblesAsync(CancellationToken ct = default)
        => repository.ObtenerCamasDisponiblesAsync(ct);

    public Task<IReadOnlyList<Convenio>> ObtenerConveniosAsync(CancellationToken ct = default)
        => repository.ObtenerConveniosAsync(ct);

    public Task<IReadOnlyList<Laboratorio>> ObtenerLaboratoriosAsync(CancellationToken ct = default)
        => repository.ObtenerLaboratoriosAsync(ct);

    public Task<IReadOnlyList<Medico>> ObtenerMedicosAsync(bool soloDelDia = false, CancellationToken ct = default)
        => repository.ObtenerMedicosAsync(soloDelDia, ct);

    public async Task<int> RegistrarMedicoAsync(NuevoMedicoRequest request, CancellationToken ct = default)
    {
        // Validación a través de la entidad de dominio (propiedades con 'field' en C# 14)
        _ = new Medico
        {
            CedulaProfesional = request.CedulaProfesional,
            Nombres = request.Nombres,
            Especialidad = request.Especialidad,
            Turno = request.Turno,
            Consultorio = string.IsNullOrWhiteSpace(request.Consultorio) ? "Consultorio General" : request.Consultorio.Trim(),
            EnGuardiaHoy = request.EnGuardiaHoy
        };

        return await repository.RegistrarMedicoAsync(request, ct);
    }

    public Task CambiarGuardiaMedicoAsync(int medicoId, bool enGuardiaHoy, CancellationToken ct = default)
        => repository.CambiarGuardiaMedicoAsync(medicoId, enGuardiaHoy, ct);

    public Task<IReadOnlyList<ResumenPacienteDto>> ObtenerPacientesAsync(bool soloActivos = false, CancellationToken ct = default)
        => repository.ObtenerPacientesResumenAsync(soloActivos, ct);

    public async Task<int> RegistrarPacienteAsync(RegistroPacienteRequest request, CancellationToken ct = default)
    {
        // Valida obligatoriamente Cédula, Nombres y Síntomas usando nuestra entidad C# 14
        _ = new Paciente
        {
            Cedula = request.Cedula,
            Nombres = request.Nombres,
            SintomasActuales = request.Sintomas,
            Sexo = request.Sexo,
            ConvenioId = request.ConvenioId
        };

        if (request.MedicoId <= 0)
            throw new HospitalValidationException("Debe seleccionar un médico tratante del día.");

        if (request.RequiereLaboratorio && (!request.LaboratorioId.HasValue || request.LaboratorioId.Value <= 0))
            throw new HospitalValidationException("Si el paciente requiere salida a laboratorio, seleccione el laboratorio destino.");

        return await repository.RegistrarPacienteAsync(request, ct);
    }

    public Task DarAltaAtencionAsync(int atencionId, CancellationToken ct = default)
    {
        if (atencionId <= 0)
            throw new HospitalValidationException("La atención seleccionada no es válida para dar de alta.");

        return repository.DarAltaAtencionAsync(atencionId, ct);
    }

    public Task<IReadOnlyList<SalidaLaboratorioDto>> ObtenerSalidasLaboratorioAsync(CancellationToken ct = default)
        => repository.ObtenerSalidasLaboratorioAsync(ct);

    public Task<int> RegistrarSalidaLaboratorioAsync(NuevaSalidaLaboratorioRequest request, CancellationToken ct = default)
    {
        if (request.PacienteId <= 0)
            throw new HospitalValidationException("Debe seleccionar un paciente válido.");
        if (request.MedicoId <= 0)
            throw new HospitalValidationException("Debe seleccionar el médico que ordena la salida.");
        if (request.LaboratorioId <= 0)
            throw new HospitalValidationException("Debe elegir el laboratorio de destino.");
        if (string.IsNullOrWhiteSpace(request.ExamenSolicitado))
            throw new HospitalValidationException("Especifique el examen o estudio solicitado.");
        if (string.IsNullOrWhiteSpace(request.MotivoClinico))
            throw new HospitalValidationException("Indique el motivo clínico o síntomas para el laboratorio.");

        return repository.RegistrarSalidaLaboratorioAsync(request, ct);
    }

    public Task CompletarSalidaLaboratorioAsync(int salidaId, string resultadoResumen, CancellationToken ct = default)
    {
        if (salidaId <= 0)
            throw new HospitalValidationException("Identificador de salida a laboratorio inválido.");
        if (string.IsNullOrWhiteSpace(resultadoResumen))
            throw new HospitalValidationException("Ingrese el resumen del resultado recibido del laboratorio.");

        return repository.CompletarSalidaLaboratorioAsync(salidaId, resultadoResumen.Trim(), ct);
    }
}
