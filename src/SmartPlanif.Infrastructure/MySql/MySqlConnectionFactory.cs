using System.Data.Common;
using MySqlConnector;

namespace SmartPlanif.Infrastructure.MySql;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken ct);
}

public sealed class MySqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
