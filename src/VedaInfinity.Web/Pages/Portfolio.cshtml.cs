using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VedaInfinity.Web.Pages;

public class PortfolioModel : PageModel
{
    public string Mode { get; private set; } = "new";
    public string? AdmissionNo { get; private set; }
    public string PageTitle { get; private set; } = "Add New Student";

    public void OnGet(string? mode, string? adm)
    {
        Mode = string.IsNullOrWhiteSpace(mode) ? "new" : mode;
        AdmissionNo = adm;
        PageTitle = Mode switch
        {
            "view" => "View Portfolio",
            "edit" => "Edit Portfolio",
            _ => "Add New Student"
        };
        // TODO: load student portfolio when adm is supplied
    }

    public IActionResult OnPostDraft()
    {
        // TODO: save portfolio as draft
        return RedirectToPage("/StudentMaster");
    }

    public IActionResult OnPostSave()
    {
        // TODO: validate and save portfolio
        return RedirectToPage("/StudentMaster");
    }

    public IActionResult OnPostPrint()
    {
        // TODO: generate PDF in the approved portfolio format
        return Page();
    }
}
