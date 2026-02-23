using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace TheRealBank.Services.Chat
{
    public class OllamaChatService
    {
        private readonly IChatClient _chatClient;
        private readonly BankPlugin _bankPlugin;

        private const string SystemPrompt = """
            Você é o Agente Financeiro do TheRealBank, assistente virtual de um banco digital.

            ## REGRA PRINCIPAL DE RESPOSTA
            Seja EXTREMAMENTE curto e direto. Máximo 1-2 frases antes dos botões.
            NUNCA explique passo a passo como chegar em uma tela. Apenas dê o botão.
            NUNCA liste URLs. Use APENAS o formato [LINK:Nome|/caminho].

            ## Exemplos de respostas CORRETAS:
            Pergunta: "Onde fico o PIX?"
            Resposta: "Aqui estão as opções de PIX:
            [LINK:Transferir|/Mobile/Pay/PixPay/Transferir]
            [LINK:Receber|/Mobile/Pay/PixPay/Receber]
            [LINK:Minhas Chaves|/Mobile/Pay/PixPay/MyKeys/Keys]"

            Pergunta: "Como pago um boleto?"
            Resposta: "Acesse a tela de boleto:
            [LINK:Pagar Boleto|/Mobile/Pay/Boleto]"

            Pergunta: "Quero ver meu extrato"
            Resposta: "Aqui está:
            [LINK:Ver Extrato|/Mobile/Extrato]"

            ## Exemplos de respostas ERRADAS (NÃO faça isso):
            ? "Para acessar o PIX, você pode ir na Área do Cliente, depois clicar em PIX, e lá você encontra..."
            ? "O caminho é /Mobile/Pay/Pix, onde você pode..."
            ? Textos longos explicando funcionalidades

            ## Suas capacidades
            - Consultar saldo, chave PIX e dados cadastrais.
            - Transferência PIX (chave + valor).
            - Cadastrar chave PIX (email, cpf ou aleatória).
            - Direcionar com botões clicáveis.

            ## Suas limitações
            - NÃO paga boletos, NÃO altera dados/senha, NÃO aprova empréstimos.
            - Quando não puder fazer algo, diga em 1 frase e dê o botão da tela certa.

            ## Mapa do banco (caminhos para [LINK:...])
            - Área do Cliente: /Experiencia/Layout
            - PIX (menu): /Mobile/Pay/Pix
            - Transferir PIX: /Mobile/Pay/PixPay/Transferir
            - Receber PIX: /Mobile/Pay/PixPay/Receber
            - QR Code PIX: /Mobile/Pay/PixPay/QRCode
            - Copia e Cola: /Mobile/Pay/PixPay/PixCC
            - Minhas Chaves: /Mobile/Pay/PixPay/MyKeys/Keys
            - Pagar Boleto: /Mobile/Pay/Boleto
            - Extrato: /Mobile/Extrato
            - Fatura: /Mobile/Fatura
            - Login: /Autentifica/Auth
            - Criar Conta: /Customers/AddCliente

            ## Regras
            1. E-mail do cliente já informado no início. Use automaticamente.
            2. NUNCA invente dados. Use resultados das funções.
            3. SEMPRE use [LINK:...] ao mencionar seções. NUNCA URLs soltas.
            4. Repasse [LINK:...] das funções sem modificar.
            5. Quando o usuário quer ser direcionado: resposta curta + botões. Nada mais.
            """;

        public OllamaChatService(IChatClient chatClient, BankPlugin bankPlugin)
        {
            _chatClient = chatClient;
            _bankPlugin = bankPlugin;
        }

        public async IAsyncEnumerable<string> StreamResponseAsync(
            List<ChatMessage> history,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var functionResult = await TryExecuteFunctionAsync(history, cancellationToken);

            var messagesWithSystem = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt)
            };
            messagesWithSystem.AddRange(history);

            if (functionResult is not null)
            {
                messagesWithSystem.Add(new ChatMessage(ChatRole.System,
                    $"[Resultado da função executada]\n{functionResult}"));
            }

            await foreach (var update in _chatClient.GetStreamingResponseAsync(
                messagesWithSystem, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return update.Text;
                }
            }
        }

        private async Task<string?> TryExecuteFunctionAsync(
            List<ChatMessage> history, CancellationToken ct)
        {
            var lastMessage = history.LastOrDefault();
            if (lastMessage is null) return null;

            var userText = lastMessage.Text?.ToLowerInvariant() ?? "";
            var email = ExtractEmail(history);

            // --- PIX Transfer: detect "transferir X para CHAVE" pattern ---
            if (ContainsAny(userText, "transferir", "enviar", "mandar", "pagar pix", "fazer pix"))
            {
                var valor = ExtractDecimal(userText);
                var chave = ExtractPixKey(userText);

                // If we have both value and key, execute transfer
                if (valor > 0 && chave is not null && email is not null)
                    return await _bankPlugin.TransferirPixAsync(email, chave, valor);

                // If only key, ask for value
                if (chave is not null && valor <= 0)
                {
                    var dest = await _bankPlugin.ConsultarDestinatarioPixAsync(chave);
                    return dest;
                }

                // Otherwise let the LLM guide the conversation
                return _bankPlugin.NavegarBanco("pix transferir");
            }

            // --- Consultar destinatário ---
            if (ContainsAny(userText, "quem é", "quem e", "dono da chave", "destinatário", "destinatario", "para quem"))
            {
                var chave = ExtractPixKey(userText);
                if (chave is not null)
                    return await _bankPlugin.ConsultarDestinatarioPixAsync(chave);
            }

            // --- Cadastrar chave PIX ---
            if (ContainsAny(userText, "cadastrar chave", "registrar chave", "criar chave", "trocar chave", "mudar chave", "nova chave"))
            {
                if (email is null) return null;

                var tipo = "aleatoria";
                if (ContainsAny(userText, "email", "e-mail")) tipo = "email";
                else if (ContainsAny(userText, "cpf")) tipo = "cpf";

                return await _bankPlugin.CadastrarChavePixAsync(email, tipo);
            }

            // --- Consultar saldo ---
            if (ContainsAny(userText, "saldo", "quanto tenho", "quanto eu tenho", "meu dinheiro"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarSaldoAsync(email);
                return null;
            }

            // --- Consultar chave PIX ---
            if (ContainsAny(userText, "chave pix", "minha chave", "minhas chaves", "key pix"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarChavePixAsync(email);
                return null;
            }

            // --- Consultar dados ---
            if (ContainsAny(userText, "meus dados", "meu cadastro", "minha informação", "minhas informações", "dados cadastrais"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarDadosClienteAsync(email);
                return null;
            }

            // --- Navegação geral (perguntas de direcionamento) ---
            if (ContainsAny(userText, "onde fica", "como acesso", "como faço para", "onde encontro",
                "como chego", "onde está", "onde esta", "aonde", "me leva", "me leve",
                "caminho para", "como ir", "ir para", "quero ir", "levar para",
                "quero acessar", "navegar", "abrir", "mostrar", "mostre",
                "quero ver", "preciso", "direcione", "redirecione", "me mande"))
            {
                return _bankPlugin.NavegarBanco(userText);
            }

            // --- Navegação por menção a funcionalidade ---
            if (ContainsAny(userText, "pix", "boleto", "extrato", "fatura", "cartão", "cartao",
                "receber", "qr code", "qrcode", "copia e cola", "login", "entrar",
                "criar conta", "empréstimo", "emprestimo",
                "pagamento", "pagar", "transferência", "transferencia",
                "home", "início", "inicio", "área do cliente", "area do cliente", "painel",
                "conta", "minha conta", "perfil", "senha", "segurança", "seguranca",
                "ajuda", "help", "suporte", "privacidade",
                "histórico", "historico", "movimentação", "movimentacao",
                "chat", "assistente", "agente",
                "recebimento", "cobrança", "cobranca", "débito", "debito", "crédito", "credito"))
            {
                return _bankPlugin.NavegarBanco(userText);
            }

            return null;
        }

        private static string? ExtractEmail(List<ChatMessage> history)
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var text = history[i].Text ?? "";
                var match = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                if (match.Success)
                    return match.Value;
            }
            return null;
        }

        private static string? ExtractPixKey(string text)
        {
            // Try email pattern
            var emailMatch = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            if (emailMatch.Success) return emailMatch.Value;

            // Try CPF pattern (xxx.xxx.xxx-xx or 11 digits)
            var cpfMatch = Regex.Match(text, @"\d{3}\.?\d{3}\.?\d{3}-?\d{2}");
            if (cpfMatch.Success) return cpfMatch.Value;

            // Try UUID/random key
            var uuidMatch = Regex.Match(text, @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}", RegexOptions.IgnoreCase);
            if (uuidMatch.Success) return uuidMatch.Value;

            return null;
        }

        private static decimal ExtractDecimal(string text)
        {
            // Match patterns like: R$ 100, R$100,50, 100.50, 100,50, 50 reais
            var match = Regex.Match(text, @"r?\$?\s*(\d{1,}[\.,]?\d{0,2})");
            if (match.Success)
            {
                var raw = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var val))
                    return val;
            }

            // Try "X reais"
            match = Regex.Match(text, @"(\d+[\.,]?\d*)\s*reais?");
            if (match.Success)
            {
                var raw = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var val))
                    return val;
            }

            return 0m;
        }

        private static bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
