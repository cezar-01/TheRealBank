using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Threading.Tasks;
using TheRealBank.Repositories.Users;
using System.Security.Claims;

namespace TheRealBank.UI.Pages.Experiencia
{
    public class LayoutModel : PageModel
    {
        private readonly ICustomerRepository _customers;

        public LayoutModel(ICustomerRepository customers)
        {
            _customers = customers;
        }

        public string FirstName { get; private set; } = "Cliente";
        public decimal Saldo { get; private set; } = 0m;

        public bool ShowBalance { get; private set; } = false;

        public async Task OnGetAsync([FromQuery] string? email)
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;

            var effectiveEmail = string.IsNullOrWhiteSpace(email) && isAuthenticated
                ? User.FindFirstValue(ClaimTypes.Email)
                : email;

            if (string.IsNullOrWhiteSpace(effectiveEmail))
                return;

            var customer = await _customers.GetByEmailAsync(effectiveEmail);
            if (customer is null)
                return;

            FirstName = GetFirstName(customer.Nome);
            Saldo = customer.Saldo;

            ShowBalance = User.IsInRole("User");
        }

        private static string GetFirstName(string? nome)
            => string.IsNullOrWhiteSpace(nome)
                ? "Cliente"
                : nome.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0];
    }
}
