using Microsoft.Extensions.AI;

namespace TheRealBank.Services.Chat
{
    public class OllamaChatService
    {
        private readonly IChatClient _chatClient;

        public OllamaChatService(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async IAsyncEnumerable<string> StreamResponseAsync(
            List<ChatMessage> history,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return update.Text;
                }
            }
        }
    }
}
