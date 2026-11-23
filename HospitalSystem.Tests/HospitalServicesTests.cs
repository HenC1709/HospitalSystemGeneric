using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using HospitalSystem.Core.Enums;
using HospitalSystem.Core.Exceptions;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Repositories;
using Xunit;

namespace HospitalSystem.Tests;

public sealed class HospitalServicesTests
{
    private static (HospitalServices Services, HospitalDataStore Store) CrearSutEnMemoria()
    {
        var store = new HospitalDataStore();
        var repo = new HospitalRepository(store);
        repo.ForzarModoMemoriaParaPruebas();
        return (new HospitalServices(repo), store);
    }

    [Fact]
    public async Task RegistrarPaciente_ConCamaYaOcupada_LanzaExcepcionYNoRegistraAtencion()
    {
        // Arrange: la cama Id = 1 (URG-01) ya viene ocupada en los datos semilla
        var (services, store) = CrearSutEnMemoria();
        var totalAtencionesAntes = store.Atenciones.Count;

        var request = new RegistroPacienteRequest(
            Cedula: "V-99001122",
            Nombres: "Paciente Ficticio Uno",
            Sintomas: "Fiebre de prueba",
            Sexo: Sexo.Masculino,
            ConvenioId: 1,
            MedicoId: 1,
            CamaId: 1,
            RequiereLaboratorio: false,
            LaboratorioId: null,
            ExamenSolicitado: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HospitalValidationException>(
            () => services.RegistrarPacienteAsync(request));

        Assert.Contains("ocupada", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(totalAtencionesAntes, store.Atenciones.Count);
    }

    [Fact]
    public async Task DarAltaAtencion_RepetidaTrasReasignarCama_RechazaSegundaAltaYMantieneCamaOcupada()
    {
        // Arrange: la atención #2 ocupa inicialmente la cama Id = 1 (URG-01)
        var (services, store) = CrearSutEnMemoria();

        // 1. Primera alta legítima libera la cama 1
        await services.DarAltaAtencionAsync(atencionId: 2);
        Assert.False(store.Camas.Single(c => c.Id == 1).Ocupada);

        // 2. Nuevo paciente ficticio ingresa y ocupa legalmente esa misma cama 1
        var nuevaAtencionId = await services.RegistrarPacienteAsync(new RegistroPacienteRequest(
            Cedula: "V-88776655",
            Nombres: "Paciente Ficticio Reasignado",
            Sintomas: "Cefalea aguda de prueba",
            Sexo: Sexo.Femenino,
            ConvenioId: 2,
            MedicoId: 1,
            CamaId: 1,
            RequiereLaboratorio: false,
            LaboratorioId: null,
            ExamenSolicitado: null));

        Assert.True(nuevaAtencionId > 0);
        Assert.True(store.Camas.Single(c => c.Id == 1).Ocupada);

        // Act & Assert: intentar dar de alta nuevamente la atención #2 debe fallar y NO liberar la cama 1
        await Assert.ThrowsAsync<HospitalValidationException>(
            () => services.DarAltaAtencionAsync(atencionId: 2));

        Assert.True(store.Camas.Single(c => c.Id == 1).Ocupada);
    }

    [Theory]
    [InlineData("", "Nombre Ficticio", "Fiebre")]
    [InlineData("V-11223344", "", "Fiebre")]
    [InlineData("V-11223344", "Nombre Ficticio", "")]
    public async Task RegistrarPaciente_SinDatosObligatorios_LanzaHospitalValidationException(
        string cedula, string nombres, string sintomas)
    {
        var (services, _) = CrearSutEnMemoria();

        var request = new RegistroPacienteRequest(
            Cedula: cedula,
            Nombres: nombres,
            Sintomas: sintomas,
            Sexo: Sexo.Otro,
            ConvenioId: 1,
            MedicoId: 1,
            CamaId: null,
            RequiereLaboratorio: false,
            LaboratorioId: null,
            ExamenSolicitado: null);

        await Assert.ThrowsAsync<HospitalValidationException>(
            () => services.RegistrarPacienteAsync(request));
    }

    [Fact]
    public async Task FlujoSalidaYRetornoLaboratorio_ActualizaEstadoAtencionCorrectamente()
    {
        var (services, store) = CrearSutEnMemoria();

        var atencionId = await services.RegistrarPacienteAsync(new RegistroPacienteRequest(
            Cedula: "V-77665544",
            Nombres: "Paciente Ficticio Laboratorio",
            Sintomas: "Tos persistente y fiebre",
            Sexo: Sexo.Masculino,
            ConvenioId: 1,
            MedicoId: 2,
            CamaId: 2,
            RequiereLaboratorio: true,
            LaboratorioId: 1,
            ExamenSolicitado: "Hemograma completo",
            PrioridadLaboratorio: PrioridadLaboratorio.Urgente));

        var atencion = store.Atenciones.Single(a => a.Id == atencionId);
        Assert.Equal(EstadoAtencion.EnLaboratorio, atencion.Estado);

        var salida = store.SalidasLaboratorio.Single(s => s.AtencionId == atencionId);
        await services.CompletarSalidaLaboratorioAsync(salida.Id, "Parámetros normales");

        Assert.Equal(EstadoSalidaLaboratorio.ResultadoRecibido, salida.Estado);
        Assert.Equal(EstadoAtencion.EnAtencion, atencion.Estado);
    }

    [Fact]
    public async Task CambiarGuardiaMedico_ActualizaIndicadorMedicosDelDia()
    {
        var (services, _) = CrearSutEnMemoria();
        var kpiInicial = await services.ObtenerDashboardAsync();

        await services.CambiarGuardiaMedicoAsync(medicoId: 1, enGuardiaHoy: false);
        var kpiActualizado = await services.ObtenerDashboardAsync();

        Assert.Equal(kpiInicial.MedicosDelDia - 1, kpiActualizado.MedicosDelDia);
    }

    [Fact]
    public void SchemaSql_ContieneVerificacionRowCountYBloqueoForUpdate()
    {
        var schemaPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database", "schema.sql"));
        var sql = File.ReadAllText(schemaPath);

        Assert.Contains("IF ROW_COUNT() <> 1 THEN", sql);
        Assert.Contains("FOR UPDATE;", sql);
        Assert.Contains("SIGNAL SQLSTATE '45000'", sql);
    }
}
