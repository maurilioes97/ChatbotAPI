namespace ChatbotAPI.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string SystemPrompt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relacionamento: Uma sessão tem várias mensagens
        public List<ChatMessage> Messages { get; set; } = new();
    }
}