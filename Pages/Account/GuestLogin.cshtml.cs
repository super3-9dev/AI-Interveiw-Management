using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace InterviewBot.Pages.Account
{
    public class GuestLoginModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "GuestUser"),
                new Claim("IsGuest", "true")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToPage("/Index");
        }
    }
}
