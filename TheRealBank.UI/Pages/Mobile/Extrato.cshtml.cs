using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TheRealBank.UI.Pages.Mobile
{
    public class Transacao
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public string Tipo { get; set; } 
        public string Icone { get; set; }
    }

    public class ExtratoModel : PageModel
    {
        public List<Transacao> Transacoes { get; set; } = new List<Transacao>();

        [BindProperty(SupportsGet = true)]
        public int? Mes { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Ano { get; set; }

        public List<SelectListItem> Meses { get; }
        public List<SelectListItem> Anos { get; }

        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public decimal SaldoMes { get; set; }

        public ExtratoModel()
        {
            Meses = new List<SelectListItem>
            {
                new SelectListItem("Janeiro", "1"), new SelectListItem("Fevereiro", "2"),
                new SelectListItem("Março", "3"), new SelectListItem("Abril", "4"),
                new SelectListItem("Maio", "5"), new SelectListItem("Junho", "6"),
                new SelectListItem("Julho", "7"), new SelectListItem("Agosto", "8"),
                new SelectListItem("Setembro", "9"), new SelectListItem("Outubro", "10"),
                new SelectListItem("Novembro", "11"), new SelectListItem("Dezembro", "12")
            };

            Anos = new List<SelectListItem>
            {
                new SelectListItem("2025", "2025"),
                new SelectListItem("2024", "2024"),
                new SelectListItem("2023", "2023")
            };
        }

        public void OnGet()
        {
            if (!Mes.HasValue) Mes = DateTime.Now.Month;
            if (!Ano.HasValue) Ano = DateTime.Now.Year;

            var dadosDoBanco = new List<Transacao>
            {
                new Transacao { Id = 1, Descricao = "Salário Empresa X", Data = new DateTime(2025, 11, 5), Valor = 5000.00m, Tipo = "Entrada", Icone = "fa-solid fa-briefcase" },
                new Transacao { Id = 2, Descricao = "Compra Supermercado", Data = new DateTime(2025, 11, 6), Valor = -350.20m, Tipo = "Saida", Icone = "fa-solid fa-shopping-cart" },
                new Transacao { Id = 3, Descricao = "Pix Recebido - Maria", Data = new DateTime(2025, 11, 7), Valor = 200.00m, Tipo = "Entrada", Icone = "fa-solid fa-qrcode" },
                new Transacao { Id = 4, Descricao = "Pagamento iFood", Data = new DateTime(2025, 11, 8), Valor = -89.90m, Tipo = "Saida", Icone = "fa-solid fa-utensils" },
                new Transacao { Id = 5, Descricao = "Gasolina Posto Shell", Data = new DateTime(2025, 11, 10), Valor = -150.00m, Tipo = "Saida", Icone = "fa-solid fa-gas-pump" },
                new Transacao { Id = 6, Descricao = "Depósito", Data = new DateTime(2024, 10, 20), Valor = 100.00m, Tipo = "Entrada", Icone = "fa-solid fa-download" },
            };

            Transacoes = dadosDoBanco
                .Where(t => t.Data.Month == Mes && t.Data.Year == Ano)
                .OrderByDescending(t => t.Data)
                .ToList();

            TotalEntradas = Transacoes.Where(t => t.Tipo == "Entrada").Sum(t => t.Valor);
            TotalSaidas = Transacoes.Where(t => t.Tipo == "Saida").Sum(t => t.Valor);
            SaldoMes = TotalEntradas + TotalSaidas;
        }
    }
}