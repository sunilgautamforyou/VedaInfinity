using MySqlConnector;
using VedaInfinity.Web.Models;

namespace VedaInfinity.Web.Data;

public class UserSearchResult
{
    public IReadOnlyList<LoginUser> Items { get; init; } = Array.Empty<LoginUser>();
    public int TotalCount { get; init; }
}

public interface IUserRepository
{
    Task<UserSearchResult> SearchAsync(string? userName, string? emailId, string? displayName, string? mobileNo, int page, int pageSize);
    Task<LoginUser?> GetByIdAsync(int userId);
    Task<IReadOnlyList<RoleMaster>> GetRolesAsync();
    Task<int> CreateAsync(LoginUser user, string passwordHash, int? createdBy);
    Task UpdateAsync(LoginUser user, string? passwordHash, int? modifiedBy);
    Task SetActiveAsync(int userId, bool isActive, int? modifiedBy);
    Task<bool> UserNameExistsAsync(string userName, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string emailId, int? excludeUserId = null);
    Task<bool> MobileExistsAsync(string mobileNo, int? excludeUserId = null);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<UserSearchResult> SearchAsync(string? userName, string? emailId, string? displayName, string? mobileNo, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "1=1" };
        if (!string.IsNullOrWhiteSpace(userName)) where.Add("u.UserName LIKE @UserName");
        if (!string.IsNullOrWhiteSpace(emailId)) where.Add("u.EmailId LIKE @EmailId");
        if (!string.IsNullOrWhiteSpace(displayName)) where.Add("u.DisplayName LIKE @DisplayName");
        if (!string.IsNullOrWhiteSpace(mobileNo)) where.Add("u.MobileNo LIKE @MobileNo");
        var whereSql = string.Join(" AND ", where);

        var countSql = $@"
SELECT COUNT(1)
FROM LoginMaster u
INNER JOIN RoleMaster r ON r.RoleId = u.RoleId
WHERE {whereSql};";

