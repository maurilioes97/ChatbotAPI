namespace ChatbotAPI.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string SystemPrompt { get; set; } = string.Empty;
        public string? DocumentName { get; set; }
        public string? DocumentContext { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relacionamento: Uma sessão tem várias mensagens
        public List<ChatMessage> Messages { get; set; } = new();
    }
}