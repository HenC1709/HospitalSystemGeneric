using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Interfaces;
using HospitalSystem.Core.Exceptions;
using HospitalSystem.Infrastructure.Persistence;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Repositories;

/// <summary>
/// Núcleo del repositorio: administra conectividad MySQL, verificación de esquema
/// y restablecimiento controlado de la base de datos de demostración.
/// </summary>
public sealed partial class HospitalRepository(HospitalDataStore dataStore) : IHospitalRepository
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

    /// <summary>
    /// Permite forzar modo memoria en pruebas unitarias deterministas sin depender de red local.
    /// </summary>
    public void ForzarModoMemoriaParaPruebas()
    {
        ModoMySqlActivo = false;
        _verificacionInicialRealizada = true;
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
                    $"Servidor MySQL activo ({conn.ServerVersion}), pero faltan las tablas. Usa 'Restablecer base de datos de demostración'.");
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
            throw new DatabaseConnectionException($"Error al restablecer MySQL de demostración: {ex.Message}", ex);
        }
    }
}
