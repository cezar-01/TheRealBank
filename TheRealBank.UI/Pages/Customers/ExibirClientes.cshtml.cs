using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheRealBank.Services.Customers;
using System.Security.Claims; 

namespace TheRealBank.UI.Pages.Customers
{
    public class ExibirClientesModel : PageModel
    {
        private readonly ICustomerService _customerService;

        public List<Customer> Clientes { get; set; } = new();

        public ExibirClientesModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Clientes = await _customerService.GetCustomersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostPromoteAsync(string cpf)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            if (!string.IsNullOrWhiteSpace(cpf))
                await _customerService.PromoteToAdminAsync(cpf);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDemoteAsync(string cpf)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var target = await _customerService.GetCustomerByCpfAsync(cpf);
            if (target is null) return RedirectToPage();
            if (target.Email == currentEmail) return RedirectToPage();
            await _customerService.DemoteFromAdminAsync(cpf);
            return RedirectToPage();
        }
    }
}
