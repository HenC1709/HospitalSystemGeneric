using HospitalSystem.Application.Services;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Repositories;

Console.WriteLine("=== HospitalSystem CLI - Verificador Rápido de MySQL (C# 14) ===");

var connectionString = Environment.GetEnvironmentVariable("HOSPITAL_MYSQL_CS")
    ?? "Server=localhost;Port=3306;Database=hospital_system_db;Uid=root;Pwd=;";

var store = new HospitalDataStore(connectionString);
var repo = new HospitalRepository(store);
var service = new HospitalServices(repo);

var conexion = await service.ProbarConexionMySqlAsync();
Console.WriteLine($"Estado MySQL : {conexion.Mensaje}");

var kpis = await service.ObtenerDashboardAsync();
Console.WriteLine($"Origen Datos : {kpis.OrigenDatos}");
Console.WriteLine($"Camas Libres : {kpis.CamasDisponibles} / {kpis.CamasTotales}");
Console.WriteLine($"Total Salas  : {kpis.CantidadSalas}");
Console.WriteLine($"Médicos Día  : {kpis.MedicosDelDia}");
Console.WriteLine($"Salidas Lab  : {kpis.SalidasLaboratorioActivas}");
