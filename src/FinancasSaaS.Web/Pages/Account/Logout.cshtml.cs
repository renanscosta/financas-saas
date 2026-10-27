using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;

namespace FinancasSaaS.Web.Pages.Account;

[Authorize]
public sealed class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        return LocalRedirect("/Account/Login");
    }
}
