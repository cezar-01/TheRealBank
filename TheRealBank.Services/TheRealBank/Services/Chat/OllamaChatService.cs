using System.Runtime.CompilerServices;
using System.Text.Json;
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
            - Seja educado, profissional e objetivo.
            - Use português brasileiro.
            - Use emojis com moderação para deixar a conversa amigável.
            - Sempre se apresente como "Agente Financeiro do TheRealBank" quando perguntarem quem você é.

            ## Suas capacidades
            Você tem acesso a funções que podem ser chamadas para ajudar o cliente:
            - **consultar_saldo**: Consulta o saldo do cliente pelo e-mail.
            - **consultar_chave_pix**: Consulta a chave PIX do cliente pelo e-mail.
            - **consultar_dados_cliente**: Consulta dados cadastrais do cliente pelo e-mail.
            - **navegar_banco**: Indica ao cliente onde encontrar funcionalidades do banco.

            ## Conhecimento do banco (RAG)
            O TheRealBank é um banco digital com as seguintes seções e funcionalidades:

            ### Área do Cliente (/Experiencia/Layout)
            - Tela principal após login.
            - Mostra saldo da conta, cartão de crédito, últimas transações.
            - Atalhos rápidos: PIX, Pagar Boleto, Cartões, Empréstimo.

            ### PIX (/Mobile/Pay/Pix)
            - Transferir para outra conta via chave PIX: /Mobile/Pay/PixPay/Transferir
            - Receber pagamentos: /Mobile/Pay/PixPay/Receber
            - QR Code (gerar/ler): /Mobile/Pay/PixPay/QRCode
            - PIX Copia e Cola: /Mobile/Pay/PixPay/PixCC
            - Gerenciar Minhas Chaves PIX: /Mobile/Pay/PixPay/MyKeys/Keys

            ### Pagamentos
            - Pagar Boleto: /Mobile/Pay/Boleto

            ### Consultas
            - Extrato bancário: /Mobile/Extrato
            - Fatura do cartão de crédito: /Mobile/Fatura

            ### Acesso
            - Login / Entrar: /Autentifica/Auth
            - Criar nova conta: /Customers/AddCliente
            - Chat IA (você): /ChatBot/Chat

            ## Regras importantes
            1. Quando o cliente pedir para consultar saldo, chave PIX ou dados, peça o e-mail dele se ainda não souber.
            2. Quando o cliente perguntar onde fica algo ou como acessar uma funcionalidade, use a função navegar_banco.
            3. NUNCA invente dados financeiros. Sempre use as funções para buscar dados reais.
            4. Se não souber responder algo fora do contexto bancário, diga que é um agente financeiro e só pode ajudar com assuntos do banco.
            5. Quando retornar resultados das funções, apresente de forma amigável e formatada.
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

            if (ContainsAny(userText, "saldo", "quanto tenho", "quanto eu tenho", "meu dinheiro", "minha conta"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarSaldoAsync(email);
                return null;
            }

            if (ContainsAny(userText, "chave pix", "minha chave", "minhas chaves", "key pix"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarChavePixAsync(email);
                return null;
            }

            if (ContainsAny(userText, "meus dados", "meu cadastro", "minha informação", "minhas informações", "dados cadastrais"))
            {
                if (email is not null)
                    return await _bankPlugin.ConsultarDadosClienteAsync(email);
                return null;
            }

            if (ContainsAny(userText, "onde fica", "como acesso", "como faço para", "onde encontro",
                "como chego", "onde está", "aonde", "me leva", "caminho para", "como ir"))
            {
                return _bankPlugin.NavegarBanco(userText);
            }

            if (ContainsAny(userText, "pix", "boleto", "extrato", "fatura", "cartão", "cartao",
                "transferir", "transferência", "transferencia", "login", "entrar",
                "criar conta", "cadastrar", "empréstimo", "emprestimo"))
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
                var match = System.Text.RegularExpressions.Regex.Match(
                    text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                if (match.Success)
                    return match.Value;
            }
            return null;
        }

        private static bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
