using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using TheRealBank.Services.Customers;

namespace TheRealBank.Services.Chat
{
    public class BankPlugin
    {
        private readonly ICustomerService _customerService;
        private static readonly CultureInfo BRL = new("pt-BR");

        public BankPlugin(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [KernelFunction("consultar_saldo")]
        [Description("Consulta o saldo da conta bancária do cliente pelo e-mail. Retorna o saldo formatado em reais.")]
        public async Task<string> ConsultarSaldoAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Não consegui identificar seu e-mail. Por favor, informe seu e-mail para consultar o saldo.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Não encontrei nenhuma conta com o e-mail '{email}'.";

            var nome = customer.Nome?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Cliente";
            return $"{nome}, seu saldo atual é de {customer.Saldo.ToString("C", BRL)}.";
        }

        [KernelFunction("consultar_chave_pix")]
        [Description("Consulta a chave PIX cadastrada do cliente pelo e-mail.")]
        public async Task<string> ConsultarChavePixAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Informe seu e-mail para que eu consulte sua chave PIX.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Não encontrei nenhuma conta com o e-mail '{email}'.";

            if (string.IsNullOrWhiteSpace(customer.KeyPix))
                return "Você ainda não tem uma chave PIX cadastrada. Acesse: Área do Cliente ? PIX ? Minhas Chaves.";

            return $"Sua chave PIX cadastrada é: {customer.KeyPix}";
        }

        [KernelFunction("consultar_dados_cliente")]
        [Description("Consulta os dados cadastrais do cliente (nome, CPF, e-mail, data de nascimento) pelo e-mail.")]
        public async Task<string> ConsultarDadosClienteAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Informe seu e-mail para que eu consulte seus dados.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Não encontrei nenhuma conta com o e-mail '{email}'.";

            return $"""
                Dados da conta:
                • Nome: {customer.Nome}
                • CPF: {customer.CPF}
                • E-mail: {customer.Email}
                • Data de nascimento: {customer.DataNascimento:dd/MM/yyyy}
                • Chave PIX: {(string.IsNullOrWhiteSpace(customer.KeyPix) ? "Não cadastrada" : customer.KeyPix)}
                """;
        }

        [KernelFunction("navegar_banco")]
        [Description("Indica ao cliente onde encontrar uma funcionalidade ou seção específica do banco digital TheRealBank. Use quando o cliente perguntar onde fica algo ou como acessar algo.")]
        public string NavegarBanco(
            [Description("Nome da funcionalidade que o cliente procura, por exemplo: pix, boleto, extrato, fatura, saldo, chave pix, criar conta, login, area do cliente")] string funcionalidade)
        {
            var key = (funcionalidade ?? "").Trim().ToLowerInvariant();

            return key switch
            {
                var k when k.Contains("pix") && (k.Contains("chave") || k.Contains("key") || k.Contains("minhas")) =>
                    "Para gerenciar suas chaves PIX, acesse: Área do Cliente ? PIX ? Minhas Chaves (/Mobile/Pay/PixPay/MyKeys/Keys).",

                var k when k.Contains("pix") && k.Contains("transfer") =>
                    "Para transferir via PIX, acesse: Área do Cliente ? PIX ? Transferir (/Mobile/Pay/PixPay/Transferir).",

                var k when k.Contains("pix") && k.Contains("receb") =>
                    "Para receber via PIX, acesse: Área do Cliente ? PIX ? Receber (/Mobile/Pay/PixPay/Receber).",

                var k when k.Contains("pix") && k.Contains("qr") =>
                    "Para gerar ou ler QR Code PIX, acesse: Área do Cliente ? PIX ? QR Code (/Mobile/Pay/PixPay/QRCode).",

                var k when k.Contains("pix") && k.Contains("copia") =>
                    "Para PIX Copia e Cola, acesse: Área do Cliente ? PIX ? Copia e Cola (/Mobile/Pay/PixPay/PixCC).",

                var k when k.Contains("pix") =>
                    "Para acessar todas as opções de PIX, vá em: Área do Cliente ? PIX (/Mobile/Pay/Pix). Lá você encontra: Transferir, Receber, QR Code, Copia e Cola e Minhas Chaves.",

                var k when k.Contains("boleto") =>
                    "Para pagar boletos, acesse: Área do Cliente ? Pagar Boleto (/Mobile/Pay/Boleto).",

                var k when k.Contains("extrato") =>
                    "Para ver seu extrato, acesse: Área do Cliente ? Ver Extrato (/Mobile/Extrato).",

                var k when k.Contains("fatura") || k.Contains("cartão") || k.Contains("cartao") || k.Contains("crédito") || k.Contains("credito") =>
                    "Para ver a fatura do cartão de crédito, acesse: Área do Cliente ? Fatura (/Mobile/Fatura).",

                var k when k.Contains("saldo") =>
                    "Seu saldo aparece na tela principal da Área do Cliente (/Experiencia/Layout). Se quiser, posso consultar seu saldo agora — basta me dizer seu e-mail.",

                var k when k.Contains("login") || k.Contains("entrar") || k.Contains("autenti") =>
                    "Para fazer login, acesse: Entrar (/Autentifica/Auth).",

                var k when k.Contains("criar conta") || k.Contains("cadastr") || k.Contains("novo cliente") || k.Contains("registr") =>
                    "Para criar uma nova conta, acesse: Novo Cliente (/Customers/AddCliente).",

                var k when k.Contains("área do cliente") || k.Contains("area do cliente") || k.Contains("home") || k.Contains("inicio") || k.Contains("início") =>
                    "A tela principal do banco (Área do Cliente) fica em: /Experiencia/Layout. Lá você vê saldo, cartão, transações e atalhos para PIX, Boleto, Cartões e Empréstimo.",

                var k when k.Contains("privacidade") =>
                    "A página de Privacidade fica em: /Privacy.",

                var k when k.Contains("chat") || k.Contains("ia") || k.Contains("assistente") =>
                    "Você já está no Chat IA! Mas o link direto é: /ChatBot/Chat.",

                _ => $"Não encontrei uma seção específica para '{funcionalidade}'. As principais áreas do banco são:\n" +
                     "• Área do Cliente (saldo, cartão, transações): /Experiencia/Layout\n" +
                     "• PIX (transferir, receber, QR Code, chaves): /Mobile/Pay/Pix\n" +
                     "• Pagar Boleto: /Mobile/Pay/Boleto\n" +
                     "• Extrato: /Mobile/Extrato\n" +
                     "• Fatura do cartão: /Mobile/Fatura\n" +
                     "• Login: /Autentifica/Auth\n" +
                     "• Criar conta: /Customers/AddCliente\n" +
                     "Posso te ajudar a encontrar algo mais específico?"
            };
        }
    }
}