        var dataSql = $@"
SELECT u.UserId, u.UserName, u.EmailId, u.PasswordHash, u.RoleId,
       r.RoleCode, r.RoleName, u.DisplayName, u.MobileNo,
       u.ExpiryDate, u.IsActive, u.LastLoginDate
FROM LoginMaster u
INNER JOIN RoleMaster r ON r.RoleId = u.RoleId
WHERE {whereSql}
ORDER BY u.UserId
LIMIT @PageSize OFFSET @Offset;";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        int total;
        await using (var countCmd = new MySqlCommand(countSql, conn))
        {
            BindSearch(countCmd, userName, emailId, displayName, mobileNo);
            total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        var list = new List<LoginUser>();
        await using (var dataCmd = new MySqlCommand(dataSql, conn))
        {
            BindSearch(dataCmd, userName, emailId, displayName, mobileNo);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);
            dataCmd.Parameters.AddWithValue("@Offset", offset);
            await using var reader = await dataCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));
        }

        return new UserSearchResult { Items = list, TotalCount = total };
    }

    private static void BindSearch(MySqlCommand cmd, string? userName, string? emailId, string? displayName, string? mobileNo)
    {
        if (!string.IsNullOrWhiteSpace(userName))
            cmd.Parameters.AddWithValue("@UserName", "%" + userName.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(emailId))
            cmd.Parameters.AddWithValue("@EmailId", "%" + emailId.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(displayName))
            cmd.Parameters.AddWithValue("@DisplayName", "%" + displayName.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(mobileNo))
            cmd.Parameters.AddWithValue("@MobileNo", "%" + mobileNo.Trim() + "%");
    }

    public async Task<LoginUser?> GetByIdAsync(int userId)
    {
        const string sql = @"
SELECT u.UserId, u.UserName, u.EmailId, u.PasswordHash, u.RoleId,
       r.RoleCode, r.RoleName, u.DisplayName, u.MobileNo,
       u.ExpiryDate, u.IsActive, u.LastLoginDate
FROM LoginMaster u
INNER JOIN RoleMaster r ON r.RoleId = u.RoleId
WHERE u.UserId = @UserId
LIMIT 1;";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return Map(reader);
    }

    public async Task<IReadOnlyList<RoleMaster>> GetRolesAsync()
    {
        const string sql = @"
SELECT RoleId, RoleCode, RoleName, Description, IsActive
FROM RoleMaster
WHERE IsActive = 1
ORDER BY RoleId;";

        var list = new List<RoleMaster>();
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new RoleMaster
            {
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleCode = reader.GetString(reader.GetOrdinal("RoleCode")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }
        return list;
    }

    public async Task<int> CreateAsync(LoginUser user, string passwordHash, int? createdBy)
    {
        const string sql = @"
INSERT INTO LoginMaster
(UserName, EmailId, PasswordHash, RoleId, DisplayName, MobileNo, ExpiryDate, IsActive, CreatedBy, CreatedDate)
VALUES
(@UserName, @EmailId, @PasswordHash, @RoleId, @DisplayName, @MobileNo, @ExpiryDate, @IsActive, @CreatedBy, NOW());
SELECT LAST_INSERT_ID();";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        BindUser(cmd, user);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmd.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(LoginUser user, string? passwordHash, int? modifiedBy)
    {
        // Username is intentionally not updated in edit mode
        var sql = @"
UPDATE LoginMaster SET
  EmailId = @EmailId,
  RoleId = @RoleId,
  DisplayName = @DisplayName,
  MobileNo = @MobileNo,
  ExpiryDate = @ExpiryDate,
  IsActive = @IsActive,
  ModifiedBy = @ModifiedBy,
  ModifiedDate = NOW()";
        if (!string.IsNullOrEmpty(passwordHash))
            sql += ", PasswordHash = @PasswordHash";
        sql += " WHERE UserId = @UserId;";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", user.UserId);
        cmd.Parameters.AddWithValue("@EmailId", user.EmailId.Trim());
        cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)user.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MobileNo", (object?)user.MobileNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiryDate", (object?)user.ExpiryDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@ModifiedBy", (object?)modifiedBy ?? DBNull.Value);
        if (!string.IsNullOrEmpty(passwordHash))
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActiveAsync(int userId, bool isActive, int? modifiedBy)
    {
        const string sql = @"
UPDATE LoginMaster
SET IsActive = @IsActive, ModifiedBy = @ModifiedBy, ModifiedDate = NOW()
WHERE UserId = @UserId;";

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
        cmd.Parameters.AddWithValue("@ModifiedBy", (object?)modifiedBy ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UserNameExistsAsync(string userName, int? excludeUserId = null)
    {
        var sql = "SELECT COUNT(1) FROM LoginMaster WHERE UserName = @UserName";
        if (excludeUserId.HasValue) sql += " AND UserId <> @UserId";
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserName", userName.Trim());
        if (excludeUserId.HasValue) cmd.Parameters.AddWithValue("@UserId", excludeUserId.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<bool> EmailExistsAsync(string emailId, int? excludeUserId = null)
    {
        var sql = "SELECT COUNT(1) FROM LoginMaster WHERE EmailId = @EmailId";
        if (excludeUserId.HasValue) sql += " AND UserId <> @UserId";
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@EmailId", emailId.Trim());
        if (excludeUserId.HasValue) cmd.Parameters.AddWithValue("@UserId", excludeUserId.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<bool> MobileExistsAsync(string mobileNo, int? excludeUserId = null)
    {
        var sql = "SELECT COUNT(1) FROM LoginMaster WHERE MobileNo = @MobileNo";
        if (excludeUserId.HasValue) sql += " AND UserId <> @UserId";
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MobileNo", mobileNo.Trim());
        if (excludeUserId.HasValue) cmd.Parameters.AddWithValue("@UserId", excludeUserId.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    private static void BindUser(MySqlCommand cmd, LoginUser user)
    {
        cmd.Parameters.AddWithValue("@UserName", user.UserName.Trim());
        cmd.Parameters.AddWithValue("@EmailId", user.EmailId.Trim());
        cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)user.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MobileNo", (object?)user.MobileNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiryDate", (object?)user.ExpiryDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
    }

    private static LoginUser Map(MySqlDataReader reader)
    {
        return new LoginUser
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            EmailId = reader.GetString(reader.GetOrdinal("EmailId")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("PasswordHash")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleCode = reader.GetString(reader.GetOrdinal("RoleCode")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName"))
                ? null
                : reader.GetString(reader.GetOrdinal("DisplayName")),
            MobileNo = reader.IsDBNull(reader.GetOrdinal("MobileNo"))
                ? null
                : reader.GetString(reader.GetOrdinal("MobileNo")),
            ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("LastLoginDate"))
        };
    }
}
