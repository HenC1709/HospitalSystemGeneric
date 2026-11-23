using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;

namespace HospitalSystem.Application.Interfaces;

public interface IHospitalRepository
{
    string CadenaConexionActual { get; }
    bool ModoMySqlActivo { get; }
    void ActualizarCadenaConexion(string connectionString);

    Task<EstadoConexionDto> ProbarConexionMySqlAsync(CancellationToken ct = default);
    Task InicializarBaseDeDatosMySqlAsync(string rutaSchemaSql, string rutaSeedSql, CancellationToken ct = default);

    Task<DashboardHospitalDto> ObtenerIndicadoresAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalaResumenDto>> ObtenerSalasResumenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Cama>> ObtenerCamasDisponiblesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Convenio>> ObtenerConveniosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Laboratorio>> ObtenerLaboratoriosAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Medico>> ObtenerMedicosAsync(bool soloDelDia = false, CancellationToken ct = default);
    Task<int> RegistrarMedicoAsync(NuevoMedicoRequest request, CancellationToken ct = default);
    Task CambiarGuardiaMedicoAsync(int medicoId, bool enGuardiaHoy, CancellationToken ct = default);

    Task<IReadOnlyList<ResumenPacienteDto>> ObtenerPacientesResumenAsync(bool soloActivos = false, CancellationToken ct = default);
    Task<int> RegistrarPacienteAsync(RegistroPacienteRequest request, CancellationToken ct = default);
    Task DarAltaAtencionAsync(int atencionId, CancellationToken ct = default);

    Task<IReadOnlyList<SalidaLaboratorioDto>> ObtenerSalidasLaboratorioAsync(CancellationToken ct = default);
    Task<int> RegistrarSalidaLaboratorioAsync(NuevaSalidaLaboratorioRequest request, CancellationToken ct = default);
    Task CompletarSalidaLaboratorioAsync(int salidaId, string resultadoResumen, CancellationToken ct = default);
}
