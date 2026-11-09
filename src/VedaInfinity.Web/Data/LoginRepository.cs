using MySqlConnector;
using VedaInfinity.Web.Models;

namespace VedaInfinity.Web.Data;

public interface ILoginRepository
{
    Task<LoginUser?> GetByLoginIdentifierAsync(string identifier);
    Task UpdateLastLoginAsync(int userId);
}

public class LoginRepository : ILoginRepository
{
    private readonly IDbConnectionFactory _db;

    public LoginRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<LoginUser?> GetByLoginIdentifierAsync(string identifier)
    {
        const string sql = @"
SELECT
    u.UserId,
    u.UserName,
    u.EmailId,
    u.PasswordHash,
    u.RoleId,
    r.RoleCode,
    r.RoleName,
    u.DisplayName,
    u.MobileNo,
    u.ExpiryDate,
    u.IsActive,
    u.LastLoginDate
FROM LoginMaster u
INNER JOIN RoleMaster r ON r.RoleId = u.RoleId
WHERE u.UserName = @Id
   OR u.EmailId = @Id
   OR u.MobileNo = @Id
LIMIT 1;";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", identifier.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new LoginUser
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            EmailId = reader.GetString(reader.GetOrdinal("EmailId")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleCode = reader.GetString(reader.GetOrdinal("RoleCode")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
            MobileNo = reader.IsDBNull(reader.GetOrdinal("MobileNo")) ? null : reader.GetString(reader.GetOrdinal("MobileNo")),
            ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginDate"))
        };
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        const string sql = "UPDATE LoginMaster SET LastLoginDate = NOW() WHERE UserId = @UserId;";
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }
}
