using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheRealBank.UI.Pages.ChatBot
{
    public class ChatModel : PageModel
    {
        public string? UserEmail { get; private set; }

        public void OnGet()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                UserEmail = User.FindFirstValue(ClaimTypes.Email);
            }
        }
    }
}
