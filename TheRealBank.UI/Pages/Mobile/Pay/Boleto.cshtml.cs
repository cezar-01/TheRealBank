using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace TheRealBank.UI.Pages.Mobile.Pay
{
    public class BoletoModel : PageModel
    {
        [BindProperty]
        [Display(Name = "Código de Barras")]
        [Required(ErrorMessage = "Você precisa digitar ou colar o código de barras.")]
        public string CodigoDeBarras { get; set; }

        public decimal SaldoDisponivel { get; private set; }

        public void OnGet()
        {
            // Puxa o saldo (estou usando o valor do  print)
            SaldoDisponivel = 1110473.88m;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                OnGet();
                return Page();
            }

            return RedirectToPage("/Experiencia/Layout");
        }
    }
}