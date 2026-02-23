using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace TheRealBank.UI.Pages.Mobile.Pay
{
    public class CopiaColaModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Você precisa colar um código PIX")]
        [Display(Name = "Código PIX Copia e Cola")]
        public string PixCode { get; set; }

        public decimal SaldoDisponivel { get; private set; }

        public void OnGet()
        {
            SaldoDisponivel = 1110473.88m;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                SaldoDisponivel = 1110473.88m;
                return Page();
            }

            
            return RedirectToPage("/Experiencia/Layout");
        }
    }
}