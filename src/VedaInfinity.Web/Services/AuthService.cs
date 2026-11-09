using VedaInfinity.Web.Data;
using VedaInfinity.Web.Models;

namespace VedaInfinity.Web.Services;

public interface IAuthService
{
    Task<(bool Success, LoginUser? User, string? Error)> ValidateLoginAsync(string userName, string password);
}

public class AuthService : IAuthService
{
    private readonly ILoginRepository _loginRepository;

    public AuthService(ILoginRepository loginRepository)
    {
        _loginRepository = loginRepository;
    }

    public async Task<(bool Success, LoginUser? User, string? Error)> ValidateLoginAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return (false, null, "Username and password are required.");

        var user = await _loginRepository.GetByUserNameAsync(userName.Trim());
        if (user is null)
            return (false, null, "Invalid username or password.");

        if (!user.IsActive)
            return (false, null, "This account is inactive. Contact the school administrator.");

        if (user.ExpiryDate.HasValue && user.ExpiryDate.Value.Date < DateTime.Today)
            return (false, null, "This account has expired. Contact the school administrator.");

        if (!PasswordHasher.VerifySha1(password, user.PasswordHash))
            return (false, null, "Invalid username or password.");

        await _loginRepository.UpdateLastLoginAsync(user.UserId);
        return (true, user, null);
    }
}
