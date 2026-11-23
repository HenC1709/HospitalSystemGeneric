using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;

namespace HospitalSystem.Desktop.ViewModels;

/// <summary>
/// Módulo Menú Médicos y Guardia del Día de MainViewModel.
/// </summary>
public sealed partial class MainViewModel
{
    public ObservableCollection<Medico> MedicosDelDia { get; } = [];
    public ObservableCollection<Medico> MedicosListado { get; } = [];

    public bool SoloMedicosDelDiaFiltro
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = RecargarMedicosAsync();
        }
    } = true;

    public string NuevoMedicoCedula { get; set => SetProperty(ref field, value); } = string.Empty;
    public string NuevoMedicoNombres { get; set => SetProperty(ref field, value); } = string.Empty;
    public string NuevoMedicoEspecialidad { get; set => SetProperty(ref field, value); } = string.Empty;
    public TurnoMedico NuevoMedicoTurno { get; set => SetProperty(ref field, value); } = TurnoMedico.Manana;
    public string NuevoMedicoConsultorio { get; set => SetProperty(ref field, value); } = string.Empty;
    public bool NuevoMedicoEnGuardia { get; set => SetProperty(ref field, value); } = true;

    private async Task RecargarMedicosAsync()
    {
        RecargarColeccion(MedicosListado, await _services.ObtenerMedicosAsync(soloDelDia: SoloMedicosDelDiaFiltro));
    }

    [RelayCommand]
    private async Task AlternarGuardiaMedicoAsync(Medico? medico)
    {
        if (medico is null)
            return;

        try
        {
            var nuevoEstado = !medico.EnGuardiaHoy;
            await _services.CambiarGuardiaMedicoAsync(medico.Id, nuevoEstado);

            Notificar(
                SeveridadFeedback.Exito,
                nuevoEstado ? "Incorporado a Médicos del Día" : "Guardia Actualizada",
                nuevoEstado
                    ? $"{medico.Nombres} ahora figura activo en la guardia del día."
                    : $"{medico.Nombres} pasó a estado fuera de turno por hoy.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al cambiar guardia", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RegistrarMedicoAsync()
    {
        try
        {
            var nombre = NuevoMedicoNombres.Trim();
            var request = new NuevoMedicoRequest(
                CedulaProfesional: NuevoMedicoCedula,
                Nombres: NuevoMedicoNombres,
                Especialidad: NuevoMedicoEspecialidad,
                Turno: NuevoMedicoTurno,
                Consultorio: NuevoMedicoConsultorio,
                EnGuardiaHoy: NuevoMedicoEnGuardia
            );

            await _services.RegistrarMedicoAsync(request);

            NuevoMedicoCedula = string.Empty;
            NuevoMedicoNombres = string.Empty;
            NuevoMedicoEspecialidad = string.Empty;
            NuevoMedicoConsultorio = string.Empty;

            Notificar(SeveridadFeedback.Exito, "Especialista Incorporado", $"{nombre} se agregó correctamente a la plantilla médica.");
            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Datos incompletos del médico", ex.Message);
        }
    }
}
