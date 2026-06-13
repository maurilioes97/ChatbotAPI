namespace ChatbotAPI.Models
{
    /// <summary>
    /// Representa uma mensagem individual dentro de uma sessao.
    /// Pode ser uma fala do usuario ou uma resposta do assistente.
    /// </summary>
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navegacao para a sessao dona desta mensagem.
        public ChatSession? Session { get; set; }
    }
}
