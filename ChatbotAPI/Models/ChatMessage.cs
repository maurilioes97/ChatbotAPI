namespace ChatbotAPI.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; } // A "senha" da pasta
        public string Role { get; set; } = string.Empty; // 'User', 'Assistant' ou 'System'
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relacionamento com a sessão
        public ChatSession? Session { get; set; }
    }
}
