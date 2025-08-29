using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace InterviewBot.Pages
{
    [Authorize]
    public class TextInterviewModel : PageModel
    {
        public void OnGet()
        {
            // Page logic can be added here if needed
        }
    }
}
