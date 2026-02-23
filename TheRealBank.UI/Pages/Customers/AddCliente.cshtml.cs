using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TheRealBank.Services.Customers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace TheRealBank.UI.Pages.Customers
{
    public class AddClienteModel : PageModel
    {
        private readonly ICustomerService _customerService;

        [BindProperty]
        public Customer Cliente { get; set; }

        public AddClienteModel(ICustomerService customerService) => _customerService = customerService;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            await _customerService.AddCustomerAsync(Cliente);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Cliente.CPF ?? Cliente.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, Cliente.Nome ?? Cliente.Email ?? "Cliente"),
                new Claim(ClaimTypes.Email, Cliente.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToPage("/Experiencia/Layout");
        }
    }
}