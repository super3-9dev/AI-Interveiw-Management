using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InterviewBot.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public LoginModel(AppDbContext db) => _db = db;

        public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl ?? Url.Content("~/");

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid) return Page();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new("IsGuest", user.IsGuest.ToString())
            };

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                RedirectUri = returnUrl
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                authProperties);

            // Preserve culture parameter in redirect
            var culture = Request.Query["culture"].ToString();
            var redirectUrl = string.IsNullOrEmpty(culture) ? returnUrl : $"{returnUrl}?culture={culture}";
            return LocalRedirect(redirectUrl);
        }

        public async Task<IActionResult> OnPostGuestAsync()
        {
            // Find all demo emails
            var demoEmails = await _db.Users
                .Where(u => u.IsGuest && u.FullName.StartsWith("demo"))
                .Select(u => u.Email)
                .ToListAsync();

            int demoNumber = 1;
            while (demoEmails.Contains($"demo{demoNumber}@interviewbot.com"))
            {
                demoNumber++;
            }
            var guestUser = new User
            {
                Email = $"demo{demoNumber}@interviewbot.com",
                FullName = $"demo{demoNumber}",
                IsGuest = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = "" // Empty for guests
            };

            _db.Users.Add(guestUser);
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, guestUser.Id.ToString()),
                new(ClaimTypes.Email, guestUser.Email),
                new(ClaimTypes.Name, guestUser.FullName),
                new("IsGuest", "true")
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = false });

            // Preserve culture parameter in redirect
            var culture = Request.Query["culture"].ToString();
            var redirectUrl = string.IsNullOrEmpty(culture) ? "/" : $"/?culture={culture}";
            return LocalRedirect(redirectUrl);
        }
    }

    public class LoginInputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}