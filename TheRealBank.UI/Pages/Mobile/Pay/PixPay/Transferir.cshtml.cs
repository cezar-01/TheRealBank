using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TheRealBank.Repositories.Users;

namespace TheRealBank.UI.Pages.Mobile.Pay
{
    public class ContatoFrequente
    {
        public string Nome { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string Icone { get; set; } = "fa-solid fa-user";
    }

    public class TransferirModel : PageModel
    {
        private readonly ICustomerRepository _repo;

        public TransferirModel(ICustomerRepository repo) => _repo = repo;

        [BindProperty]
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Display(Name = "Valor")]
        public decimal? Valor { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A chave é obrigatória")]
        [Display(Name = "Chave PIX ou CPF/CNPJ")]
        public string Chave { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Descrição (Opcional)")]
        public string Descricao { get; set; } = string.Empty;

        public decimal SaldoDisponivel { get; private set; }
        public List<ContatoFrequente> Contatos { get; private set; } = new();
        public string? MensagemErro { get; private set; }
        public string? MensagemSucesso { get; private set; }

        public async Task OnGetAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var sender = await _repo.GetByEmailAsync(email);
                SaldoDisponivel = sender?.Saldo ?? 0m;
            }
            else
            {
                SaldoDisponivel = 0m;
            }

            Contatos = new List<ContatoFrequente>
            {
                new ContatoFrequente { Nome = "Maria Silva", Info = "PIX: maria@email.com", Icone = "fa-solid fa-user" },
                new ContatoFrequente { Nome = "João Santos", Info = "Ag: 0001 C: 12345-6", Icone = "fa-solid fa-user-tie" },
                new ContatoFrequente { Nome = "Padaria Pão Quente", Info = "CNPJ: **.345.678/0001-**", Icone = "fa-solid fa-store" }
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Valor is null)
            {
                await OnGetAsync();
                return Page();
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToPage("/Autentifica/Auth");

            var result = await _repo.TransferAsync(email, Chave.Trim(), Valor.Value, Descricao);

            switch (result.Status)
            {
                case TransferStatus.InvalidAmount:
                    MensagemErro = "Valor inválido.";
                    break;
                case TransferStatus.SenderNotFound:
                    MensagemErro = "Sua conta não foi encontrada.";
                    break;
                case TransferStatus.ReceiverNotFound:
                    MensagemErro = "Destinatário não encontrado pela chave informada.";
                    break;
                case TransferStatus.InsufficientFunds:
                    MensagemErro = "Saldo insuficiente para esta transferência.";
                    break;
                case TransferStatus.Success:
                    MensagemSucesso = $"Transferência realizada com sucesso. Novo saldo: {result.NewSenderBalance?.ToString("C")}";
                    break;
            }

            await OnGetAsync(); 
            return Page();
        }
    }
}