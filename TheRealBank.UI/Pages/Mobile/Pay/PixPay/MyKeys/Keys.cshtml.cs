using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Linq;
using TheRealBank.Services.Customers;

namespace TheRealBank.UI.Pages.Mobile.Pay
{
    public class PixKey
    {
        public string Id { get; set; } = default!;
        public string Tipo { get; set; } = default!;
        public string Valor { get; set; } = default!;
        public string Icone { get; set; } = default!;
    }

    public class KeysModel : PageModel
    {
        private readonly ICustomerService _customers;

        public KeysModel(ICustomerService customers) => _customers = customers;

        public List<PixKey> ChavesPix { get; set; } = new();

        [BindProperty]
        [Required]
        public string SelectedKeyId { get; set; } = string.Empty; 
        public async Task OnGetAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var customer = string.IsNullOrWhiteSpace(email) ? null : await _customers.GetCustomerByEmailAsync(email);

            var maskedEmail = string.IsNullOrWhiteSpace(customer?.Email) ? "seu-email" : MaskEmail(customer!.Email!);
            var maskedCpf = string.IsNullOrWhiteSpace(customer?.CPF) ? "***.***.***-**" : MaskCpf(customer!.CPF!);

            ChavesPix = new List<PixKey>
            {
                new PixKey { Id = "email",  Tipo = "E-mail",          Valor = maskedEmail, Icone = "fas fa-envelope" },
                new PixKey { Id = "cpf",    Tipo = "CPF",             Valor = maskedCpf,   Icone = "fas fa-id-card" },
                new PixKey { Id = "random", Tipo = "Chave Aleatória", Valor = "Gerada na hora", Icone = "fas fa-key" }
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); 
                return Page();
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToPage("/Autentifica/Auth");

            var customer = await _customers.GetCustomerByEmailAsync(email);
            if (customer is null)
            {
                ModelState.AddModelError(string.Empty, "Usuário não encontrado.");
                await OnGetAsync(); 
                return Page();
            }

            var keyToSave = SelectedKeyId switch
            {
                "email"  => customer.Email!,
                "cpf"    => customer.CPF!,
                "random" => Guid.NewGuid().ToString("D"),
                _        => string.Empty
            };

            if (string.IsNullOrWhiteSpace(keyToSave))
            {
                ModelState.AddModelError(string.Empty, "Seleção inválida.");
                await OnGetAsync(); 
                return Page();
            }

            await _customers.SetPixKeyAsync(email, keyToSave);
            return RedirectToPage("/Mobile/Pay/PixPay/Receber", new { chave = keyToSave });
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***" + email;
            var visible = email[..1];
            return $"{visible}***{email[at..]}";
        }

        private static string MaskCpf(string cpf)
        {
            var digits = new string(cpf.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return "***.***.***-**";
            return $"{digits[..3]}.***.***-{digits[^2..]}";
        }
    }
}