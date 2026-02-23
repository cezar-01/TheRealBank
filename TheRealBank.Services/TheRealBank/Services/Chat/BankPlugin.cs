using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using TheRealBank.Repositories.Users;
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
        [Description("Consulta o saldo da conta bancária do cliente pelo e-mail.")]
        public async Task<string> ConsultarSaldoAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Informe seu e-mail para consultar o saldo.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Conta não encontrada para '{email}'.";

            var nome = customer.Nome?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Cliente";
            return $"{nome}, saldo: {customer.Saldo.ToString("C", BRL)}\n[LINK:Ver Extrato|/Mobile/Extrato]\n[LINK:Área do Cliente|/Experiencia/Layout]";
        }

        [KernelFunction("consultar_chave_pix")]
        [Description("Consulta a chave PIX cadastrada do cliente pelo e-mail.")]
        public async Task<string> ConsultarChavePixAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Informe seu e-mail para consultar sua chave PIX.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Conta não encontrada para '{email}'.";

            if (string.IsNullOrWhiteSpace(customer.KeyPix))
                return "Você não tem chave PIX cadastrada.\n[LINK:Cadastrar Chave|/Mobile/Pay/PixPay/MyKeys/Keys]";

            return $"Sua chave PIX: {customer.KeyPix}\n[LINK:Gerenciar Chaves|/Mobile/Pay/PixPay/MyKeys/Keys]\n[LINK:Transferir|/Mobile/Pay/PixPay/Transferir]\n[LINK:Receber|/Mobile/Pay/PixPay/Receber]";
        }

        [KernelFunction("consultar_dados_cliente")]
        [Description("Consulta os dados cadastrais do cliente pelo e-mail.")]
        public async Task<string> ConsultarDadosClienteAsync(
            [Description("E-mail do cliente")] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Informe seu e-mail para consultar seus dados.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Conta não encontrada para '{email}'.";

            return $"• Nome: {customer.Nome}\n" +
                   $"• CPF: {customer.CPF}\n" +
                   $"• E-mail: {customer.Email}\n" +
                   $"• Nascimento: {customer.DataNascimento:dd/MM/yyyy}\n" +
                   $"• Chave PIX: {(string.IsNullOrWhiteSpace(customer.KeyPix) ? "Não cadastrada" : customer.KeyPix)}\n" +
                   $"[LINK:Área do Cliente|/Experiencia/Layout]";
        }

        [KernelFunction("transferir_pix")]
        [Description("Realiza uma transferência PIX do cliente logado para um destinatário identificado pela chave PIX. Requer e-mail do remetente, chave PIX do destinatário e valor.")]
        public async Task<string> TransferirPixAsync(
            [Description("E-mail do remetente (cliente logado)")] string emailRemetente,
            [Description("Chave PIX do destinatário")] string chaveDestinatario,
            [Description("Valor da transferência em reais")] decimal valor,
            [Description("Descrição opcional")] string? descricao = null)
        {
            if (string.IsNullOrWhiteSpace(emailRemetente))
                return "Preciso do seu e-mail para realizar a transferência. Faça login ou informe seu e-mail.";

            if (string.IsNullOrWhiteSpace(chaveDestinatario))
                return "Informe a chave PIX do destinatário (e-mail, CPF ou chave aleatória).";

            if (valor <= 0)
                return "O valor da transferência precisa ser maior que zero.";

            var result = await _customerService.TransferPixAsync(emailRemetente, chaveDestinatario, valor, descricao);

            return result.Status switch
            {
                TransferStatus.Success =>
                    $"? Transferência de {valor.ToString("C", BRL)} realizada com sucesso!\n" +
                    $"Novo saldo: {result.NewSenderBalance?.ToString("C", BRL)}\n" +
                    $"[LINK:Ver Extrato|/Mobile/Extrato]\n[LINK:Nova Transferência|/Mobile/Pay/PixPay/Transferir]",

                TransferStatus.SenderNotFound =>
                    "Não encontrei sua conta. Verifique se você está logado.\n[LINK:Fazer Login|/Autentifica/Auth]",

                TransferStatus.ReceiverNotFound =>
                    $"Destinatário não encontrado com a chave '{chaveDestinatario}'. Verifique a chave e tente novamente.\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]",

                TransferStatus.InsufficientFunds =>
                    $"Saldo insuficiente. Seu saldo atual é {result.NewSenderBalance?.ToString("C", BRL)}.\n[LINK:Ver Extrato|/Mobile/Extrato]",

                TransferStatus.InvalidAmount =>
                    "Valor inválido. Informe um valor positivo.",

                _ => "Ocorreu um erro inesperado. Tente novamente pela tela de transferência.\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]"
            };
        }

        [KernelFunction("consultar_destinatario_pix")]
        [Description("Consulta quem é o dono de uma chave PIX antes de transferir. Retorna o nome do destinatário.")]
        public async Task<string> ConsultarDestinatarioPixAsync(
            [Description("Chave PIX do destinatário (e-mail, CPF ou chave aleatória)")] string chavePix)
        {
            if (string.IsNullOrWhiteSpace(chavePix))
                return "Informe a chave PIX para consultar o destinatário.";

            var customer = await _customerService.GetCustomerByPixKeyAsync(chavePix);
            if (customer is null)
                return $"Nenhum destinatário encontrado com a chave '{chavePix}'. Verifique se a chave está correta.";

            var nome = customer.Nome ?? "Destinatário";
            return $"Destinatário encontrado: {nome}\nChave: {chavePix}\n\nDeseja transferir? Informe o valor.\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]";
        }

        [KernelFunction("cadastrar_chave_pix")]
        [Description("Cadastra ou altera a chave PIX do cliente. O tipo pode ser 'email', 'cpf' ou 'aleatoria'.")]
        public async Task<string> CadastrarChavePixAsync(
            [Description("E-mail do cliente")] string email,
            [Description("Tipo da chave: 'email', 'cpf' ou 'aleatoria'")] string tipo)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Preciso do seu e-mail para cadastrar a chave PIX.";

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer is null)
                return $"Não encontrei sua conta com o e-mail '{email}'.";

            var novaChave = tipo?.Trim().ToLowerInvariant() switch
            {
                "email" => customer.Email ?? email,
                "cpf" => customer.CPF ?? "",
                "aleatoria" or "aleatorio" or "random" => Guid.NewGuid().ToString("D"),
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(novaChave))
                return "Tipo de chave inválido. Escolha: email, cpf ou aleatoria.\n[LINK:Gerenciar Chaves|/Mobile/Pay/PixPay/MyKeys/Keys]";

            await _customerService.SetPixKeyAsync(email, novaChave);

            var tipoFormatado = tipo?.ToLowerInvariant() switch
            {
                "email" => "E-mail",
                "cpf" => "CPF",
                _ => "Aleatória"
            };

            return $"? Chave PIX cadastrada com sucesso!\nTipo: {tipoFormatado}\nChave: {novaChave}\n[LINK:Receber PIX|/Mobile/Pay/PixPay/Receber]\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]";
        }

        [KernelFunction("navegar_banco")]
        [Description("Indica ao cliente onde encontrar uma funcionalidade ou seção do banco.")]
        public string NavegarBanco(
            [Description("Nome da funcionalidade")] string funcionalidade)
        {
            var key = (funcionalidade ?? "").Trim().ToLowerInvariant();

            return key switch
            {
                var k when k.Contains("pix") && (k.Contains("chave") || k.Contains("key") || k.Contains("minhas") || k.Contains("cadastr")) =>
                    "Acesse suas chaves PIX:\n[LINK:Minhas Chaves PIX|/Mobile/Pay/PixPay/MyKeys/Keys]",

                var k when k.Contains("pix") && (k.Contains("transfer") || k.Contains("enviar") || k.Contains("mandar")) =>
                    "Acesse a transferência PIX:\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]",

                var k when k.Contains("pix") && (k.Contains("receb") || k.Contains("cobr")) =>
                    "Acesse o recebimento PIX:\n[LINK:Receber PIX|/Mobile/Pay/PixPay/Receber]",

                var k when k.Contains("pix") && k.Contains("qr") =>
                    "Acesse o QR Code PIX:\n[LINK:QR Code PIX|/Mobile/Pay/PixPay/QRCode]",

                var k when k.Contains("pix") && (k.Contains("copia") || k.Contains("cola")) =>
                    "Acesse o PIX Copia e Cola:\n[LINK:PIX Copia e Cola|/Mobile/Pay/PixPay/PixCC]",

                var k when k.Contains("pix") =>
                    "Opções de PIX:\n" +
                    "[LINK:Transferir|/Mobile/Pay/PixPay/Transferir]\n" +
                    "[LINK:Receber|/Mobile/Pay/PixPay/Receber]\n" +
                    "[LINK:QR Code|/Mobile/Pay/PixPay/QRCode]\n" +
                    "[LINK:Copia e Cola|/Mobile/Pay/PixPay/PixCC]\n" +
                    "[LINK:Minhas Chaves|/Mobile/Pay/PixPay/MyKeys/Keys]",

                var k when k.Contains("transferên") || k.Contains("transferen") || k.Contains("enviar dinheiro") || k.Contains("mandar dinheiro") =>
                    "Acesse a transferência:\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]",

                var k when k.Contains("boleto") =>
                    "Acesse a tela de boleto:\n[LINK:Pagar Boleto|/Mobile/Pay/Boleto]",

                var k when k.Contains("pagamento") || k.Contains("pagar") =>
                    "Opções de pagamento:\n[LINK:Pagar Boleto|/Mobile/Pay/Boleto]\n[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]\n[LINK:PIX Copia e Cola|/Mobile/Pay/PixPay/PixCC]",

                var k when k.Contains("extrato") || k.Contains("histórico") || k.Contains("historico") || k.Contains("movimenta") =>
                    "Acesse seu extrato:\n[LINK:Ver Extrato|/Mobile/Extrato]",

                var k when k.Contains("fatura") || k.Contains("cartão") || k.Contains("cartao") || k.Contains("crédito") || k.Contains("credito") || k.Contains("débito") || k.Contains("debito") =>
                    "Acesse a fatura do cartão:\n[LINK:Ver Fatura|/Mobile/Fatura]",

                var k when k.Contains("saldo") =>
                    "Seu saldo está na tela principal:\n[LINK:Área do Cliente|/Experiencia/Layout]",

                var k when k.Contains("receb") || k.Contains("cobrança") || k.Contains("cobranca") =>
                    "Opções para receber:\n[LINK:Receber PIX|/Mobile/Pay/PixPay/Receber]\n[LINK:QR Code PIX|/Mobile/Pay/PixPay/QRCode]",

                var k when k.Contains("login") || k.Contains("entrar") || k.Contains("autenti") || k.Contains("logar") =>
                    "Acesse o login:\n[LINK:Entrar|/Autentifica/Auth]",

                var k when kContains("criar conta") || k.Contains("novo cliente") || k.Contains("registr") || k.Contains("abrir conta") || k.Contains("cadastr") =>
                    "Crie sua conta:\n[LINK:Criar Conta|/Customers/AddCliente]",

                var k when k.Contains("área do cliente") || k.Contains("area do cliente") || k.Contains("home")
                    || k.Contains("inicio") || k.Contains("início") || k.Contains("painel")
                    || k.Contains("tela principal") || k.Contains("dashboard") =>
                    "Acesse a tela principal:\n[LINK:Área do Cliente|/Experiencia/Layout]",

                var k when k.Contains("minha conta") || k.Contains("conta") || k.Contains("perfil")
                    || k.Contains("meus dados") || k.Contains("meu cadastro") =>
                    "Acesse sua conta:\n[LINK:Área do Cliente|/Experiencia/Layout]",

                var k when k.Contains("senha") || k.Contains("seguranç") || k.Contains("seguranc") =>
                    "Não altero senhas pelo chat. Acesse sua conta:\n[LINK:Área do Cliente|/Experiencia/Layout]\n[LINK:Login|/Autentifica/Auth]",

                var k when k.Contains("empréstimo") || k.Contains("emprestimo") =>
                    "Empréstimos não estão disponíveis pelo chat:\n[LINK:Área do Cliente|/Experiencia/Layout]",

                var k when k.Contains("privacidade") =>
                    "Acesse nossa privacidade:\n[LINK:Privacidade|/Privacy]",

                var k when k.Contains("ajuda") || k.Contains("help") || k.Contains("suporte") =>
                    "Posso te ajudar! Principais áreas:\n" +
                    "[LINK:Área do Cliente|/Experiencia/Layout]\n" +
                    "[LINK:PIX|/Mobile/Pay/Pix]\n" +
                    "[LINK:Pagar Boleto|/Mobile/Pay/Boleto]\n" +
                    "[LINK:Extrato|/Mobile/Extrato]\n" +
                    "[LINK:Fatura|/Mobile/Fatura]",

                var k when k.Contains("chat") || k.Contains("ia") || k.Contains("assistente") || k.Contains("agente") =>
                    "Você já está no Chat IA! Como posso ajudar?",

                _ => "Principais áreas do banco:\n" +
                     "[LINK:Área do Cliente|/Experiencia/Layout]\n" +
                     "[LINK:PIX|/Mobile/Pay/Pix]\n" +
                     "[LINK:Transferir PIX|/Mobile/Pay/PixPay/Transferir]\n" +
                     "[LINK:Receber PIX|/Mobile/Pay/PixPay/Receber]\n" +
                     "[LINK:Minhas Chaves PIX|/Mobile/Pay/PixPay/MyKeys/Keys]\n" +
                     "[LINK:Pagar Boleto|/Mobile/Pay/Boleto]\n" +
                     "[LINK:Extrato|/Mobile/Extrato]\n" +
                     "[LINK:Fatura|/Mobile/Fatura]"
            };
        }
    }
}
