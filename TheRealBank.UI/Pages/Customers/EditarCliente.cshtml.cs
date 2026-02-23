using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheRealBank.Services.Customers;

namespace TheRealBank.UI.Pages.Customers
{
    public class EditarClienteModel : PageModel
    {
        private readonly ICustomerService _service;

        public EditarClienteModel(ICustomerService service)
        {
            _service = service;
        }

        [BindProperty]
        public Customer Cliente { get; set; }

        public async Task<IActionResult> OnGetAsync(string cpf)
        {
            Cliente = await _service.GetCustomerByCpfAsync(cpf);
            if (Cliente == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string CPF)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _service.UpdateAsync(CPF, Cliente);

            return RedirectToPage("/Customers/ExibirClientes");
        }
    }
}