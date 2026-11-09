using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using VedaInfinity.Web.Data;
using VedaInfinity.Web.Models;
using VedaInfinity.Web.Services;

namespace VedaInfinity.Web.Pages;

public class UserMasterModel : PageModel
{
    private readonly IUserRepository _users;
    public const int DefaultPageSize = 10;

    public UserMasterModel(IUserRepository users)
    {
        _users = users;
    }

    public IReadOnlyList<LoginUser> Users { get; private set; } = Array.Empty<LoginUser>();
    public SelectList RoleOptions { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public IReadOnlyList<RoleMaster> Roles { get; private set; } = Array.Empty<RoleMaster>();

    public int PageNumber { get; private set; } = 1;
    public int PageSize { get; private set; } = DefaultPageSize;
    public int TotalCount { get; private set; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? FilterUserName { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? FilterEmail { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? FilterDisplayName { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? FilterMobile { get; set; }

    [BindProperty]
    public UserFormInput Input { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public bool KeepModalOpen { get; set; }
    public bool SaveSucceeded { get; set; }
    public string? SuccessPopupMessage { get; set; }

    public class UserFormInput
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int RoleId { get; set; }
        public string? DisplayName { get; set; }
        public string? MobileNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync(int page = 1)
    {
        PageNumber = page < 1 ? 1 : page;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync(int page = 1)
    {
        PageNumber = page < 1 ? 1 : page;
        await LoadAsync();

        if (string.IsNullOrWhiteSpace(Input.EmailId)
            || Input.RoleId <= 0
            || string.IsNullOrWhiteSpace(Input.DisplayName)
            || string.IsNullOrWhiteSpace(Input.MobileNo))
        {
            ErrorMessage = "Role, Display Name, Mobile No and Email are required.";
            KeepModalOpen = true;
            return Page();
        }

        var isNew = Input.UserId <= 0;

        if (isNew && string.IsNullOrWhiteSpace(Input.UserName))
        {
            ErrorMessage = "User Name is required.";
            KeepModalOpen = true;
            return Page();
        }

        if (isNew && string.IsNullOrWhiteSpace(Input.Password))
        {
            ErrorMessage = "Password is required for new users.";
            KeepModalOpen = true;
            return Page();
        }

        if (!Regex.IsMatch(Input.MobileNo.Trim(), @"^[0-9]{10}$"))
        {
            ErrorMessage = "Mobile No must be a 10-digit number.";
            KeepModalOpen = true;
            return Page();
        }

        if (isNew && await _users.UserNameExistsAsync(Input.UserName))
        {
            ErrorMessage = "Username already exists. Please change the generated name slightly.";
            KeepModalOpen = true;
            return Page();
        }

        if (await _users.EmailExistsAsync(Input.EmailId, isNew ? null : Input.UserId))
        {
            ErrorMessage = "Email already exists.";
            KeepModalOpen = true;
            return Page();
        }

        if (await _users.MobileExistsAsync(Input.MobileNo, isNew ? null : Input.UserId))
        {
            ErrorMessage = "Mobile No already exists.";
            KeepModalOpen = true;
            return Page();
        }

        var currentUserId = GetCurrentUserId();

        if (isNew)
        {
            var entity = new LoginUser
            {
                UserName = Input.UserName.Trim(),
                EmailId = Input.EmailId.Trim(),
                RoleId = Input.RoleId,
                DisplayName = Input.DisplayName.Trim(),
                MobileNo = Input.MobileNo.Trim(),
                ExpiryDate = Input.ExpiryDate,
                IsActive = Input.IsActive
            };
            var hash = PasswordHasher.HashSha1(Input.Password!);
            await _users.CreateAsync(entity, hash, currentUserId);
            SuccessPopupMessage = "User created successfully.";
        }
        else
        {
            // Username cannot be changed in edit mode
            var entity = new LoginUser
            {
                UserId = Input.UserId,
                EmailId = Input.EmailId.Trim(),
                RoleId = Input.RoleId,
                DisplayName = Input.DisplayName.Trim(),
                MobileNo = Input.MobileNo.Trim(),
                ExpiryDate = Input.ExpiryDate,
                IsActive = Input.IsActive
            };
            string? hash = string.IsNullOrWhiteSpace(Input.Password)
                ? null
                : PasswordHasher.HashSha1(Input.Password);
            await _users.UpdateAsync(entity, hash, currentUserId);
            SuccessPopupMessage = "User updated successfully.";
        }

        // Stay on page, show success inside entry modal, then close it via JS (no banner on list page)
        KeepModalOpen = true;
        SaveSucceeded = true;
        Input = new UserFormInput(); // clear form after successful save
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int userId, bool isActive, int page = 1)
    {
        await _users.SetActiveAsync(userId, isActive, GetCurrentUserId());
        return RedirectToPage(new { page, FilterUserName, FilterEmail, FilterDisplayName, FilterMobile });
    }

    private async Task LoadAsync()
    {
        var result = await _users.SearchAsync(FilterUserName, FilterEmail, FilterDisplayName, FilterMobile, PageNumber, PageSize);
        Users = result.Items;
        TotalCount = result.TotalCount;
        if (PageNumber > TotalPages) PageNumber = TotalPages;

        Roles = await _users.GetRolesAsync();
        RoleOptions = new SelectList(Roles, nameof(RoleMaster.RoleId), nameof(RoleMaster.RoleName));
    }

    private int? GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : null;
    }
}
