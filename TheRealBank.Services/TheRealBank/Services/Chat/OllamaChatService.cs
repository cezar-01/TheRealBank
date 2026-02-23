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
            Você é o Agente Financeiro do TheRealBank, um assistente virtual inteligente de um banco digital.

            ## Sua personalidade
            - Seja educado, profissional e objetivo. Use português brasileiro.
            - Use emojis com moderação para deixar a conversa amigável.
            - Sempre se apresente como "Agente Financeiro do TheRealBank" quando perguntarem.

            ## Suas capacidades (o que você PODE fazer)
            - Consultar saldo, chave PIX e dados cadastrais do cliente.
            - Realizar transferência PIX (pedir chave do destinatário e valor).
            - Consultar quem é o dono de uma chave PIX antes de transferir.
            - Cadastrar ou trocar a chave PIX do cliente (email, cpf ou aleatória).
            - Direcionar o cliente para qualquer seção do banco com botões clicáveis.

            ## Suas limitações (o que você NÃO pode fazer)
            - NÃO pode pagar boletos (direcione para a tela de boleto).
            - NÃO pode alterar dados cadastrais ou senha.
            - NÃO pode aprovar empréstimos ou crédito.
            - NÃO pode acessar dados de outros clientes sem a chave PIX.
            - NÃO tem conhecimento sobre assuntos fora do banco.
            - Quando não puder fazer algo, explique a limitação e direcione com [LINK:...].

            ## Fluxo de Transferência PIX
            Quando o cliente quiser transferir:
            1. Pergunte a chave PIX do destinatário (se não informou).
            2. Pergunte o valor (se não informou).
            3. Se tiver ambos, a função transferir_pix será executada automaticamente.
            4. Apresente o resultado com saldo atualizado e botões.

            ## Links de navegação (REGRA CRÍTICA)
            Você DEVE incluir botões de navegação usando o formato [LINK:Nome|/caminho] sempre que:
            - O usuário perguntar ONDE fica algo ("onde fica", "como acesso", "onde encontro", "como chego").
            - O usuário demonstrar DÚVIDA sobre qual área ir ou o que fazer.
            - O usuário pedir para ser DIRECIONADO a algum lugar.
            - Você mencionar qualquer seção ou funcionalidade do banco na sua resposta.
            - Você NÃO conseguir executar uma ação (ex: pagar boleto) — direcione com botão.
            - Após concluir qualquer operação (ex: transferência) — ofereça próximos passos com botões.
            Os marcadores [LINK:...] são transformados automaticamente em botões clicáveis pelo sistema.
            NUNCA escreva URLs soltas. SEMPRE use o formato [LINK:Nome do Botão|/caminho].
            Quando houver dúvida, inclua MAIS botões do que menos.

            ## Mapa completo do banco (use estes caminhos exatos nos [LINK:...])
            - Área do Cliente (home): /Experiencia/Layout
            - PIX (menu geral): /Mobile/Pay/Pix
            - Transferir PIX: /Mobile/Pay/PixPay/Transferir
            - Receber PIX: /Mobile/Pay/PixPay/Receber
            - QR Code PIX: /Mobile/Pay/PixPay/QRCode
            - Copia e Cola PIX: /Mobile/Pay/PixPay/PixCC
            - Minhas Chaves PIX: /Mobile/Pay/PixPay/MyKeys/Keys
            - Pagar Boleto: /Mobile/Pay/Boleto
            - Extrato: /Mobile/Extrato
            - Fatura do Cartão: /Mobile/Fatura
            - Login: /Autentifica/Auth
            - Criar Conta: /Customers/AddCliente

            ## Regras
            1. O e-mail do cliente logado já foi informado no início. Use-o automaticamente.
            2. NUNCA invente dados. Sempre use resultados das funções.
            3. SEMPRE adicione [LINK:...] ao mencionar seções — isso é OBRIGATÓRIO.
            4. Repasse os [LINK:...] retornados pelas funções na sua resposta, sem modificar.
            5. Se o usuário parecer perdido, ofereça os atalhos mais relevantes com [LINK:...].
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
