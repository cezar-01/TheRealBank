
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TheRealBank.Services.Customers;

namespace TheRealBank.UI.Pages.Customers
{
    public class MostrarClienteModel : PageModel
    {
        private readonly ICustomerService _customerService;

        public Customer Cliente { get; set; }


        public MostrarClienteModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> OnGetAsync(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
            {
                return NotFound();
            }

            Cliente = await _customerService.GetCustomerByCpfAsync(cpf);

            if (Cliente == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}