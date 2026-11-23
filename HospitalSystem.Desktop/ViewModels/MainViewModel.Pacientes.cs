using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;

namespace HospitalSystem.Desktop.ViewModels;

/// <summary>
/// Módulo Menú Pacientes de MainViewModel.
/// </summary>
public sealed partial class MainViewModel
{
    public ObservableCollection<ResumenPacienteDto> Pacientes { get; } = [];
    public ObservableCollection<ResumenPacienteDto> PacientesActivosSelector { get; } = [];

    public string PacienteCedula { get; set => SetProperty(ref field, value); } = string.Empty;
    public string PacienteNombres { get; set => SetProperty(ref field, value); } = string.Empty;
    public string PacienteSintomas { get; set => SetProperty(ref field, value); } = string.Empty;
    public Sexo PacienteSexo { get; set => SetProperty(ref field, value); } = Sexo.Masculino;
    public Convenio? ConvenioSeleccionado { get; set => SetProperty(ref field, value); }
    public Medico? MedicoAsignadoSeleccionado { get; set => SetProperty(ref field, value); }
    public bool AsignarCamaHospitalaria { get; set => SetProperty(ref field, value); } = true;
    public Cama? CamaSeleccionada { get; set => SetProperty(ref field, value); }
    public bool PacienteRequiereLaboratorio { get; set => SetProperty(ref field, value); }
    public Laboratorio? LaboratorioIngresoSeleccionado { get; set => SetProperty(ref field, value); }
    public string ExamenIngreso { get; set => SetProperty(ref field, value); } = "Hemograma completo y bioquímica";
    public PrioridadLaboratorio PrioridadIngreso { get; set => SetProperty(ref field, value); } = PrioridadLaboratorio.Urgente;

    public bool SoloPacientesActivos
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = RecargarTodoAsync();
        }
    }

    [RelayCommand]
    private async Task RegistrarPacienteAsync()
    {
        try
        {
            var camaId = AsignarCamaHospitalaria ? CamaSeleccionada?.Id : null;
            var labId = PacienteRequiereLaboratorio ? LaboratorioIngresoSeleccionado?.Id : null;
            var nombreRegistrado = PacienteNombres.Trim();

            var request = new RegistroPacienteRequest(
                Cedula: PacienteCedula,
                Nombres: PacienteNombres,
                Sintomas: PacienteSintomas,
                Sexo: PacienteSexo,
                ConvenioId: ConvenioSeleccionado?.Id,
                MedicoId: MedicoAsignadoSeleccionado?.Id ?? 0,
                CamaId: camaId,
                RequiereLaboratorio: PacienteRequiereLaboratorio,
                LaboratorioId: labId,
                ExamenSolicitado: ExamenIngreso,
                PrioridadLaboratorio: PrioridadIngreso
            );

            var atencionId = await _services.RegistrarPacienteAsync(request);

            var derivadoLab = PacienteRequiereLaboratorio;
            PacienteCedula = string.Empty;
            PacienteNombres = string.Empty;
            PacienteSintomas = string.Empty;
            PacienteRequiereLaboratorio = false;

            Notificar(
                SeveridadFeedback.Exito,
                "Ingreso de Paciente Registrado",
                derivadoLab
                    ? $"{nombreRegistrado} ingresó en Atención #{atencionId} con salida inmediata a laboratorio."
                    : $"{nombreRegistrado} ingresó correctamente en Atención #{atencionId}.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Revisa los datos del paciente", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DarAltaPacienteAsync(ResumenPacienteDto? paciente)
    {
        if (paciente?.AtencionId is null)
            return;

        try
        {
            await _services.DarAltaAtencionAsync(paciente.AtencionId.Value);
            Notificar(
                SeveridadFeedback.Exito,
                "Alta Médica Otorgada",
                $"Se finalizó la atención de {paciente.Nombres} y se verificó la liberación segura de su cama.");
            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Alta médica no aplicada", ex.Message);
        }
    }

    [RelayCommand]
    private void PrepararSalidaLaboratorioDesdePaciente(ResumenPacienteDto? paciente)
    {
        if (paciente is null)
            return;

        PacienteParaLabSeleccionado = PacientesActivosSelector.FirstOrDefault(p => p.PacienteId == paciente.PacienteId) ?? paciente;
        MotivoSalidaLab = paciente.Sintomas;
        ExamenSalidaLab = "Perfil diagnóstico según sintomatología";
        SeccionActual = SeccionMenu.Laboratorios;

        Notificar(
            SeveridadFeedback.Informacion,
            "Derivación a Laboratorio Lista",
            $"Confirma el laboratorio y examen solicitado para {paciente.Nombres}.");
    }
}
