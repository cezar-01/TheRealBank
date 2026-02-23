using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheRealBank.Services.Customers;

namespace TheRealBank.UI.Pages.Customers
{
    public class Excluir_ClienteModel : PageModel
    {

        private readonly ICustomerService _service;

        public Excluir_ClienteModel(ICustomerService service)
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (Cliente == null || string.IsNullOrEmpty(Cliente.CPF))
            {
                return BadRequest();
            }

            await _service.DeleteAsync(Cliente.CPF);

            return RedirectToPage("/Customers/ExibirClientes");
        }

    }

}