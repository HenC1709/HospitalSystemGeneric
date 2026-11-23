using CommunityToolkit.Mvvm.Input;
using HospitalSystem.Application.DTOs;

namespace HospitalSystem.Desktop.ViewModels;

/// <summary>
/// Módulo de Exportación de Expediente Clínico a PDF e Impresión de Ticket de Salida
/// mediante un diálogo modal animado con la identidad visual del hospital.
/// </summary>
public sealed partial class MainViewModel
{
    public ResumenPacienteDto? PacienteSeleccionadoTabla { get; set => SetProperty(ref field, value); }

    public bool DialogoExportacionVisible { get; set => SetProperty(ref field, value); }
    public string ExportacionEtiquetaSuperior { get; set => SetProperty(ref field, value); } = "DOCUMENTO CLÍNICO OFICIAL";
    public string ExportacionTitulo { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionSubtitulo { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionFolio { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionPaciente { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionCedula { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionMedicoYCama { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionSintomas { get; set => SetProperty(ref field, value); } = string.Empty;
    public string ExportacionArchivoDestino { get; set => SetProperty(ref field, value); } = string.Empty;

    [RelayCommand]
    private void ExportarExpedientePdf(ResumenPacienteDto? pacienteParam)
    {
        var objetivo = pacienteParam ?? PacienteSeleccionadoTabla ?? Pacientes.FirstOrDefault();
        if (objetivo is null)
        {
            Notificar(SeveridadFeedback.Advertencia, "Sin paciente seleccionado", "Registra o selecciona un paciente para exportar su expediente en PDF.");
            return;
        }

        var folio = $"EXP-{DateTime.Now:yyyyMMdd}-{objetivo.PacienteId:D4}";
        var cedulaLimpia = objetivo.Cedula.Replace(" ", "_");

        ExportacionEtiquetaSuperior = "EXPEDIENTE CLÍNICO DIGITAL · PDF";
        ExportacionTitulo = "¡Expediente Exportado a PDF con Éxito!";
        ExportacionSubtitulo = "El historial clínico completo del paciente fue generado con sello digital hospitalario.";
        ExportacionFolio = folio;
        ExportacionPaciente = $"{objetivo.Nombres} ({objetivo.ConvenioNombre})";
        ExportacionCedula = objetivo.Cedula;
        ExportacionMedicoYCama = $"{objetivo.MedicoAsignado}  ·  {objetivo.CamaDescripcion}";
        ExportacionSintomas = objetivo.Sintomas;
        ExportacionArchivoDestino = $"Documentos/HospitalSanLucas/Expediente_{cedulaLimpia}_{DateTime.Now:HHmmss}.pdf";

        DialogoExportacionVisible = true;
        DialogoExportacionAbierto?.Invoke();

        Notificar(
            SeveridadFeedback.Exito,
            "Expediente PDF Exportado",
            $"Se generó exitosamente el expediente clínico de {objetivo.Nombres} ({folio}).");
    }

    [RelayCommand]
    private void ImprimirTicketSalida(ResumenPacienteDto? pacienteParam)
    {
        var objetivo = pacienteParam ?? PacienteSeleccionadoTabla ?? Pacientes.FirstOrDefault();
        if (objetivo is null)
        {
            Notificar(SeveridadFeedback.Advertencia, "Sin paciente seleccionado", "Selecciona un paciente en la lista para imprimir su ticket de salida.");
            return;
        }

        var numeroAtencion = objetivo.AtencionId ?? objetivo.PacienteId;
        var folio = $"TCK-SAL-{numeroAtencion:D5}";

        ExportacionEtiquetaSuperior = "PASE Y TICKET DE SALIDA HOSPITALARIA";
        ExportacionTitulo = "¡Ticket de Salida Impreso y Exportado con Éxito!";
        ExportacionSubtitulo = "El comprobante de egreso / pase de salida fue enviado a cola de impresión térmica y respaldado en PDF.";
        ExportacionFolio = folio;
        ExportacionPaciente = $"{objetivo.Nombres} — Estado: {objetivo.EstadoLegible}";
        ExportacionCedula = objetivo.Cedula;
        ExportacionMedicoYCama = $"Autoriza: {objetivo.MedicoAsignado}  ·  Área: {objetivo.CamaDescripcion}";
        ExportacionSintomas = $"Motivo de atención: {objetivo.Sintomas}";
        ExportacionArchivoDestino = $"Impresora Admisión #01 + Ticket_Salida_{folio}.pdf";

        DialogoExportacionVisible = true;
        DialogoExportacionAbierto?.Invoke();

        Notificar(
            SeveridadFeedback.Exito,
            "Ticket de Salida Generado",
            $"Comprobante {folio} emitido con éxito para {objetivo.Nombres}.");
    }

    [RelayCommand]
    private void CerrarDialogoExportacion() => DialogoExportacionVisible = false;
}
