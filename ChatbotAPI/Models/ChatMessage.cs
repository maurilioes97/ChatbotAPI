namespace ChatbotAPI.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relacionamento com a sessão
        public ChatSession? Session { get; set; }
    }
}
