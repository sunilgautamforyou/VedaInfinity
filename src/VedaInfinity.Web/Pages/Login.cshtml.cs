using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VedaInfinity.Web.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty] public string Username { get; set; } = "admin";
    [BindProperty] public string Password { get; set; } = "admin";
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // TEMPORARY demo login only. Replace with real user store later.
        if (Username == "admin" && Password == "admin")
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "Administrator") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToPage("/Dashboard");
        }

        ErrorMessage = "Invalid credentials. Use admin / admin until the user module is built.";
        return Page();
    }
}
