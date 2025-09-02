using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace InterviewBot.Pages
{
    [Authorize]
    public class InterviewFormatModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        public void OnGet()
        {
            // The InterviewId is automatically bound from the query string
        }
    }
}
