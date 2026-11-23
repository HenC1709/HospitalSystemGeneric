using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Core.Entities;
using HospitalSystem.Core.Enums;

namespace HospitalSystem.Desktop.ViewModels;

/// <summary>
/// Módulo Salidas a Laboratorios de MainViewModel.
/// </summary>
public sealed partial class MainViewModel
{
    public ObservableCollection<Laboratorio> Laboratorios { get; } = [];
    public ObservableCollection<SalidaLaboratorioDto> SalidasLaboratorio { get; } = [];

    public ResumenPacienteDto? PacienteParaLabSeleccionado
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
                MotivoSalidaLab = value.Sintomas;
        }
    }

    public Laboratorio? LaboratorioSalidaSeleccionado { get; set => SetProperty(ref field, value); }
    public string ExamenSalidaLab { get; set => SetProperty(ref field, value); } = string.Empty;
    public string MotivoSalidaLab { get; set => SetProperty(ref field, value); } = string.Empty;
    public PrioridadLaboratorio PrioridadSalidaLab { get; set => SetProperty(ref field, value); } = PrioridadLaboratorio.Urgente;
    public SalidaLaboratorioDto? SalidaSeleccionada { get; set => SetProperty(ref field, value); }
    public string ResultadoRecibidoTexto { get; set => SetProperty(ref field, value); } = "Valores dentro de parámetros clínicos esperados.";

    [RelayCommand]
    private async Task RegistrarSalidaLaboratorioAsync()
    {
        if (PacienteParaLabSeleccionado is null)
        {
            Notificar(SeveridadFeedback.Advertencia, "Paciente requerido", "Selecciona un paciente activo para autorizar su salida a laboratorio.");
            return;
        }

        try
        {
            var medicoId = PacienteParaLabSeleccionado.MedicoId > 0
                ? PacienteParaLabSeleccionado.MedicoId
                : (MedicosDelDia.FirstOrDefault()?.Id ?? 1);

            var request = new NuevaSalidaLaboratorioRequest(
                PacienteId: PacienteParaLabSeleccionado.PacienteId,
                AtencionId: PacienteParaLabSeleccionado.AtencionId,
                MedicoId: medicoId,
                LaboratorioId: LaboratorioSalidaSeleccionado?.Id ?? 0,
                ExamenSolicitado: ExamenSalidaLab,
                MotivoClinico: MotivoSalidaLab,
                Prioridad: PrioridadSalidaLab
            );

            await _services.RegistrarSalidaLaboratorioAsync(request);
            ExamenSalidaLab = string.Empty;

            Notificar(
                SeveridadFeedback.Exito,
                "Salida a Laboratorio Autorizada",
                $"{PacienteParaLabSeleccionado.Nombres} fue derivado a {LaboratorioSalidaSeleccionado?.Nombre}.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Advertencia, "Revisa la orden de laboratorio", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CompletarSalidaLaboratorioAsync(SalidaLaboratorioDto? salida)
    {
        var objetivo = salida ?? SalidaSeleccionada;
        if (objetivo is null || !objetivo.PuedeRecibirResultado)
            return;

        try
        {
            var resumen = string.IsNullOrWhiteSpace(ResultadoRecibidoTexto)
                ? "Estudio completado sin hallazgos críticos adicionales."
                : ResultadoRecibidoTexto;

            await _services.CompletarSalidaLaboratorioAsync(objetivo.SalidaId, resumen);

            Notificar(
                SeveridadFeedback.Exito,
                "Resultado Recibido de Laboratorio",
                $"Se registró el retorno clínico de {objetivo.PacienteNombres} a sala.");

            await RecargarTodoAsync();
        }
        catch (Exception ex)
        {
            Notificar(SeveridadFeedback.Error, "Error al registrar retorno", ex.Message);
        }
    }
}
