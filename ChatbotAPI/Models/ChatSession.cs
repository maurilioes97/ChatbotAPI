namespace ChatbotAPI.Models
{
    /// <summary>
    /// Representa uma sessao de conversa.
    /// Guarda o prompt base e o contexto de documento associado ao chat.
    /// </summary>
    public class ChatSession
    {
        public int Id { get; set; }
        public string SystemPrompt { get; set; } = string.Empty;
        public string? DocumentName { get; set; }
        public string? DocumentContext { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Uma sessao pode possuir varias mensagens ao longo da conversa.
        public List<ChatMessage> Messages { get; set; } = new();
    }
}
