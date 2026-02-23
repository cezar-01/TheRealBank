using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheRealBank.UI.Pages.Mobile.Pay
{
    public class QRCodeModel : PageModel
    {
        public string MinhaChavePix { get; set; }

        public void OnGet()
        {
            MinhaChavePix = "123.456.789-00";
        }
    }
}