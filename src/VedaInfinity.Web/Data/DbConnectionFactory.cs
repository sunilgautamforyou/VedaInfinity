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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        var password = configuration["ConnectionStrings:DefaultConnectionPassword"];
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database password is not configured. Set 'ConnectionStrings:DefaultConnectionPassword' in .NET user-secrets or environment variables.");
        }

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Password = password
        };
        _connectionString = builder.ConnectionString;
    }

    public MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);
}
