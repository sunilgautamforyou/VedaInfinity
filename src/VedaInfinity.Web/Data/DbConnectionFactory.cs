using MySqlConnector;

namespace VedaInfinity.Web.Data;

public interface IDbConnectionFactory
{
    MySqlConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    public MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);
}
