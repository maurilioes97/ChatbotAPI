namespace ChatbotAPI.Clients
{
    /// <summary>
    /// Modelo simples de mensagem no formato esperado pelo Gemini.
    /// </summary>
    public class GeminiChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
