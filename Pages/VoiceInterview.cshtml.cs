using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace InterviewBot.Pages
{
    [Authorize]
    public class VoiceInterviewModel : PageModel
    {
        public void OnGet()
        {
            // Page logic can be added here if needed
        }
    }
}
