using Microsoft.EntityFrameworkCore;
using Npgsql;
using Omnia.Api.Data;

namespace Omnia.Api.Tests;

internal static class TestDatabase
{
    public static OmniaDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OmniaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OmniaDbContext(options);
    }

    public static async Task<List<string>> QueryTablesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public static async Task<Dictionary<(string Table, string Column), string>> QueryColumnTypesAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT table_name, column_name, data_type FROM information_schema.columns " +
            "WHERE table_schema = 'public' AND table_name IN ('Users', 'Clips')";

        var columns = new Dictionary<(string, string), string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns[(reader.GetString(0), reader.GetString(1))] = reader.GetString(2);
        }

        return columns;
    }
}
