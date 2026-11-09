namespace VedaInfinity.Web.Models;

public class LoginUser
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmailId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? MobileNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
